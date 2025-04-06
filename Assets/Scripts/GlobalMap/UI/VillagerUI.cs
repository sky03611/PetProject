using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VillagerUI : MonoBehaviour
{
    public TMP_Text partySize;
    public RectTransform rect;
    public Image flagSR;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void UpdatePartySize(VillagerScript villager)
    {
        partySize.text = villager.partySize.ToString();
        if (villager.IsHostileTowardsPlayer ())
        {
            partySize.color = Color.red;
        }
        else
        {
            partySize.color = Color.white;
        }
        rect = GetComponent<RectTransform>();
        if (villager.type == VillagerType.VILLAGER || villager.type == VillagerType.BANDIT)
        {
            flagSR.enabled = false;
        }
        else
        {
            flagSR.sprite = FactionScript.Instance.GetFlag(villager.GetFaction());
        }
        canvasGroup.alpha = villager.IsVisible() ? 1 : 0;
    }
}