using UnityEngine;
using System.Collections;

public class ItemReferenceController
{
	private float initialProbability;

	public void Initialize(float initialProbability)
	{
		this.initialProbability = initialProbability;
	}

	public virtual float GetProbability()
	{
		return initialProbability;
	}
}

/// <summary>
/// Base container for any metadata required for the item.
/// </summary>
public abstract class ItemReferenceMetadata : MonoBehaviour
{
	public abstract ItemReferenceController GetController();
}