using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct ItemData
{
    public int itemIndex;
    public string itemId;
    public string itemTag;
    public ItemAnchorPlane anchorPlane;
    public ItemReferenceData[] references; // null if no references

	public ItemLayoutController[] layoutMetadata;
    public int[] layoutOffsets;
    public Bounds itemBounds;
    public float itemVolume;
}

public struct ItemReferenceData
{
    public Matrix4x4 transform;
    public int itemIndex;
    public int maxChildDepth;

	public ItemReferenceController metadata;
    public float probability;
    public bool procedural;
    public int instanceCount;
    public bool uniformScale;
    public ItemAnchorPlane anchorPlane;
    public Bounds availableProceduralVolume;
    public bool randomOrientation;
    public Vector3 randomPositionAmplitude;
    public Vector3 randomRotationAmplitude;
    public Vector3 randomScaleAmplitude;
    public int[] availableItemIndicesByTag; // If procedural, tags define possible items
}

// Tags serve as semantic metadata for procedural instantiation
// Note: because there can be a lot of tags, it is defined as a string
[System.Serializable]
public class ItemTag
{
    public string id; // (door, bag, container, box, etc)
    public string[] parentTags; // Tags accepted by this tag as containers
}

public class ItemFactory : MonoBehaviorSingleton<ItemFactory> 
{
	public const int ITEM_TREE_MAX_DEPTH = 5;
	public const string RESOURCES_ITEM_PATH = "Items";
    public const float MAX_CONTRIBUTION_DISTANCE = 0f; // 0f means only intersecting bounds will generate occlusion/GI

	private static List<int> randomBagList = new List<int>(1000);
	private static List<int> reusableWeightList = new List<int>(100);

    public ItemTag[] tags;

    private Dictionary<string, Item> itemsById = new Dictionary<string, Item>();
    private Dictionary<string, int> itemIndexById = new Dictionary<string, int>();

    // Key: tag | Value: parent tags
    private Dictionary<string, HashSet<string>> tagDictionary = new Dictionary<string, HashSet<string>>();

    // Key: tag | Value: every item that this tag can have as child
    private Dictionary<string, HashSet<int>> itemIndicesByTagParent = new Dictionary<string, HashSet<int>>();

    private Item[] itemPrefabs;
    private ItemData[] itemDataArray;

