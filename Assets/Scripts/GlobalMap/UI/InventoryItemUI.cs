using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    public bool ignoreClick = false;
    public InventoryItem item;
    public Image itemImage;
    public TMP_Text amount;

    public void Initialize (InventoryItem _item)
    {
        item = _item;
        amount.gameObject.SetActive(item.amount > 1);
        amount.text = item.amount.ToString();
        itemImage.sprite = item.sprite;
    }

    public void OnClick()
    {
        if (ignoreClick)
            return;
        InterfaceHandler.Instance.OnItemClicked(item);
    }
}
