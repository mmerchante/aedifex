using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Dummy class to represent referenced items
/// It essentially saves its transform
/// </summary>
[ExecuteInEditMode]
public class ItemReference : MonoBehaviour 
{
    private static float SNAP_THRESHOLD_DISTANCE = .5f;
    private static Vector3[] snapAABB = { new Vector3 (1f, 1f, 1f), new Vector3 (1f, -1f, -1f), new Vector3 (1f, 1f, -1f), new Vector3 (1f, -1f, 1f),
        new Vector3 (-1f, 1f, 1f), new Vector3 (-1f, -1f, -1f), new Vector3 (-1f, 1f, -1f), new Vector3 (-1f, -1f, 1f),     
    };

	public string itemId = "";

    // Max depth the item tree can have under this item when instancing
    public int maxChildDepth = 10;

    public bool procedural = false; // If true, the item to instantiate depends on the parent item tag

    public int instanceCount = 1; // If procedural, amount of random items to place

    // Available volume for instancing stuff
    public Bounds availableProceduralVolume = new Bounds(Vector3.zero, Vector3.one);

    // Plane in which item is going to be instanced
    public ItemAnchorPlane anchorPlane = ItemAnchorPlane.NEGATIVE_Y;

    // Added to the original transformation
    public bool visualizeRandomness = false;

    public bool viewTree = false;

    // If true, the item can be rotated a random multiplier of 90 degrees over its anchor plane
    public bool randomSnappedOrientation = false;

    public Vector3 randomPositionAmplitude = Vector3.zero;
    public Vector3 randomRotationAmplitude = Vector3.zero;
    public Vector3 randomScaleAmplitude = Vector3.zero;
    public bool uniformScale = true; // If uniform scale, scale is always the same in x,y,z

	/// <summary>
	/// Probability of appeareance (0.0f: never, 1.0f: always)
	/// </summary>
	public float probability = 1.0f;

    private Vector3 GetAnchorPlaneSize()
    {
        Vector3 planeNormal = Vector3.one;

        switch (anchorPlane)
        {
            case ItemAnchorPlane.POSITIVE_X:
            case ItemAnchorPlane.NEGATIVE_X:
                planeNormal = Vector3.right;
                break;
            case ItemAnchorPlane.POSITIVE_Y:
            case ItemAnchorPlane.NEGATIVE_Y:
                planeNormal = Vector3.up;
                break;
            case ItemAnchorPlane.POSITIVE_Z:
            case ItemAnchorPlane.NEGATIVE_Z:
                planeNormal = Vector3.forward;
                break;
        }

        return availableProceduralVolume.size - Vector3.Scale(availableProceduralVolume.size, planeNormal) + planeNormal * .01f;
    }

    public Vector3 GetAnchorPositionLocal()
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

