using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

// This class will contain all elements related to the music edition
public class UIEditorScreen : MonoBehaviour
{

    public void ShowEditorScreen()
    {
        this.gameObject.SetActive(true);
    }

    public void HideEditorScreen()
    {
        this.gameObject.SetActive(false);
    }
}
