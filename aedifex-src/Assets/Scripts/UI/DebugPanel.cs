using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script will show stuff and decisions made in runtime
/// </summary>
public class DebugPanel : MonoBehaviour
{
    public Text debugText;

    public void DebugText(string txt)
    {
        debugText.text = txt;
        debugText.gameObject.SetActive(false);
    }

    public void Start()
    {
        HidePanel();
    }

    public void ShowPanel()
    {
        this.gameObject.SetActive(true);
    }

    public void HidePanel()
    {
        this.gameObject.SetActive(false);
    }
}
