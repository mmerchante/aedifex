using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PoolMetaData
{
	public string prefabName;
	public int poolInitialSize;
}

public class PoolManager : MonoBehaviorSingleton<PoolManager>
{
	public const string POOLABLE_OBJECTS_PATH = "PoolableObjects";

	public int defaultInitialSize = 5;

	public PoolMetaData[] metaData;

    private Dictionary<string, Pool> pools = new Dictionary<string,Pool>();
	
    protected override void Initialize()
    {
		GameObject[] prefabs = Resources.LoadAll<GameObject>(POOLABLE_OBJECTS_PATH);
        Pool[] pools = new Pool[prefabs.Length];

        //Add all pools
		for(int i = 0; i < prefabs.Length; i++)
		{
			GameObject poolContainer = new GameObject(prefabs[i].name);
            poolContainer.transform.SetParent(this.transform, false);

			Pool pool = poolContainer.AddComponent<Pool>();
			pool.Initialize(prefabs[i]);

            pools[i] = pool;

			if (this.pools.ContainsKey(pool.name))
				Debug.LogError("Another pool named " + pool.name + " already exists.");
			else
				this.pools[pool.name] = pool;
		}

        //Set all pools initial size (we do this AFTER adding all of them to solve inter-pool-dependencies issues)
        for(int i = 0; i < prefabs.Length; i++)
        {
            Pool pool = pools[i];
            pool.SetInitialSize(GetInitialSizeForPool(pool.name));
        }
    }

	protected int GetInitialSizeForPool(string poolName)
	{
		for(int i = 0; i < metaData.Length; i++)
			if(metaData[i].prefabName == poolName)
				return metaData[i].poolInitialSize;

		return defaultInitialSize;
	}
    
    public Pool GetPool(string name)
    {
		if (pools.ContainsKey(name))
			return pools[name];
		else
			Debug.LogError("There is no pool with that name! It should be only the object's name.");

		return null;
    }
}