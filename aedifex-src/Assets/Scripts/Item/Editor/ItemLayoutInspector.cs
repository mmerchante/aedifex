using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(ItemLayout))]
public class ItemLayoutInspector : Editor
{
    public override void OnInspectorGUI()
    {
        ItemLayout layout = (ItemLayout) target;

        if(!layout)
            return;

        Undo.RecordObject(layout, "Modify item layout");

        GUILayout.Space(5f);

        GUILayout.BeginVertical("Item Layout", "Window", GUILayout.MinHeight(75f));

        layout.weight = EditorGUILayout.IntField("Weight", layout.weight);
        layout.visualize = EditorGUILayout.Toggle("Visualize", layout.visualize);
        layout.visualizeAllRandom = EditorGUILayout.Toggle("View Random", layout.visualizeAllRandom);

        if(layout.transform.parent)
        {
            if(GUILayout.Button("Select parent"))
                Selection.activeGameObject = layout.transform.parent.gameObject;
        }

        if(GUILayout.Button("Add item reference"))
        {
            GameObject itemRef = new GameObject("ItemRef");
            itemRef.transform.parent = layout.transform;
            itemRef.transform.localPosition = Vector3.zero;
            itemRef.transform.localRotation = Quaternion.identity;
            itemRef.transform.localScale = Vector3.one;
            itemRef.AddComponent<ItemReference>();
        }

        ItemReference[] refs = layout.GetItemReferences();

        for(int i = 0; i < refs.Length; i++)
        {
            GUILayout.BeginHorizontal();

            Color prevColor = GUI.color;

            GUI.color = refs[i].GetStatusColor();

            GUILayout.Label(refs[i].NiceName());

            GUI.color = prevColor;

            if(GUILayout.Button("Select"))
                Selection.activeGameObject = refs[i].gameObject;
            
            if(GUILayout.Button("Remove"))
            {
                if (EditorUtility.DisplayDialog("Delete item reference", "Are you sure you want to remove this reference?", "Remove", "Cancel"))
                    GameObject.DestroyImmediate(refs[i].gameObject);
            }

            GUILayout.EndHorizontal();
        }


        GUILayout.EndVertical();

        if(GUI.changed)
            EditorUtility.SetDirty(layout);
    }
}