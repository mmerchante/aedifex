using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ItemLayout : MonoBehaviour 
{
    public bool visualize = false; // Only useful in edit mode

    public bool visualizeAllRandom = false;

    public int weight = 1;

    [HideInInspector]
    public int layoutId = -1;

    public void Update()
    {
        if(!Application.isPlaying)
        {
            this.transform.localPosition = Vector3.zero;
            this.transform.localRotation = Quaternion.identity;
            this.transform.localScale = Vector3.one;

            this.gameObject.name = "Layout-" + layoutId;

#if UNITY_EDITOR
            // If a reference is selected, update layout visibility to prevent handling invisible references
            if (Selection.activeGameObject && Selection.activeGameObject.GetComponent<ItemReference>())
                this.visualize = CheckIfTreeIsSelected(Selection.activeGameObject.transform);
#endif
        }
    }

    private bool CheckIfTreeIsSelected(Transform t)
    {
        if (t && t.gameObject == this.gameObject)
            return true;

        if(t)
            return CheckIfTreeIsSelected(t.parent);

        return false;
    }

    public ItemReference[] GetItemReferences()
    {
        return this.gameObject.GetComponentsInChildren<ItemReference>(true);
    }
}