	protected override void Initialize ()
	{
        // Dont initialize stuff on edit mode
        if(Application.isPlaying)
        {
            float time = Time.realtimeSinceStartup;

            itemPrefabs = Resources.LoadAll<Item>(RESOURCES_ITEM_PATH);
            itemDataArray = new ItemData[itemPrefabs.Length];

            BuildTagDictionary();

            // First pass: item data
            for (int i = 0; i < itemPrefabs.Length; i++)
            {
                Item item = itemPrefabs[i];

                if (item == null)
                    Debug.LogError("Item factory references null item!", this);

                if (itemsById.ContainsKey(item.itemId))
                    Debug.LogError("Duplicate item id! " + item.itemId, item);
                
                AddItemToTagParents(item.itemTag, i);

                itemsById[item.itemId] = item;
                itemIndexById[item.itemId] = i;

                ItemData itemData = new ItemData();
                itemData.itemIndex = i;
                itemData.itemId = item.itemId;
                itemData.itemTag = item.itemTag;
                itemData.anchorPlane = item.anchorPlane;
                itemData.itemBounds = item.CalculateBoundsLocalSpace(0); // Important to calculate only the base level

                itemDataArray[i] = itemData;
            }

            // Second pass: item reference data
            for(int i = 0; i < itemDataArray.Length; i++)
            {
                int lOffset = 0;
                ItemData itemData = itemDataArray[i];
                Item item = itemPrefabs[i];
                ItemLayout[] layouts = item.GetItemLayouts();

                itemData.layoutOffsets = new int[layouts.Length];
				itemData.layoutMetadata = new ItemLayoutController[layouts.Length];

                List<ItemReferenceData> references = new List<ItemReferenceData>();

                for(int layout = 0; layout < layouts.Length; layout++)
                {
                    ItemReference[] itemReferences = layouts[layout].GetItemReferences();
                    itemData.layoutOffsets[layout] = lOffset;

					ItemLayoutMetadata metadata = layouts[layout].GetComponent<ItemLayoutMetadata>();

					if(metadata)
						itemData.layoutMetadata[layout] = metadata.GetController();
					else // Just set a simple controller with simple data				
						itemData.layoutMetadata[layout] = new ItemLayoutController();
					
					itemData.layoutMetadata[layout].Initialize(layouts[layout].weight);

                    for(int r = 0; r < itemReferences.Length; r++)
                    {
                        ItemReference itemReference = itemReferences[r];
                        ItemReferenceData itemReferenceData = new ItemReferenceData();

						ItemReferenceMetadata itemRefMetadata = itemReference.GetComponent<ItemReferenceMetadata>();

						if(itemRefMetadata)
							itemReferenceData.metadata = itemRefMetadata.GetController();
						else // Just set a simple controller with simple data				
							itemReferenceData.metadata = new ItemReferenceController();

						itemReferenceData.metadata.Initialize(itemReference.probability);						

                        if (itemIndexById.ContainsKey(itemReference.itemId))
                            itemReferenceData.itemIndex = itemIndexById[itemReference.itemId];
                        else
                            itemReferenceData.itemIndex = -1;

                        // Ignore item root transform, because prefabs may be modified
                        itemReferenceData.transform = item.transform.worldToLocalMatrix * itemReference.transform.localToWorldMatrix;
                        itemReferenceData.maxChildDepth = itemReference.maxChildDepth;
                        itemReferenceData.probability = itemReference.probability;
                        itemReferenceData.procedural = itemReference.procedural;
                        itemReferenceData.instanceCount = itemReference.instanceCount;

                        itemReferenceData.uniformScale = itemReference.uniformScale;
                        itemReferenceData.anchorPlane = itemReference.anchorPlane;

                        itemReferenceData.randomOrientation = itemReference.randomSnappedOrientation;

                        itemReferenceData.randomPositionAmplitude = itemReference.randomPositionAmplitude;
                        itemReferenceData.randomRotationAmplitude = itemReference.randomRotationAmplitude;
                        itemReferenceData.randomScaleAmplitude = itemReference.randomScaleAmplitude;

                        // Procedural volume is ALWAYS LOCAL TO REFERENCE
                        itemReferenceData.availableProceduralVolume = itemReference.availableProceduralVolume;

                        references.Add(itemReferenceData);
                    }

                    lOffset += itemReferences.Length;
                }

                itemData.references = references.ToArray();

                // Save back data
                itemDataArray[i] = itemData;
            }

            // Calculate bounds, save them temporarily and then assign
            Bounds[] tempItemBounds = new Bounds[itemDataArray.Length];
            for (int i = 0; i < itemDataArray.Length; i++)
                tempItemBounds[i] = CalculateItemBounds(ref itemDataArray[i]);

            for (int i = 0; i < itemDataArray.Length; i++)
            {
                Bounds b = tempItemBounds[i];
                itemDataArray[i].itemBounds = b;
                itemDataArray[i].itemVolume = b.size.x * b.size.y * b.size.z;
            }

            for (int i = 0; i < itemDataArray.Length; i++)
            {
                ItemData item = itemDataArray[i];

                if (item.references != null)
                {
                    // Basically, for each reference, precalculate every item that can fit inside it :)
                    for (int r = 0; r < item.references.Length; r++)
                    {                        
                        item.references[r].availableItemIndicesByTag =
                            CalculateAvailableItemsInProceduralVolume(item.itemTag, item.references[r].availableProceduralVolume, item.references[r].anchorPlane).ToArray();
                    }
                }
            }

            //initMillis = (Time.realtimeSinceStartup - time) * 1000f;
        }
	}

    public Bounds CalculateItemReferenceBounds(ref ItemReferenceData itemReference, int depth = ITEM_TREE_MAX_DEPTH)
    {
        if (itemReference.procedural)
            return MathUtils.TransformBounds(ref itemReference.transform, itemReference.availableProceduralVolume);

        if (itemReference.itemIndex >= 0)
        {
            Bounds itemBounds = CalculateItemBounds(ref itemDataArray[itemReference.itemIndex], depth);
            return MathUtils.TransformBounds(ref itemReference.transform, itemBounds);
        }

        return MathUtils.TransformBounds(ref itemReference.transform, new Bounds());
    }

