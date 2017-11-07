using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ItemReference))]
public class ItemReferenceInspector : Editor 
{	
	private Vector2 modelScrollBarPosition;
	private Vector2 effectScrollBarPosition;

	private string modelFilter;
	private string effectFilter;

	private bool firstTime = true;
    private bool rotateFoldout = false;

	private Item GetItemById(string id)
	{
		Item[] items = Resources.LoadAll<Item>(ItemFactory.RESOURCES_ITEM_PATH);

		foreach(Item i in items)
			if(i.itemId == id)
				return i;

		return null;
	}

	protected void Rotate90Over(Vector3 axis)
	{
		ItemReference item = (ItemReference)target;

		if(item)
			item.transform.Rotate(axis.normalized * 90f);
	}

	public override void OnInspectorGUI()
	{
		ItemReference item = (ItemReference)target;

		if(!item)
			return;

        Undo.RecordObject(item, "Modify item reference");

		// Load item type at least, to simplify search :)
		if(firstTime)
		{
			//Item instance = GetItemById(item.itemId);
            //typeFilter = instance ? instance.type : ItemType.Decoration;
			firstTime = false;
		}

		EditorGUIUtility.labelWidth = 75f;

        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = item.GetStatusColor();

        EditorGUILayout.LabelField(item.NiceName(), titleStyle);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        ItemReferenceData(item);
        EditorGUILayout.Space();

        if(!item.procedural)
            ItemShowPreview();
        EditorGUILayout.Space();

		GUILayout.BeginVertical();

		if(EditorApplication.isPlaying)
		{
			EditorGUILayout.LabelField("Cannot edit in play mode.");
		}
		else
		{
            if(!item.procedural)
                ItemSelectGUI(item);
		}

		GUILayout.EndVertical();

        if(GUI.changed)
            EditorUtility.SetDirty(item);
	}

    private void ShowRotatePanel(ItemReference item)
    {
        EditorGUI.indentLevel++;

        rotateFoldout = EditorGUILayout.Foldout(rotateFoldout, "Rotate");

        if(rotateFoldout)
        {
            GUILayout.BeginVertical("Rotate", "Box", GUILayout.MaxHeight(100f));

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Z <-"))
                Rotate90Over(-Vector3.forward);

            if(GUILayout.Button("Z ->"))
                Rotate90Over(Vector3.forward);
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Y <-"))
                Rotate90Over(-Vector3.up);

            if(GUILayout.Button("Y ->"))
                Rotate90Over(Vector3.up);
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            if(GUILayout.Button("X <-"))
                Rotate90Over(-Vector3.right);

            if(GUILayout.Button("X ->"))
                Rotate90Over(Vector3.right);
            GUILayout.EndHorizontal();


            GUILayout.EndVertical();

            if(GUILayout.Button("Identity"))
                item.transform.localRotation = Quaternion.identity;

            GUILayout.EndVertical();
        }

        EditorGUI.indentLevel--;
    }

    private void ItemReferenceData(ItemReference itemRef)
    {
        GUILayout.BeginVertical("Item Reference", "Window", GUILayout.MinHeight(75f));

        itemRef.instanceCount = EditorGUILayout.IntField("Instance count", itemRef.instanceCount);
        itemRef.procedural = EditorGUILayout.Toggle("Procedural", itemRef.procedural);

        itemRef.probability = EditorGUILayout.Slider("Probability", itemRef.probability, 0f, 1f);
        itemRef.maxChildDepth = EditorGUILayout.IntSlider("Max depth", itemRef.maxChildDepth, 0, ItemFactory.ITEM_TREE_MAX_DEPTH);
        itemRef.viewTree = EditorGUILayout.Toggle("View Tree", itemRef.viewTree);

        itemRef.visualizeRandomness = EditorGUILayout.Toggle("Visualize", itemRef.visualizeRandomness);
        itemRef.randomSnappedOrientation = EditorGUILayout.Toggle("Random Snapped Orientation", itemRef.randomSnappedOrientation);
        itemRef.randomPositionAmplitude = EditorGUILayout.Vector3Field("Random Position Amplitude", itemRef.randomPositionAmplitude);
        itemRef.randomRotationAmplitude = EditorGUILayout.Vector3Field("Random Rotation Amplitude", itemRef.randomRotationAmplitude);
        itemRef.randomScaleAmplitude = EditorGUILayout.Vector3Field("Random Scale Amplitude", itemRef.randomScaleAmplitude);
        itemRef.uniformScale = EditorGUILayout.Toggle("Uniform Scale", itemRef.uniformScale);

        ShowRotatePanel(itemRef);

        if(itemRef.procedural)
        {
            itemRef.anchorPlane = (ItemAnchorPlane) EditorGUILayout.EnumPopup("Anchor", itemRef.anchorPlane);
            itemRef.availableProceduralVolume = EditorGUILayout.BoundsField("Volume", itemRef.availableProceduralVolume);
        }

        if(itemRef.transform.parent)
        {
            if(GUILayout.Button("Select parent"))
                Selection.activeGameObject = itemRef.transform.parent.gameObject;
        }
        
        GUILayout.EndVertical();
    }

