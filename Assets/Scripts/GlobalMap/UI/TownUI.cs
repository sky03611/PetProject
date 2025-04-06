using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TownUI : MonoBehaviour
{
    public TMP_Text townName;
    public RectTransform rect;
    public Image flagSR;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Initialize (Town town)
    {
        townName.text = town.name;
        rect = GetComponent<RectTransform>();
    }

    public void SetFaction(Town town)
    {
        flagSR.sprite = FactionScript.Instance.GetFlag(town.GetFaction());
    }
}
