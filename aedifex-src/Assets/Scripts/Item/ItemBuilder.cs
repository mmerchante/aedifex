using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemBuilder : MonoBehaviour 
{
    public string rootItemId = "";
    public Light lightObject;

    private GameObject item;

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space) || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended))
        {
            if (item)
                GameObject.Destroy(item);

            item = ItemFactory.Instance.BuildItem(Quaternion.identity, Camera.main.transform.forward, lightObject.transform.forward, rootItemId).gameObject;
        }
    }
}