    private static Dictionary<string, Texture2D> previewMap = new Dictionary<string, Texture2D>();

    private Texture2D GetPreviewForItem(string id, GameObject go)
    {
        if (previewMap.ContainsKey(id))
            return previewMap[id];

        Texture2D preview = AssetPreview.GetAssetPreview(go as Object);

        if (preview != null)
            previewMap[id] = preview;

        return preview;
    }

    private void ItemShowPreview()
	{	
		ItemReference itemRef = (ItemReference)target;

		if(!itemRef)
			return;

		Item item = GetItemById(itemRef.itemId);

		if(item)
		{
			GUILayout.Space(5f);

			GUILayout.BeginHorizontal("Window");
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical();

            Texture2D preview = GetPreviewForItem(itemRef.itemId, item.gameObject);

            if (preview)
                GUILayout.Label(preview, GUILayout.MaxHeight(preview.height), GUILayout.MaxWidth(preview.width));
			
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
	}

	private void ItemSelectGUI(ItemReference ir)
	{
		GUILayout.BeginVertical("Item Select", "Window", GUILayout.MinHeight(75f));
        
		modelFilter = EditorGUILayout.TextField("Filter Id", modelFilter);
		//typeFilter = (ItemType) EditorGUILayout.EnumPopup("Type ", typeFilter);

		List<Item> items = GetFilteredItemsList(ItemFactory.RESOURCES_ITEM_PATH, modelFilter);

		if(items.Count > 0)
		{
			modelScrollBarPosition = EditorGUILayout.BeginScrollView(modelScrollBarPosition, false, true, GUILayout.MinHeight(600f));

			for(int i = 0; i < items.Count; i++)
			{ 
				GUI.backgroundColor = i % 2 == 0 ? Color.white : Color.gray;

				Item item = items[i];

				if(item)
				{
					GUILayout.BeginHorizontal("Box");

					GUI.backgroundColor = Color.white;
                    Texture2D preview = GetPreviewForItem(item.itemId, item.gameObject);

                    bool buttonPressed = false;

					if(preview)
                        buttonPressed = GUILayout.Button(preview, GUILayout.MaxHeight(preview.height * .6f), GUILayout.MaxWidth(preview.width * .6f));
                    else
                        buttonPressed = GUILayout.Button("No Preview", GUILayout.Height(75f), GUILayout.MaxWidth(75f));

                    if(buttonPressed){
                        ItemReference itemReference = (ItemReference)target;

                        if(itemReference)
                        {
                            itemReference.itemId = item.itemId;
                            itemReference.name = "Ref-" + item.itemId;
                        }
                    }

					GUILayout.Label(item.itemId);
					GUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.EndScrollView();
		}
		else
		{
			EditorGUILayout.LabelField("No items found");
		}

		GUILayout.EndVertical();
	}

	private List<Item> GetFilteredItemsList(string path, string idFilter)
	{
		List<Item> prefabs = new List<Item>();
		
		foreach(Item item in Resources.LoadAll<Item>(path))
		{
			if(item)
			{
				if(string.IsNullOrEmpty(idFilter) ||  item.itemId.ToLower().Contains(idFilter.ToLower()))
					prefabs.Add(item);					
			}
		}
		
		return prefabs;
	}
    
	private static Texture2D backgroundTexture = null;
    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGameObjectName(ItemReference itemReference, GizmoType gizmoType)
	{   
        // Dont show gizmos over item references that live in an inactive layout
        if(itemReference.transform.parent)
        {
            ItemLayout layout = itemReference.transform.parent.GetComponent<ItemLayout>();

            if(layout && !layout.visualize)
                return;
        }
        
        if (!backgroundTexture)
        {
            backgroundTexture = new Texture2D(1,1);
            backgroundTexture.SetPixel(0,0, new Color(0f, 0f, 0f, .75f));
            backgroundTexture.Apply(); 
        }

        GUIStyle labelStyle = new GUIStyle();
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = itemReference.GetStatusColor();
        labelStyle.normal.background = backgroundTexture;

        Handles.Label(itemReference.transform.position, "Ref" + itemReference.NiceName(), labelStyle);

        // TODO: edit the bounds gizmo
//        Handles.CubeCap(0, itemReference.transform.position, Quaternion.identity, 1f);
//        Handles.RectangleCap(0, itemReference.transform.position, Quaternion.identity, 1f);
	}
}
