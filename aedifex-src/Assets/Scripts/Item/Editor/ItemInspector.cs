using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Item))]
public class ItemInspector : Editor 
{
    private int currentTagIndex = 0;
    private string[] tagNames;
    private ItemLayout currentItemLayout;

    private GUIStyle titleStyle;

    public void OnEnable()
    {
        Item item = (Item)target;

        if(!item)
            return;
        
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.green;

        if (ItemFactory.IsAvailable())
            this.tagNames = ItemFactory.Instance.GetTagNames();
        else
            this.tagNames = null;

        // -2 means the item has a legacy tag
        currentTagIndex = -2;

        // -1 means the item is new
        if(string.IsNullOrEmpty(item.itemTag))
            currentTagIndex = -1;

        if (tagNames != null)
        {
            for (int i = 0; i < tagNames.Length; i++)
            {
                if (tagNames[i] == item.itemTag)
                {
                    currentTagIndex = i;
                    break;
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        Item item = (Item)target;

        if(!item)
            return;

        Undo.RecordObject(item, "Modify item");

        EditorGUIUtility.labelWidth = 75f;

        EditorGUILayout.LabelField(item.itemId, titleStyle);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        ShowItemInfo(item);
        EditorGUILayout.Space();

        if(GUI.changed)
            EditorUtility.SetDirty(item);
    }

    private void DrawArray(string propertyName)
    {
        serializedObject.Update();
        SerializedProperty tps = serializedObject.FindProperty(propertyName);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(tps, true);

        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
    }

    private void DrawSeparator()
    {
        Rect r = EditorGUILayout.BeginVertical(GUILayout.MaxHeight(1f));
        GUILayout.Space(1f);
        EditorGUILayout.EndVertical();

        Color c = GUI.color;
        GUI.color = Color.gray;
        GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
        GUI.color = c;
    }

    private bool biomesFoldout;
    
    private void ShowLayouts(Item item)
    {
        GUILayout.Label("Layouts");

        ItemLayout[] layouts = item.GetItemLayouts();

        if(GUILayout.Button("Add new layout"))
        {
            GameObject layoutGO = new GameObject("Layout");
            layoutGO.transform.parent = item.transform;
            layoutGO.transform.localPosition = Vector3.zero;
            layoutGO.transform.localRotation = Quaternion.identity;
            layoutGO.transform.localScale = Vector3.one;
            ItemLayout layout = layoutGO.AddComponent<ItemLayout>();

            int maxLayoutId = -1;

            for(int i = 0; i < layouts.Length; i++)
                if(maxLayoutId < layouts[i].layoutId)
                    maxLayoutId = layouts[i].layoutId;

            layout.layoutId = maxLayoutId + 1;
        }

        for(int i = 0; i < layouts.Length; i++)
        {
            GUILayout.BeginVertical("Box");
            GUILayout.BeginHorizontal();

            layouts[i].visualize = EditorGUILayout.Toggle(layouts[i].visualize, GUILayout.MaxWidth(30f));

            GUILayout.Label(layouts[i].name);

            GUILayout.Label("W:"+layouts[i].weight.ToString());

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select"))
                Selection.activeGameObject = layouts[i].gameObject;

            if (GUILayout.Button("Duplicate"))
            {
                ItemLayout layout = GameObject.Instantiate<ItemLayout>(layouts[i]);
                layout.transform.parent = item.transform;
                layout.transform.localPosition = Vector3.zero;
                layout.transform.localRotation = Quaternion.identity;
                layout.transform.localScale = Vector3.one;

                int maxLayoutId = -1;

                for (int j = 0; j < layouts.Length; j++)
                    if (maxLayoutId < layouts[j].layoutId)
                        maxLayoutId = layouts[j].layoutId;

                layout.layoutId = maxLayoutId + 1;
            }

            if (GUILayout.Button("Remove"))
            {
                if (EditorUtility.DisplayDialog("Delete layout", "Are you sure you want to remove this layout?", "Remove", "Cancel"))
                    GameObject.DestroyImmediate(layouts[i].gameObject);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }

    private void ShowItemInfo(Item item)
    {
        GUILayout.BeginVertical("Item Definition", "Window", GUILayout.MinHeight(75f));

        item.itemId = EditorGUILayout.TextField("Item Id", item.itemId);

        if(currentTagIndex == -2)
        {
            EditorGUILayout.LabelField("LEGACY Tag", item.itemTag);
        }
        else 
        {
            if (tagNames != null)
            {
                currentTagIndex = EditorGUILayout.Popup("Item Tag", currentTagIndex, tagNames);

                if (currentTagIndex >= 0 && currentTagIndex < tagNames.Length)
                    item.itemTag = tagNames[currentTagIndex];
            }
        }

        DrawSeparator();

        item.anchorPlane = (ItemAnchorPlane)EditorGUILayout.EnumPopup("Anchor", item.anchorPlane);

        DrawSeparator();
        DrawSeparator();

        ItemShowPreview(item);
        DrawSeparator();
        ShowLayouts(item);
        GUILayout.EndVertical();
    }

    private void ItemShowPreview(Item item)
    {   
        item = PrefabUtility.GetPrefabParent(item) as Item;

        if(item)
        {
            GUILayout.Space(5f);

            Texture2D preview = AssetPreview.GetAssetPreview(item.gameObject as Object);

            if(preview)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal("Box", GUILayout.MaxHeight(preview.height), GUILayout.MaxWidth(preview.width));

                if(preview)
                    GUILayout.Label(preview, GUILayout.MaxHeight(preview.height), GUILayout.MaxWidth(preview.width));

                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }
    }

    private static Texture2D backgroundTexture = null;
    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    static void DrawGameObjectName(Item item, GizmoType gizmoType)
    {   
        if (!backgroundTexture)
        {
            backgroundTexture = new Texture2D(1,1);
            backgroundTexture.SetPixel(0,0, new Color(0f, 0f, 0f, .75f));
            backgroundTexture.Apply(); 
        }

        GUIStyle labelStyle = new GUIStyle();
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = string.IsNullOrEmpty(item.itemId) ? Color.red : Color.green;
        labelStyle.normal.background = backgroundTexture;

        Handles.Label(item.transform.position, item.itemId + "(" + item.itemTag + ")", labelStyle);
    }
}
