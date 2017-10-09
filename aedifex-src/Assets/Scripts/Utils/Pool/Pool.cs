using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pool : MonoBehaviour 
{
	private ExtendablePoolGameObject internalPool = null;

	public void Initialize(GameObject prefab)
    {
		if(!prefab)
			Debug.LogError("Could not find prefab", this);
		
		this.transform.position = Vector3.zero;
		this.transform.rotation = Quaternion.identity;
		this.transform.localScale = Vector3.one;

		this.internalPool = new ExtendablePoolGameObject(prefab, this.transform);
    }

    public void SetInitialSize(int initialSize)
    {
        this.internalPool.SetInitialSize(initialSize);
    }

    public PoolGameObject Retrieve()
    {
		return internalPool.Retrieve();
    }

    public void Return(PoolGameObject go)
    {
		internalPool.Return(go);
    }
}