    private Bounds CalculateItemBounds(ref ItemData itemData, int depth = ITEM_TREE_MAX_DEPTH)
    {
        // Bounds are initially defined as the item meshes bounds
        Bounds bounds = itemData.itemBounds;

        if(depth > 0)
        {
            ItemReferenceData[] itemReferences = itemData.references;

            if (itemReferences != null)
            {
                for (int r = 0; r < itemReferences.Length; r++)
                    bounds.Encapsulate(CalculateItemReferenceBounds(ref itemData.references[r], depth - 1));
            }
        }

        return bounds;
    }
    
    /// <summary>
    /// Adds an item to the available items for a specific parent tag
    /// This way, for a specific parent tag we already know the complete set of items that can be added.
    /// </summary>
    private void AddItemToTagParents(string tag, int itemIndex)
    {
        if (!tagDictionary.ContainsKey(tag))
            return;
        
        HashSet<string> parentTags = tagDictionary[tag];

        foreach (string pTag in parentTags)
        {
            if (!itemIndicesByTagParent.ContainsKey(pTag))
                itemIndicesByTagParent[pTag] = new HashSet<int>();

            itemIndicesByTagParent[pTag].Add(itemIndex);
        }
    }

    /// <summary>
    /// Builds a dictionary with tag parents by tag
    /// </summary>
    private void BuildTagDictionary()
    {
        for (int i = 0; i < tags.Length; i++)
        {
            string[] parents = tags[i].parentTags;

            tagDictionary[tags[i].id] = new HashSet<string>();

            for (int t = 0; t < parents.Length; t++)
                tagDictionary[tags[i].id].Add(parents[t]);
        }
    }

    private List<int> CalculateAvailableItemsInProceduralVolume(string parentItemTag, Bounds proceduralVolume, ItemAnchorPlane anchorPlane)
    {
        List<int> itemIndices = new List<int>();

        if (itemIndicesByTagParent.ContainsKey(parentItemTag))
        {
            HashSet<int> availableIndices = itemIndicesByTagParent[parentItemTag];

            foreach (int itemIndex in availableIndices)
            {
                ItemData item = itemDataArray[itemIndex];

                // We are ignoring random transformations for this check. This is on purpose... randoms shouldn't change too much
                if (item.anchorPlane == anchorPlane && MathUtils.ContainsWithoutTranslation(proceduralVolume, item.itemBounds))
                    itemIndices.Add(itemIndex);
            }
        }

        return itemIndices;
    }

    //private float buildMillis = 0f;
    //private float initMillis = 0f;
    private List<Bounds> transformedBoundsGizmosReferences = new List<Bounds>();
    private List<Bounds> transformedBoundsGizmosItems = new List<Bounds>();
    
	public RuntimeItemRoot BuildItem(Quaternion objectRotation, Vector3 viewDirection, Vector3 lightDirection, string itemId)
    {
        if(!ItemExists(itemId))
            Debug.LogError("Item " + itemId + " does not exist!");

        GameObject itemGO = new GameObject(itemId);
		RuntimeItemRoot itemRoot = itemGO.AddComponent<RuntimeItemRoot>();

        transformedBoundsGizmosReferences.Clear();
        transformedBoundsGizmosItems.Clear();

        float time = Time.realtimeSinceStartup;

        if(itemIndexById.ContainsKey(itemId))
        {
            int itemIndex = itemIndexById[itemId];

            // Add the floor!
            Bounds sceneBounds = itemDataArray[itemIndex].itemBounds;
            sceneBounds = new Bounds(Vector3.up * -.1f, new Vector3(sceneBounds.size.x, .25f, sceneBounds.size.z));

			GameObject dynamicRoot = new GameObject("DynamicItems");
			dynamicRoot.transform.parent = itemGO.transform;
			dynamicRoot.transform.localPosition = Vector3.zero;
			dynamicRoot.transform.localRotation = Quaternion.identity;
			dynamicRoot.transform.localScale = Vector3.one;

			// Control randomness (TODO: hook to proc engine)
            System.Random rnd = new System.Random((int)System.DateTime.Now.Ticks);

            BuildRootDynamicItem(dynamicRoot, viewDirection, rnd, itemIndex, Matrix4x4.identity, 0, ITEM_TREE_MAX_DEPTH - 1);
        }

        //buildMillis += (Time.realtimeSinceStartup - time) * 1000f;

		Renderer[] renderers = itemGO.GetComponentsInChildren<Renderer>();

		for(int i = 0; i < renderers.Length; i++)
		{
			if(i == 0)
				itemRoot.itemBounds = renderers[i].bounds;
			else
				itemRoot.itemBounds.Encapsulate(renderers[i].bounds);
		}

		return itemRoot;
    }

