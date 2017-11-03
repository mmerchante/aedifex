using UnityEngine;
using System.Collections;

public class RuntimeItemRoot : MonoBehaviour 
{
	public Bounds itemBounds;

	public void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(itemBounds.center, itemBounds.size);
	}
}