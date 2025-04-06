using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BusinessUI : MonoBehaviour
{
    public int thisID = -1;
    public Business thisBusiness;
    public Image backgroundPicture;
    public Image ownerPicture;
    [SerializeField] private Sprite emptyVillage;
    public TMP_Text businessTitle, businessDescription;
    public TMP_Text openText;
    public Transform productionList;

    public void SetBusiness (Business _business, int _id)
    {
        thisID = _id;
        thisBusiness = _business;
        businessTitle.text = thisBusiness.title;
        businessDescription.text = thisBusiness.description + string.Format("\n\nProfit for the last 24 hours: {0}\nProduction capacity: {1}\nNecessary resources: {2}", thisBusiness.GetProfit(), thisBusiness.productionCap, thisBusiness.GetNecessaryItems());
        backgroundPicture.sprite = TexturesContainer.Instance.GetBusinessPicture(thisBusiness.businessType);
        ownerPicture.gameObject.SetActive(true); 
        if (thisBusiness.belongsToPlayer)
        {
            ownerPicture.sprite = PlayerController.Instance.GetPortrait();
            openText.text = "Manage";
        }
        else
        {
            ownerPicture.sprite = FactionScript.Instance.GetFlag(_business.thisTown.thisTown.GetFaction());
            openText.text = "Talk";
        }
        if (thisBusiness.producesProducts)
        {
            productionList.gameObject.SetActive(false);
        }
        else
        {
            productionList.gameObject.SetActive(false);
        }
    }

    public void SetEmpty(TownManager town)
    {
        thisBusiness = null;
        businessTitle.text = "Empty land";
        businessDescription.text = "This land could be yours. For the right price.";
        ownerPicture.gameObject.SetActive(false);
        productionList.gameObject.SetActive(false);
        thisID = -1;
        if (town.IsVillage())
        {
            backgroundPicture.sprite = emptyVillage;
        }
    }

    public void OnClicked ()
    {
        if (thisID == -1)
        {
            InterfaceHandler.Instance.OnNewBusinessWindowOpen();
            return;
        }
        InterfaceHandler.Instance.OnCommerceWindowClosed(true);
        if (thisBusiness.belongsToPlayer)
        {
            InterfaceHandler.Instance.OnBusinessManagementOpen(thisBusiness);
        }
        else
        {
            DialogueManager.Instance.LoadDialogue(thisBusiness.dialoguePath + "Greetings", thisBusiness);
        }
    }
}
