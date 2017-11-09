using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemAnchorPlane
{
    POSITIVE_X,
    POSITIVE_Y,
    POSITIVE_Z,

    NEGATIVE_X,
    NEGATIVE_Y,
    NEGATIVE_Z
}

/// <summary>
/// Item transform must always be an identity
/// </summary>
public class Item : MonoBehaviour
{
    // Unique identifier for item
	public string itemId = "";
    
    // Item tag, which defines what this item represents
    public string itemTag = "";
    
    // Anchor point it uses to bind itself to its container
    public ItemAnchorPlane anchorPlane = ItemAnchorPlane.NEGATIVE_Y;
    
    // For elongated shapes (floor, walls), AABB is better; else it is recomended using a sphere
    //public OcclusionType occlusionType = OcclusionType.Sphere;

    // Sometimes some objects are over estimated
    //public float occlusionSizeMultiplier = 1f;
        
	public virtual int ItemLayer
	{
		get { return 0; }
	}
    
    public ItemLayout[] GetItemLayouts()
    {
        return this.gameObject.GetComponentsInChildren<ItemLayout>(true);
    }

#if UNITY_EDITOR
	[ContextMenu ("Set name to GO name")]
	public void EditorSetItemIdToGameObjectName()
	{
		this.itemId = this.gameObject.name;
	}

    public Vector3 GetAnchorPlaneSize(Bounds bounds)
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

        return bounds.size - Vector3.Scale(bounds.size, planeNormal) + planeNormal * .01f;
    }

    public Vector3 GetAnchorPositionWorld(Bounds bounds)
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

        return bounds.center + direction * Mathf.Abs(Vector3.Dot(direction, bounds.extents));
    }

    public void OnDrawGizmos()
    {
        Bounds b = CalculateBoundsLocalSpace();
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + b.center, b.size);

        Gizmos.color = new Color(0f, 1f, 1f, .5f);
        Gizmos.DrawCube(transform.position + GetAnchorPositionWorld(b), GetAnchorPlaneSize(b));
    }

#endif

    /// <summary>
    /// Calculates the bounding volume in local space, considering its child item references
    /// </summary>
    public Bounds CalculateBoundsLocalSpace(int depth = ItemFactory.ITEM_TREE_MAX_DEPTH)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);

        // An item origin IS ALWAYS contained in its bounds
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * .0001f);

        Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;

        // It is important to inverse transform each bounds, because this item may be have an offseted transform...
        // TODO: Can be precalculated
        for (int i = 0; i < renderers.Length; i++)
            bounds.Encapsulate(MathUtils.TransformBounds(ref worldToLocalMatrix, renderers[i].bounds));

        // Because we are traversing a possibly infinite tree, we must be careful with the recursion...
        if (depth > 0)
        {
            ItemReference[] references = gameObject.GetComponentsInChildren<ItemReference>(true);

            for (int i = 0; i < references.Length; i++)
                bounds.Encapsulate(MathUtils.TransformBounds(ref worldToLocalMatrix, references[i].CalculateBoundsWorldSpace(depth - 1)));
        }

        return bounds;
    }

}