    //public void OnGUI()
    //{
    //    GUILayout.Label("Item factory construction time: " + initMillis.ToString("0.00") + "ms");

    //    if (buildMillis > 0f)
    //        GUILayout.Label("Item build time: " + buildMillis.ToString("0.00") + "ms");
    //}

	/// <summary>
	/// Remove all item-related scripts to a dynamic element
	/// </summary>
	private void CleanDynamicItem(GameObject go)
	{
		Item item = go.GetComponent<Item>();
		ItemReference[] dynReferences = go.GetComponentsInChildren<ItemReference>();

		GameObject.Destroy(item);

		for(int i = 0; i < dynReferences.Length; i++)
			GameObject.Destroy(dynReferences[i]);
	}

	// Start a dynamic item subtree
	private void BuildRootDynamicItem(GameObject rootDynamicGameObject, Vector3 viewDirection, System.Random rnd, int itemIndex, Matrix4x4 parentMatrix, int depth, int maxDepth)
	{
		if(itemIndex < itemPrefabs.Length)
		{
			ItemData parentItemData = itemDataArray[itemIndex];
			GameObject itemInstance = GameObject.Instantiate(itemPrefabs[itemIndex].gameObject);
			itemInstance.name = parentItemData.itemId;

			itemInstance.transform.parent = rootDynamicGameObject.transform;
			itemInstance.transform.position = ExtractTranslationFromMatrix(ref parentMatrix);
			itemInstance.transform.rotation = ExtractRotationFromMatrix(ref parentMatrix);
			itemInstance.transform.localScale = ExtractScaleFromMatrix(parentMatrix);

			CleanDynamicItem(itemInstance);

			// Build all children now
			BuildDynamicItem(viewDirection, rnd, itemIndex, itemInstance.transform, depth, maxDepth);
		}        
	}

	private void BuildDynamicItem(Vector3 viewDirection, System.Random rnd, int parentItemIndex, Transform parentTransform, int depth, int maxDepth)
	{
		if (depth > maxDepth || parentItemIndex < 0 || parentItemIndex >= itemDataArray.Length)
			return;
		
		ItemData parentItemData = itemDataArray[parentItemIndex];

		if (parentItemData.layoutOffsets != null && parentItemData.layoutOffsets.Length > 0)
		{
			reusableWeightList.Clear();

			for(int i = 0; i < parentItemData.layoutMetadata.Length; i++)
				reusableWeightList.Add(parentItemData.layoutMetadata[i].GetWeight());

			int selectedLayout = GetWeightedRandomIndex(reusableWeightList, rnd);

			int layoutOffset = parentItemData.layoutOffsets[selectedLayout];
			int layoutSize = parentItemData.references.Length - layoutOffset; // By default, layout size is the remaining segment of the reference array

			// If there is another layout, infer the layout size from the next offset
			if (selectedLayout + 1 < parentItemData.layoutOffsets.Length)
				layoutSize = parentItemData.layoutOffsets[selectedLayout + 1] - layoutOffset;

			for (int i = 0; i < layoutSize; i++)
			{
				ItemReferenceData itemReference = parentItemData.references[layoutOffset + i];

                // Add as many children as possible
                int childCount = Mathf.Max(1, itemReference.instanceCount);

                for(int j = 0; j < childCount; ++j)
                {
                    int childItemIndex = -1;

				    // First define the item to instantiate
				    if (itemReference.procedural)
				    {
					    if (itemReference.availableItemIndicesByTag.Length > 0)
					    {
						    // Reference is a random item, find it!
						    int randomIndex = rnd.Next(itemReference.availableItemIndicesByTag.Length); // TODO: weighted random by volume
						    childItemIndex = itemReference.availableItemIndicesByTag[randomIndex];
					    }
				    }
				    else
				    {
					    // Reference is a specific item; just instance it
					    if ((float) rnd.NextDouble() < itemReference.metadata.GetProbability())
						    childItemIndex = itemReference.itemIndex;
				    }

				    // Then just build it
				    if(childItemIndex >= 0)
				    {
					    Matrix4x4 childLocalMatrix = GetRandomTransformation(ref itemReference, ref itemDataArray[childItemIndex], rnd);
					    ItemData childItemData = itemDataArray[childItemIndex];

					    if(ShouldCullObject(ref childItemData, viewDirection, childLocalMatrix))
						    continue;
					
					    GameObject itemInstance = GameObject.Instantiate(itemPrefabs[childItemIndex].gameObject);
					    itemInstance.name = itemDataArray[childItemIndex].itemId;
					    itemInstance.transform.parent = parentTransform;
					    itemInstance.transform.localPosition = ExtractTranslationFromMatrix(ref childLocalMatrix);
					    itemInstance.transform.localRotation = ExtractRotationFromMatrix(ref childLocalMatrix);
					    itemInstance.transform.localScale = ExtractScaleFromMatrix(childLocalMatrix);

                        InterestPoint[] interestPoints = itemInstance.GetComponentsInChildren<InterestPoint>();

                        foreach (InterestPoint p in interestPoints)
                            p.AssociateItemBounds(itemInstance.transform, itemDataArray[childItemIndex].itemBounds);

					    CleanDynamicItem(itemInstance);

					    BuildDynamicItem(viewDirection, rnd, childItemIndex, itemInstance.transform, depth + 1, Mathf.Min(maxDepth, depth + itemReference.maxChildDepth));
                    }
                }
            }
		}
	}

