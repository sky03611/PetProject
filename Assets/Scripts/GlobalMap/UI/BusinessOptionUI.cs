using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BusinessOptionUI : MonoBehaviour
{
    public BusinessType type;
    public TMP_Text businessTitle, businessDescription;
    public Image businessPicture;

    public void SetBusiness (Business business)
    {
        type = business.businessType;
        businessTitle.text = business.GetGenericName();
        businessDescription.text = business.GetGenericDescription();
        businessPicture.sprite = TexturesContainer.Instance.GetBusinessPicture(business.businessType);
    }

    public void OnThisChosen ()
    {
        InterfaceHandler.Instance.ChooseThisOption(this);
    }
}
