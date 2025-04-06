using UnityEngine;
using DG.Tweening;
using TMPro;

public class PopupScript : MonoBehaviour
{
    public InterfaceElement toOverview;
    protected Vector2 pos;
    protected RectTransform RT;
    protected bool right, up;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected TMP_Text descriptionText;

    public virtual void OnEnable()
    {
        RT = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual bool IsPopupOverUI()
    {
        return toOverview != null;
    }

    public void InitializePopup (string text, bool _right, float _sizeX, float _sizeY, InterfaceElement _toOverview)
    {
        RT.sizeDelta = new Vector2(_sizeX, _sizeY);
        canvasGroup.DOFade(1, 0.15f).SetUpdate(true);
        descriptionText.text = text;
        right = _right;
        up = Input.mousePosition.y < (Screen.height / 3);
        toOverview = _toOverview;
    }

    public void TurnOff (float _duration)
    {
        canvasGroup.DOKill();
        canvasGroup.DOFade(0, _duration).SetUpdate(true).OnComplete(() => canvasGroup.gameObject.SetActive(false));
        toOverview = null;
    }

    public virtual void Update()
    {
        pos = Input.mousePosition;
        if (Screen.width > 3000)
        {
            if (right)
                transform.position = new Vector2(pos.x + RT.rect.width / 1.6f + RT.rect.width / 5, up ? (pos.y + RT.rect.height) : (pos.y - RT.rect.height / 1.2f));
            else
                transform.position = new Vector2(pos.x - RT.rect.width / 1.6f, up ? (pos.y + RT.rect.height) : (pos.y - RT.rect.height / 1.2f));
        }
        else
        {
            if (right)
                transform.position = new Vector2(pos.x + RT.rect.width / 2 + RT.rect.width / 5, up ? (pos.y + RT.rect.height) : (pos.y - RT.rect.height / 2));
            else
                transform.position = new Vector2(pos.x - RT.rect.width / 2, up ? (pos.y + RT.rect.height) : (pos.y - RT.rect.height / 2));
        }
    }
}