	/// <summary>
	/// Should we ignore this object given the camera view etc?
	/// </summary>
	public bool ShouldCullObject(ref ItemData itemData, Vector3 viewDirection, Matrix4x4 matrix)
    {
		return false;
    }

    public Vector3 GetAnchorDirection(ItemAnchorPlane anchorPlane)
    {
        Vector3 direction = Vector3.one;

        switch (anchorPlane)
        {
            case ItemAnchorPlane.POSITIVE_X:
                direction = Vector3.right;
                break;
            case ItemAnchorPlane.POSITIVE_Y:
                direction = Vector3.up;
                break;
            case ItemAnchorPlane.POSITIVE_Z:
                direction = Vector3.forward;
                break;
            case ItemAnchorPlane.NEGATIVE_X:
                direction = -Vector3.right;
                break;
            case ItemAnchorPlane.NEGATIVE_Y:
                direction = -Vector3.up;
                break;
            case ItemAnchorPlane.NEGATIVE_Z:
                direction = -Vector3.forward;
                break;
        }

        return direction;
    }

    public Vector3 GetAnchorPosition(ItemAnchorPlane anchorPlane, Bounds bounds)
    {
        Vector3 direction = GetAnchorDirection(anchorPlane);
        return bounds.center + direction * Mathf.Abs(Vector3.Dot(direction, bounds.extents));
    }

