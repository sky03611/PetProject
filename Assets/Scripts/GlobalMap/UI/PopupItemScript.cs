using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class PopupItemScript : PopupScript
{
    [SerializeField] protected Image itemImage;
    [SerializeField] protected TMP_Text itemNameText, priceText;

    public void InitializePopup (InventoryItem item, int currentPrice, bool _right, float _sizeX, float _sizeY, InterfaceElement _toOverview)
    {
        RT.sizeDelta = new Vector2(_sizeX, _sizeY);
        canvasGroup.DOFade(1, 0.15f).SetUpdate(true);
        if (item.sprite != null)
        {
            itemImage.sprite = item.sprite;
        }
        else
        {
            itemImage.sprite = TexturesContainer.Instance.GetSprite(item.itemName);
        }
        itemNameText.text = item.itemName;
        descriptionText.text = item.itemDescription;
        if (currentPrice == -1)
        {
            priceText.gameObject.SetActive(false);
        }
        else
        {
            priceText.gameObject.SetActive(true);
            priceText.text = string.Format("Worth: {0}\nCurrent price: {1}", item.defaultPrice, currentPrice);
        }
        right = _right;
        toOverview = _toOverview;
    }
}
