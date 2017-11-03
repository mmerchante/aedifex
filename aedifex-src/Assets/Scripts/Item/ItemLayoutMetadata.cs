using UnityEngine;
using System.Collections;

public class ItemLayoutController
{
	private int initialWeight;

	public void Initialize(int initialWeight)
	{
		this.initialWeight = initialWeight;
		OnInitialize();
	}

	protected virtual void OnInitialize()
	{
	}

	public virtual int GetWeight()
	{
		return initialWeight;
	}
}

/// <summary>
/// Metadata container for specific data required by any item layouts.
/// Because the item factory does not know any type, this cannot be easily templatized...
/// </summary>
public abstract class ItemLayoutMetadata : MonoBehaviour
{
	public abstract ItemLayoutController GetController();
}