    private Matrix4x4 GetRandomOrientationMatrix(System.Random rnd, ItemAnchorPlane anchorPlane)
    {
        Vector3 dir = GetAnchorDirection(anchorPlane);

        int rot = rnd.Next(4) * 90;

        return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(dir * rot), Vector3.one);
    }

    private Matrix4x4 GetRandomTransformation(ref ItemReferenceData itemReference, ref ItemData itemData, System.Random rnd)
    {
        Matrix4x4 matrix = itemReference.transform;

        if (itemReference.randomOrientation)
            matrix *= GetRandomOrientationMatrix(rnd, itemReference.anchorPlane);
        
        Vector3 randPositionAmplitude = itemReference.randomPositionAmplitude;

        if (itemReference.procedural)
        {
            Vector3 refAnchorPosition = GetAnchorPosition(itemReference.anchorPlane, itemReference.availableProceduralVolume);
            Vector3 itemAnchorPosition = GetAnchorPosition(itemData.anchorPlane, itemData.itemBounds);

            matrix *= Matrix4x4.TRS(refAnchorPosition - itemAnchorPosition, Quaternion.identity, Vector3.one);

            // Add random amplitude based on the possible volume
            Vector3 anchorDirection = GetAnchorDirection(itemReference.anchorPlane);
            randPositionAmplitude += Vector3.Scale(itemReference.availableProceduralVolume.size, Vector3.one - MathUtils.Abs(anchorDirection));

            Vector3 position = GetRandomPositionOffset(randPositionAmplitude, rnd);
            Quaternion rotation = Quaternion.Euler(GetRandomRotationOffset(itemReference.randomRotationAmplitude, rnd));
            Vector3 scale = GetRandomScaleOffset(itemReference.randomScaleAmplitude, itemReference.uniformScale, rnd);

            return matrix * Matrix4x4.TRS(position, rotation, scale);
        }
        else
        {
            Vector3 position = GetRandomPositionOffset(randPositionAmplitude, rnd);
            Quaternion rotation = Quaternion.Euler(GetRandomRotationOffset(itemReference.randomRotationAmplitude, rnd));
            Vector3 scale = GetRandomScaleOffset(itemReference.randomScaleAmplitude, itemReference.uniformScale, rnd);

            return matrix * Matrix4x4.TRS(position, rotation, scale);
        }
    }
    
    public static Vector3 GetRandomPositionOffset(Vector3 randomPositionAmplitude, System.Random rnd)
    {
        return Vector3.Scale(randomPositionAmplitude, new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble()) * 2f - Vector3.one);
    }

    public static Vector3 GetRandomRotationOffset(Vector3 randomRotationAmplitude, System.Random rnd)
    {
        return Vector3.Scale(randomRotationAmplitude, new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble()) * 2f - Vector3.one);
    }

    public static Vector3 GetRandomScaleOffset(Vector3 randomScaleAmplitude, bool uniformScale, System.Random rnd)
    {
        if (uniformScale)
            return Vector3.one + randomScaleAmplitude * ((float)rnd.NextDouble() * 2f - 1f);
        else
            return Vector3.one + Vector3.Scale(randomScaleAmplitude, new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble()) * 2f - Vector3.one);
    }
    
    public bool ItemExists(string id)
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            Item[] prefabs = Resources.LoadAll<Item>(RESOURCES_ITEM_PATH);

            for (int i = 0; i < prefabs.Length; i++)
                if (prefabs[i].itemId == id)
                    return true;

            return false;
        }
#endif

        return itemsById.ContainsKey(id);
    }

    public Item GetItemPrefab(string id)
    {
        if (!itemsById.ContainsKey(id))
        {
            Debug.LogError("No item with id " + id);
            return null;
        }

        return itemsById[id];
    }
    
    public string[] GetTagNames()
    {
        string[] tagNames = new string[tags.Length];

        for(int i = 0; i < tags.Length; i++)
            tagNames[i] = tags[i].id;

        return tagNames;
    }

    public void OnDrawGizmos()
    {
        if(Application.isPlaying)
        {
//            Gizmos.color = Color.red;
//            foreach(Bounds b in transformedBoundsGizmosReferences)
//                Gizmos.DrawWireCube(b.center, b.size);
//
//            Gizmos.color = Color.green;
//            foreach (Bounds b in transformedBoundsGizmosItems)
//                Gizmos.DrawWireCube(b.center, b.size);

//            Gizmos.color = Color.green;
//            foreach (Occluder b in occlusionBounds)
//                if(b.type == OcclusionType.AABB)
//                    Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
//                else
//                    Gizmos.DrawWireSphere(b.bounds.center, b.bounds.extents.magnitude);
        }
    }


	// Taken from the Unity forums... quick hack, I think these could be improved upon
	public static Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix)
	{
		Vector3 scale;
		scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
		scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
		scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
		return scale;
	}

	public static Vector3 ExtractTranslationFromMatrix(ref Matrix4x4 matrix) {
		Vector3 translate;
		translate.x = matrix.m03;
		translate.y = matrix.m13;
		translate.z = matrix.m23;
		return translate;
	}

	public static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix) {
		Vector3 forward;
		forward.x = matrix.m02;
		forward.y = matrix.m12;
		forward.z = matrix.m22;

		Vector3 upwards;
		upwards.x = matrix.m01;
		upwards.y = matrix.m11;
		upwards.z = matrix.m21;

		return Quaternion.LookRotation(forward, upwards);
	}

	private int GetWeightedRandomIndex(List<int> weights, System.Random rnd)
	{
		randomBagList.Clear();

		for(int i = 0; i < weights.Count; i++)
		{
			int weight = weights[i];

			for(int w = 0; w < weight; w++)
				randomBagList.Add(i);
		}

		int index = rnd.Next(randomBagList.Count);

		if( index < randomBagList.Count)
			return randomBagList[index];
		else 
			return -1;
	}
}