        return direction * Mathf.Abs(Vector3.Dot(direction, availableProceduralVolume.extents));
    }

    public Vector3 GetAnchorPositionWorld()
    {
        return transform.position + GetAnchorPositionLocal();
    }
    
	#if UNITY_EDITOR
	[System.Serializable]
	private class MeshTransform
	{
		public Mesh mesh;
		public Matrix4x4 matrix;
		public Material material;
	}

	private List<MeshTransform> cachedMeshes = new List<MeshTransform>();
	private bool loaded = false;
	private string previousId;
    private bool previousViewTree;

    private Item FindRootItem(Transform t)
    {
        Item item = t.gameObject.GetComponent<Item>();

        if(item)
            return item;
        else if(t.parent)            
            return FindRootItem(t.parent);

        return null;
    }

    public string NiceName()
    {
        if(procedural)
        {
            // Only direct parent can serve as reference
            Item parentReference = FindRootItem(transform);

            if(parentReference && !string.IsNullOrEmpty(parentReference.itemId))
                return "Child (" + parentReference.itemTag + ", " + probability.ToString("0.0") + ")";

            return "None";
        }
        else
        {
            Item item = GetItemEditor();

            if(item != null)
                return itemId + " (" + item.itemTag + ", " + probability.ToString("0.0") + ")";
        }

        return "Missing";
    }

    private string GetItemTagEditor()
    {
        Item item = GetItemEditor();

        if(item)
            return item.itemTag;

        return "";
    }

    private Item GetItemEditor()
    {
        if(!string.IsNullOrEmpty(itemId))
            return GetItemByIdEditor(itemId);

        return null;
    }

	private static Item GetItemByIdEditor(string id)
	{
		Item[] items = Resources.LoadAll<Item>(ItemFactory.RESOURCES_ITEM_PATH);

		foreach(Item i in items)
			if(i.itemId == id)
				return i;

		return null;
	}

    private void LoadMeshes()
    {
        if (!string.IsNullOrEmpty(itemId) && !loaded || previousId != itemId || previousViewTree != viewTree)
        {
            cachedMeshes = new List<MeshTransform>();
            loaded = LoadMeshes(cachedMeshes, itemId, Matrix4x4.identity, viewTree);
            previousId = itemId;
            previousViewTree = viewTree;
        }
    }

	private static bool LoadMeshes(List<MeshTransform> meshes, string itemId, Matrix4x4 parentMatrix, bool viewTree, int depth = ItemFactory.ITEM_TREE_MAX_DEPTH)
	{
        if (depth <= 0)
            return true;

		bool loaded = false;
		Item item = GetItemByIdEditor(itemId);

		if(item)
		{
			Matrix4x4 rootMatrix = item.transform.localToWorldMatrix.inverse;
			MeshFilter[] filters = item.GetComponentsInChildren<MeshFilter>(true);

			for(int i = 0; i < filters.Length; i++)
			{
				Mesh mesh = filters[i].sharedMesh;
				Renderer meshRenderer = filters[i].GetComponent<Renderer>();

				MeshTransform cache = new MeshTransform();
				cache.mesh = mesh;
				cache.matrix = parentMatrix * rootMatrix * filters[i].transform.localToWorldMatrix;
				cache.material = meshRenderer ? meshRenderer.sharedMaterial : null;

                meshes.Add(cache);
			}

            // If the item has no meshes it means it is a "scene"
            if (filters.Length == 0 || viewTree)
            {
                ItemReference[] childReferences = item.GetComponentsInChildren<ItemReference>(true);
                for (int i = 0; i < childReferences.Length; i++)
                    if (!childReferences[i].procedural)
                        LoadMeshes(meshes, childReferences[i].itemId, parentMatrix * rootMatrix * childReferences[i].transform.localToWorldMatrix, viewTree, depth - 1);
            }

            loaded = true;
		}

        return loaded;
	}

    public Color GetStatusColor()
    {
        if(procedural)
            return Color.yellow;
        
        if(string.IsNullOrEmpty(itemId) || !ItemFactory.Instance.ItemExists(itemId))
            return Color.red;

        return Color.green;
    }

	public void OnDrawGizmos()
	{
        if(Application.isPlaying)
            return;

        bool layoutViz = false;

        // Dont show gizmos over item references that live in an inactive layout
        if(transform.parent)
        {
            ItemLayout layout = transform.parent.GetComponent<ItemLayout>();

            layoutViz = layout.visualizeAllRandom;

            if(layout && !layout.visualize)
                return;
        }

        bool previzRandom = (visualizeRandomness && Selection.activeGameObject == this.gameObject) || layoutViz;
        int previzInstances = previzRandom ? (instanceCount > 1 ? instanceCount : 15) : 1;
        System.Random rnd = new System.Random(itemId.GetHashCode());

        if(procedural)
        {
            for (int i = 0; i < previzInstances; i++)
            {
                Matrix4x4 previzRandomMatrix = Matrix4x4.identity;

                if (previzRandom)
                {
                    Vector3 randomOffset = ItemFactory.GetRandomPositionOffset(randomPositionAmplitude, rnd);
                    Vector3 randomRotation = ItemFactory.GetRandomRotationOffset(randomRotationAmplitude, rnd);
                    Vector3 randomScale = ItemFactory.GetRandomScaleOffset(randomScaleAmplitude, uniformScale, rnd);

                    previzRandomMatrix = Matrix4x4.TRS(randomOffset, Quaternion.Euler(randomRotation), randomScale);
                }

                Gizmos.matrix = this.transform.localToWorldMatrix * previzRandomMatrix;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(Vector3.zero, this.availableProceduralVolume.size);

                Gizmos.color = new Color(0f, 1f, 1f, .5f);
                Gizmos.DrawCube(GetAnchorPositionWorld() - this.transform.position, GetAnchorPlaneSize());
            }
        }
        else
        {
    		LoadMeshes();

            Bounds itemBounds = CalculateBoundsWorldSpace(1);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(itemBounds.center, itemBounds.size);

            for(int i = 0; i < previzInstances; i++)
            {
                Matrix4x4 previzRandomMatrix = Matrix4x4.identity;

                if (previzRandom)
                {
                    Vector3 randomOffset = ItemFactory.GetRandomPositionOffset(randomPositionAmplitude, rnd);
                    Vector3 randomRotation = ItemFactory.GetRandomRotationOffset(randomRotationAmplitude, rnd);
                    Vector3 randomScale = ItemFactory.GetRandomScaleOffset(randomScaleAmplitude, uniformScale, rnd);

                    previzRandomMatrix = Matrix4x4.TRS(randomOffset, Quaternion.Euler(randomRotation), randomScale);
                }

        		foreach(MeshTransform m  in cachedMeshes)
        		{
                    m.material.SetPass(0);
                    Graphics.DrawMeshNow(m.mesh, transform.localToWorldMatrix * previzRandomMatrix * m.matrix, 0);
        		}
            }
        }
	}

    private Vector3 VectorSign(Vector3 v)
    {
        return new Vector3(Mathf.Sign(v.x), Mathf.Sign(v.y), Mathf.Sign(v.z));
    }

    private Vector3 VectorAbs(Vector3 v)
    {
        v.x = Mathf.Abs(v.x);
        v.y = Mathf.Abs(v.y);
        v.z = Mathf.Abs(v.z);

        return v;
    }

    private Vector3 GetMinimumAbsAxis(Vector3 v)
    {
        Vector3 axis = VectorAbs(v);

        if (axis.x < axis.y && axis.x < axis.z)
            return Vector3.right;
        else if (axis.y < axis.x && axis.y < axis.z)
            return Vector3.up;
        
        return Vector3.forward;
    }

    private Bounds TransformAABB(Transform t, Bounds bounds, bool ignoreBoundsCenter)
    {
        Vector3 size = Vector3.zero;

        // No need to get efficient crazy
        foreach (Vector3 v in snapAABB)
        {
            Vector3 tmp = Vector3.Scale(bounds.size, v);
            Vector3 projected = new Vector3(Mathf.Abs(Vector3.Dot(tmp, t.right)),
                Mathf.Abs(Vector3.Dot(tmp, t.up)),
                Mathf.Abs(Vector3.Dot(tmp, t.forward)));
            size = Vector3.Max(size, Vector3.Scale(projected, t.lossyScale));
        }

        if(ignoreBoundsCenter)
            return new Bounds(t.transform.position, size);
        else
            return new Bounds(bounds.center, size);
    }

    public float GetMaxAbsComponent(Vector3 v)
    {
        return Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z)));
    }

    public void Update()
    {
        if(Application.isEditor && !Application.isPlaying)
        {
            this.gameObject.name = NiceName();

            // Snapping
            if(Selection.activeGameObject == this.gameObject && transform.parent && Input.GetKey(KeyCode.LeftShift))
            {
                float minDistance = float.PositiveInfinity;
                Vector3 snapVector = Vector3.zero;
                Vector3 snapOutwards = Vector3.zero;

                ItemReference[] siblings = transform.parent.GetComponentsInChildren<ItemReference>();
                Bounds transformedBounds = TransformAABB(transform, availableProceduralVolume, true);

                List<Bounds> snappingBounds = new List<Bounds>();

                for(int i = 0; i < siblings.Length; i++)
                    if(siblings[i] != this)
                        snappingBounds.Add(TransformAABB(siblings[i].transform, siblings[i].availableProceduralVolume, true));

                Item item = FindRootItem(this.transform);

                if(item)
                    snappingBounds.Add(TransformAABB(item.transform, item.CalculateBoundsLocalSpace(), false));

                bool foundClosest = false;

                for(int i = 0; i < snappingBounds.Count; i++)
                {
                    Bounds transformedOtherBounds = snappingBounds[i];

                    Vector3 toOtherTile = transformedOtherBounds.center - transformedBounds.center;
                    Vector3 d = VectorAbs(toOtherTile) - VectorAbs(transformedBounds.extents + transformedOtherBounds.extents);
                    Vector3 minAxis = GetMinimumAbsAxis(d);

                    // Remove intersections
                    d = Vector3.Max(d, Vector3.zero);

                    // Any norm is useful
                    float distance = d.magnitude;

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        foundClosest = true;
                        snapOutwards = Vector3.Scale(minAxis, VectorSign(toOtherTile));
                        snapVector = snapOutwards * Mathf.Abs(Vector3.Dot(d, snapOutwards));
                    }
                }

                if (foundClosest)
                {
                    if (minDistance < SNAP_THRESHOLD_DISTANCE)
                        this.transform.position += snapVector;
                }
            }
        }
    }

#endif

    public Bounds CalculateBoundsWorldSpace(int depth)
    {
        Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

        if (procedural)
            return MathUtils.TransformBounds(ref localToWorldMatrix, availableProceduralVolume);

        Item item = null;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            item = GetItemEditor();
        else
#endif
            item = ItemFactory.Instance.GetItemPrefab(itemId);

        if (item)
            return MathUtils.TransformBounds(ref localToWorldMatrix, item.CalculateBoundsLocalSpace(depth));

        return MathUtils.TransformBounds(ref localToWorldMatrix, new Bounds());
    }

}