using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ElementType { NONE, GOLD, TRADING, MORALE, MODIFIER, PRODUCED }

public class InterfaceElement : MonoBehaviour
{
    [SerializeField] private bool showGUI;
    [SerializeField] private string text;
    [SerializeField] private string hotkey;
    [SerializeField] private string startingText;
    [SerializeField] private float sizeX = 360, sizeY = 180;
    [SerializeField] private ElementType type;
    [SerializeField] private bool right, up, turnOffWhenBuilding;
    private CanvasGroup CG;
    private RectTransform RT;

    public ElementType GetType()
    {
        return type;
    }

    private void Awake()
    {
        try
        {
            CG = GetComponent<CanvasGroup>();
        }
        catch
        {

        }
        RT = GetComponent<RectTransform>();
        startingText = text;
    }

    public void SetText (string _text)
    {
        startingText = _text.Replace("\r", "");
    }

    public void ShowPopup ()
    {
        if (CG != null && CG.alpha <= 0)
        {
            return;
        }
        right = Input.mousePosition.x < Screen.width / 2;
        if (type == ElementType.TRADING)
        {
            InterfaceHandler.Instance.OpenPopupItem(GetComponent<InventoryItemUI>().item, sizeX, sizeY, right, this);
        }
        else
        {
            if (type == ElementType.PRODUCED)
            {
                InterfaceHandler.Instance.OpenPopupItem(JsonReader.Instance.GetItemByName(startingText), sizeX, sizeY, right, this);
            }
            else
            {
                InterfaceHandler.Instance.OpenPopup(text, sizeX, sizeY, right, this);
            }
        }
    }

    public void MouseOn()
    {
        InterfaceHandler.Instance.SetPopUI();
    }

    public void ClosePopup(float _duration = 0.15f)
    {
        if (type == ElementType.TRADING || type == ElementType.PRODUCED)
        {
            InterfaceHandler.Instance.ClosePopupItem(_duration);
        }
        else
        {
            InterfaceHandler.Instance.ClosePopup(_duration);
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            InterfaceHandler.Instance.ClosePopup(0);
        }
    }
}
