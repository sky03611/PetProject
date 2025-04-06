using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MicroWorldNS;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.EventTrigger;

public enum ConfirmationType { NOMONEY, NEWBUSINESS }

public class InterfaceHandler : SerializedSingleton<InterfaceHandler>
{
    public bool isMenuOpen;
    [Header("Dialogue system")]
    [SerializeField] private GameObject dialogueWorld;
    [SerializeField] private Camera clearShot, mainCamera;
    [SerializeField] private CanvasGroup dialogueCanvas;
    [SerializeField] private TMP_Text speakerName, dialogueText;
    [SerializeField] private List<AnswerButton> answerButtons;
    string dialogueName = string.Empty;
    string dialoguePath = string.Empty;
    [Header("Settlement visit")]
    [SerializeField] private CanvasGroup settlementCanvas;
    [SerializeField] private Image settlementBackground;
    [SerializeField] private Sprite villageBackground, townBackground;
    [SerializeField] private TMP_Text settlementType, settlementTitle;
    [SerializeField] private TMP_Text settlementCoffers, settlementFood, settlementRecruits;
    [SerializeField] private Image settlementMoraleImage;
    [SerializeField] private List<Sprite> settlementMoralePictures;
    [SerializeField] private Image settlementModifierImage;
    private TownManager currentSettlement;
    [Header("Settlement's commerce")]
    [SerializeField] private CanvasGroup commerceCanvas;
    [SerializeField] private Transform businessUIParent;
    [SerializeField] private BusinessUI businessUIPrefab;
    [Header("Open a business")]
    public BusinessType currentType;
    [SerializeField] private CanvasGroup openCommerceCanvas;
    [SerializeField] private RectTransform businessUIParentRect;
    [SerializeField] private Transform openUIParent;
    [SerializeField] private BusinessOptionUI businessOptionPrefab;
    [SerializeField] private Image chosenPicture;
    [SerializeField] private TMP_Text chosenTitle, chosenText, businessPriceText;
    [SerializeField] private Transform businessProduction, businessRequirements;
    [SerializeField] private GameObject noProductionText, noRequirementsText;
    [SerializeField] private Image itemPicture;
    [SerializeField] private GameObject openBusinessScrollView;
    [SerializeField] private Scrollbar openBusinessSlider;
    [Header("Business management")]
    [SerializeField] private CanvasGroup businessManagementCanvas;
    [SerializeField] private Image businessArt;
    [SerializeField] private TMP_InputField businessTitleInput;
    [SerializeField] private TMP_Text businessDescriptionText;
    [SerializeField] private TMP_Text businessMoneyText;
    [SerializeField] private Slider profitSplitSlider;
    [SerializeField] private Image levelExperience;
    [SerializeField] private TMP_Text levelExperienceText;
    [SerializeField] private TMP_Text profitSplitLabel;
    [SerializeField] private Transform employeeSlotParent, producedGoodsParent, requiredGoodsParent;
    [SerializeField] private GameObject noProductionTextManagement, noRequirementsTextManagement, noPolicies;
    [SerializeField] private EmployeeUI employeeSlotPrefab;
    private Business currentManagedBusiness;
    [Header("Budget Window")]
    [SerializeField] private CanvasGroup budgetCanvas;
    [SerializeField] private TMP_Text budgetIncomeText;
    [SerializeField] private TMP_Text lastProfitText;
    [SerializeField] private TMP_Text budgetExpensesText;
    [SerializeField] private TMP_Text budgetForecastText;
    [SerializeField] private TMP_Text budgetProfitText;
    [SerializeField] private Transform businessInventory;
    [Header("Confirmation window")]
    [SerializeField] private CanvasGroup notEnoughMoneyCanvas;
    [SerializeField] private TMP_Text confirmationTitle, confirmationText;
    [SerializeField] private GameObject oneButton, twoButtons; //only yes or yes and no
    [SerializeField] private ConfirmationType confirmationType;
    [Header("Trading interface")]
    [SerializeField] private CanvasGroup tradingCanvas;
    [SerializeField] private Transform yourInventory, tradersInventory;
    [SerializeField] private InventoryItemUI itemPrefab;
    [SerializeField] private TMP_Text yourMoney, tradersMoney;
    [SerializeField] private TMP_Text dealResult, noMoney;
    [SerializeField] private Inventory justBoughtItems = new Inventory();
    private VillagerScript trader = null;
    private int tradeBalance = 0;
    [SerializeField] private Inventory temporaryInventory;
    [Header("Trading deal confirmation")]
    [SerializeField] private CanvasGroup tradingConfirmation;
    [Header("Tooltip info")]
    [SerializeField] private PopupScript popUp;
    [SerializeField] private PopupItemScript popUpItem;
    [SerializeField] private float popTimer = 30;
    [SerializeField] private bool popUI = false;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (dialogueCanvas.gameObject.activeSelf)
        {
            dialogueText.text = DialogueManager.Instance.currentDialogue;
        }
        if (!IsPointerOverUIElement())
        {
            if (popUI)
            {
                if (popTimer <= 0)
                {
                    ClosePopup();
                    ClosePopupItem();
                    popUI = false;
                }
                else
                {
                    popTimer -= Time.unscaledDeltaTime * 30;
                }
            }
        }
    }

    public void ActivateDialogue (Dialogue dialogue)
    {
        if (DialogueManager.Instance.currentSpeaker != null)
            speakerName.text = L.G(DialogueManager.Instance.currentSpeaker.dialogueName) + string.Format(" ({0})", DialogueManager.Instance.currentSpeaker.GetFaction().name);
        else
            speakerName.text = L.G(DialogueManager.Instance.currentBusiness.speaker);
        dialogueWorld.SetActive(true);
        mainCamera.enabled = false;
        clearShot.gameObject.SetActive(true);
        dialogueName = DialogueManager.Instance.dialogueName;
        dialoguePath = DialogueManager.Instance.dialoguePath;
        dialogueCanvas.gameObject.SetActive(true);
        foreach (var button in answerButtons)
        {
            button.button.SetActive(false);
        }
        HandleAnswerButton(0, dialogue.answer1, answerButtons[0]);
        HandleAnswerButton(1, dialogue.answer2, answerButtons[1]);
        HandleAnswerButton(2, dialogue.answer3, answerButtons[2]);
        HandleAnswerButton(3, dialogue.answer4, answerButtons[3]);
    }

    private void HandleAnswerButton(int index, Answer answer, AnswerButton answerButton)
    {
        if (answer.answerText == null)
            return;
        if (answer.answerText.Length < 1)
            return;
        answerButton.button.SetActive(true);
        answerButton.text.text = L.G(answer.answerText);
        HandleAnswerAction(answer.action, answer.check, ref answerButton);
        answerButton.answerFolder = answer.answerFolder;
        answerButton.answerDialogue = answer.answerDialogue;
        answerButton.negativeAnswerFolder = answer.negativeAnswerFolder;
        answerButton.negativeAnswerDialogue = answer.negativeAnswerDialogue;
    }

    private void HandleAnswerAction(AnswerActions action, DialogueCheck check, ref AnswerButton answerButton)
    {
        answerButton.action = action;
        answerButton.check = check;
    }

    public void DeactivateDialogue()
    {
        dialogueWorld.SetActive(false);
        mainCamera.enabled = true;
        clearShot.gameObject.SetActive(false);
        DialogueManager.Instance.isDialogueOpen = false;
        DialogueManager.Instance.dialogue = null;
        DialogueManager.Instance.currentSpeaker = null;
        dialogueCanvas.gameObject.SetActive(false);
        if (DialogueManager.Instance.HasCurrentBusiness())
        {
            OnCommerceWindowOpen(true);
            DialogueManager.Instance.currentBusiness = null;
        }
    }

    public void AnswerPressed (int answerID)
    {
        if (answerButtons[answerID].check != DialogueCheck.NONE)
        {
            switch (answerButtons[answerID].check)
            {
                case DialogueCheck.MONEY:
                    if (PlayerController.Instance.HasEnoughMoney(DialogueManager.Instance.currentBusiness.GetBusinessPrice()))
                    {
                        SuccessfulCheck(answerID);
                    }
                    else
                    {
                        FailedCheck(answerID);
                    }
                    break;
                case DialogueCheck.INFLUENCE:
                    SuccessfulCheck(answerID);
                    break;
            }
        }
        else
        {
            SuccessfulCheck(answerID);
        }
    }

    private void SuccessfulCheck(int answerID)
    {
        if (answerButtons[answerID].action != AnswerActions.NONE)
        {
            switch (answerButtons[answerID].action)
            {
                case AnswerActions.TRADE:
                    TradeWithVillager(DialogueManager.Instance.currentSpeaker);
                    break;
                case AnswerActions.BUYBUSINESS:
                    PlayerController.Instance.ChangeMoney(-DialogueManager.Instance.currentBusiness.GetBusinessPrice());
                    DialogueManager.Instance.currentBusiness.belongsToPlayer = true;
                    AnswerDialogue(answerID);
                    break;
            }
        }
        else
        {
            AnswerDialogue(answerID);
        }
    }

    private void AnswerDialogue (int answerID)
    {
        try
        {
            if (answerButtons[answerID].answerFolder != null && answerButtons[answerID].answerFolder.Length > 0)
            {
                DialogueManager.Instance.LoadDialogue(dialoguePath + string.Format("/{0}/{1}", answerButtons[answerID].answerFolder, answerButtons[answerID].answerDialogue));
            }
            else
            {
                DialogueManager.Instance.LoadDialogue(dialoguePath + string.Format("/{0}", answerButtons[answerID].answerDialogue));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            DeactivateDialogue();
        }
    }

    private void FailedCheck(int answerID)
    {
        try
        {
            if (answerButtons[answerID].negativeAnswerFolder != null && answerButtons[answerID].negativeAnswerFolder.Length > 0)
            {
                DialogueManager.Instance.LoadDialogue(dialoguePath + string.Format("/{0}/{1}", answerButtons[answerID].negativeAnswerFolder, answerButtons[answerID].negativeAnswerDialogue));
            }
            else
            {
                DialogueManager.Instance.LoadDialogue(dialoguePath + string.Format("/{0}", answerButtons[answerID].negativeAnswerDialogue));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            DeactivateDialogue();
        }
    }

    public void OnSettlementVisit (TownManager settlement)
    {
        if (isMenuOpen)
            return;
        if (settlement.isVisited)
            return;
        currentSettlement = settlement;
        settlement.OnVisit();
        isMenuOpen = true;
        settlementCanvas.gameObject.SetActive(true);
        settlementCanvas.DOFade(1, 0.75f).SetUpdate(true);
        settlementBackground.sprite = currentSettlement.IsVillage() ? villageBackground : townBackground;
        settlementTitle.text = settlement.thisTown.name;
        settlementType.text = settlement.GetSettlementType();
        settlementCoffers.text = settlement.thisTown.Coffers.ToString();
        settlementFood.text = settlement.thisTown.GetFood().ToString();
        if (settlement.thisTown.HasModifier())
        {
            settlementModifierImage.gameObject.SetActive(true);
            settlementModifierImage.sprite = TexturesContainer.Instance.GetModifier(settlement.thisTown.modifier.modifierName);
        }
        else
        {
            settlementModifierImage.gameObject.SetActive(false);
        }
        settlementMoraleImage.sprite = settlementMoralePictures[settlement.thisTown.GetMorale()];
    }

    public void OnSettlementLeave()
    {
        currentSettlement = null;
        isMenuOpen = false;
        settlementCanvas.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() =>
        {
            settlementCanvas.gameObject.SetActive(false);
        });
    }

    public void TradeWithVillager (VillagerScript _trader)
    {
        trader = _trader;
        isMenuOpen = true;
        DeactivateDialogue();
        OnTradeWindowOpen();
    }

    public void OnTradeWindowOpen ()
    {
        temporaryInventory = new Inventory(PlayerController.Instance.GetInventory());
        tradingCanvas.gameObject.SetActive(true);
        tradingCanvas.DOFade(1, 0.75f).SetUpdate(true);
        yourMoney.text = PlayerController.Instance.GetCurrentMoney().ToString();
        if (trader != null)
        {
            tradersMoney.text = trader.GetMoney().ToString();
        }
        else
        {
            tradersMoney.text = currentSettlement.thisTown.Coffers.ToString();
            settlementCanvas.gameObject.SetActive(false);
        }
        dealResult.gameObject.SetActive(false);
        noMoney.gameObject.SetActive(false);
        RefreshTrading();
    }

    public void OnTradeWindowClosed()
    {
        tradeBalance = 0;
        tradingConfirmation.gameObject.SetActive(false);
        tradingCanvas.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() =>
        {
            yourInventory.DestroyAllChildrenImmediate();
            tradersInventory.DestroyAllChildrenImmediate();
            tradingCanvas.gameObject.SetActive(false);
            if (temporaryInventory != null)
            {
                if (trader != null)
                {
                    foreach (var item in temporaryInventory.items)
                    {
                        trader.thisInventory.AddItem(item);
                    }
                }
                else
                {
                    if (currentSettlement != null)
                    {
                        foreach (var item in temporaryInventory.items)
                        {
                            currentSettlement.thisTown.inventory.AddItem(item);
                        }
                    }
                }
                temporaryInventory = new Inventory();
            }
            justBoughtItems = new Inventory();
        });
        if (trader != null)
        {
            trader = null;
            isMenuOpen = false;
        }
        if (currentSettlement != null)
        {
            settlementCanvas.gameObject.SetActive(true);
        }
    }

    public void OnItemClicked (InventoryItem item)
    {
        int _amount = 1;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            _amount = Mathf.Min(5, item.amount);
        }
        if (trader != null)
        {
            if (item.owner == trader.gameObject)
            {
                temporaryInventory.AddItem(item, _amount, null, trader.GetPriceForItem(item));
                trader.thisInventory.RemoveItem(item, _amount);
                tradeBalance -= trader.GetPriceForItem(item) * _amount;
                justBoughtItems.AddItem(item, _amount, null, trader.GetPriceForItem(item));
                RefreshTrading();
            }
            else
            {
                //if we've just bought this item we sell it at the same price tag
                if (justBoughtItems.HasItem(item.itemName))
                {
                    tradeBalance += trader.GetPriceForItem(item, false) * _amount;
                    justBoughtItems.RemoveItem(item, _amount);
                }
                else
                {
                    tradeBalance += trader.GetPriceForItem(item, true) * _amount;
                }
                trader.thisInventory.AddItem(item, _amount);
                temporaryInventory.RemoveItem(item, _amount);
                RefreshTrading();
            }
        }
        else
        {
            if (item.owner == currentSettlement.gameObject)
            {
                temporaryInventory.AddItem(item, _amount, null, currentSettlement.GetPriceForItem(item));
                currentSettlement.thisTown.inventory.RemoveItem(item, _amount);
                tradeBalance -= currentSettlement.GetPriceForItem(item) * _amount;
                justBoughtItems.AddItem(item, _amount, null, currentSettlement.GetPriceForItem(item));
                RefreshTrading();
            }
            else
            {
                //if we've just bought this item we sell it at the same price tag
                if (justBoughtItems.HasItem(item.itemName))
                {
                    tradeBalance += currentSettlement.GetPriceForItem(item, false) * _amount;
                    justBoughtItems.RemoveItem(item, _amount);
                    List<InventoryItem> toRemove = new List<InventoryItem>();
                    foreach (var j in justBoughtItems.items)
                    {
                        if (j.toRemove)
                        {
                            toRemove.Add(item);
                        }
                    }
                    foreach (var j in toRemove)
                    {
                        justBoughtItems.RemoveItemImmediately(j);
                    }
                }
                else
                {
                    tradeBalance += currentSettlement.GetPriceForItem(item, true) * _amount;
                }
                currentSettlement.thisTown.inventory.AddItem(item, _amount);
                temporaryInventory.RemoveItem(item, _amount);
                RefreshTrading();
            }
        }
    }

    private void RefreshTrading()
    {
        dealResult.gameObject.SetActive(tradeBalance != 0);
        if (tradeBalance < 0)
        {
            yourMoney.text = PlayerController.Instance.GetCurrentMoney() + string.Format("<color=red>{0}</color>", tradeBalance);
            if (trader != null)
            {
                tradersMoney.text = trader.GetMoney() + string.Format("<color=yellow>+{0}</color>", Mathf.Abs(tradeBalance));
            }
            else
            {
                tradersMoney.text = currentSettlement.thisTown.Coffers + string.Format("<color=yellow>+{0}</color>", Mathf.Abs(tradeBalance));
            }
            dealResult.text = L.G(string.Format("You will spend <color=yellow>{0} coins</color> as a result of this deal.", Mathf.Abs(tradeBalance)));
            if (PlayerController.Instance.GetCurrentMoney() < Mathf.Abs(tradeBalance))
            {
                noMoney.gameObject.SetActive(true);
                noMoney.text = L.G("You don't have enough money!");
            }
        }
        else
        {
            if (tradeBalance != 0)
            {
                yourMoney.text = PlayerController.Instance.GetCurrentMoney() + string.Format("<color=yellow>+{0}</color>", Mathf.Abs(tradeBalance));
                dealResult.text = L.G(string.Format("You will gain <color=yellow>{0} coins</color> as a result of this deal.", tradeBalance));
                if (trader != null)
                {
                    tradersMoney.text = trader.GetMoney() + string.Format("<color=red>-{0}</color>", tradeBalance);
                    if (trader.GetMoney() < tradeBalance)
                    {
                        noMoney.gameObject.SetActive(true);
                        noMoney.text = L.G("The trader doesn't have enough money!");
                    }
                }
                else
                {
                    tradersMoney.text = currentSettlement.thisTown.Coffers + string.Format("<color=red>-{0}</color>", tradeBalance);
                    if (currentSettlement.thisTown.Coffers < tradeBalance)
                    {
                        noMoney.gameObject.SetActive(true);
                        noMoney.text = L.G("The trader doesn't have enough money!");
                    }
                }
            }
            else
            {
                yourMoney.text = PlayerController.Instance.GetCurrentMoney().ToString();
                if (trader != null)
                    tradersMoney.text = trader.GetMoney().ToString();
                else
                    tradersMoney.text = currentSettlement.thisTown.Coffers.ToString();
                dealResult.gameObject.SetActive(false);
                noMoney.gameObject.SetActive(false);
            }
        }
        yourInventory.DestroyAllChildrenImmediate();
        tradersInventory.DestroyAllChildrenImmediate();
        ProcessInventoryItems(temporaryInventory, yourInventory);
        if (trader != null)
            ProcessInventoryItems(trader.GetInventory(), tradersInventory);
        else
            ProcessInventoryItems(currentSettlement.thisTown.inventory, tradersInventory);
    }

    private void ProcessInventoryItems(Inventory inventory, Transform inventoryParent)
    {
        List<InventoryItem> toRemove = new List<InventoryItem>();

        foreach (var item in inventory.items)
        {
            if (item.amount > 0)
            {
                var i = Instantiate(itemPrefab, inventoryParent);
                i.Initialize(item);
            }
            else
            {
                toRemove.Add(item);
            }
        }

        foreach (var item in toRemove)
        {
            inventory.RemoveItemImmediately(item);
        }
    }

    private bool TraderNotEnoughMoney()
    {
        if (trader != null)
        {
            return trader.GetMoney() < tradeBalance;
        }
        else
        {
            return currentSettlement.thisTown.Coffers < tradeBalance;
        }
    }

    public void TradeButtonPressed()
    {
        if (tradeBalance < 0 && PlayerController.Instance.GetCurrentMoney() < Mathf.Abs(tradeBalance))
        {
            TextBlink(noMoney, Color.red);
            return;
        }
        if (tradeBalance > 0 && TraderNotEnoughMoney())
        {
            TradeConfirmWindow();
            return;
        }
        OnTradeConfirm();
    }

    private void TextBlink(TMP_Text text, Color color)
    {
        text.DOKill();
        text.DOColor(color, 0.25f).SetUpdate(true).OnComplete(() =>
        {
            text.DOColor(color, 0.15f).SetUpdate(true).OnComplete(() =>
            {
                text.DOColor(Color.white, 0.25f).SetUpdate(true);
            });
        });
    }

    private void TradeConfirmWindow()
    {
        tradingConfirmation.gameObject.SetActive(true);
        tradingConfirmation.DOFade(1, 0.75f).SetUpdate(true);
    }

    public void OnTradeConfirm()
    {
        tradingConfirmation.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() => tradingConfirmation.gameObject.SetActive(false));
        if (tradeBalance > 0)
        {
            PlayerController.Instance.ChangeMoney(tradeBalance);
            if (trader != null)
            {
                trader.ChangeMoney(-tradeBalance);
            }
            else
            {
                currentSettlement.thisTown.ChangeCoffers(-tradeBalance);
            }
            TextBlink(yourMoney, Color.yellow);
            TextBlink(tradersMoney, Color.red);
        }
        else
        {
            PlayerController.Instance.ChangeMoney(tradeBalance);
            if (trader != null)
            {
                trader.ChangeMoney(Mathf.Abs(tradeBalance));
            }
            else
            {
                currentSettlement.thisTown.ChangeCoffers(Mathf.Abs(tradeBalance));
            }
            TextBlink(yourMoney, Color.red);
            TextBlink(tradersMoney, Color.yellow);
        }
        PlayerController.Instance.SetInventory(temporaryInventory);
        temporaryInventory = new Inventory(PlayerController.Instance.GetInventory());
        yourInventory.DestroyAllChildrenImmediate();
        tradersInventory.DestroyAllChildrenImmediate();
        ProcessInventoryItems(temporaryInventory, yourInventory);
        if (trader != null)
        {
            ProcessInventoryItems(trader.GetInventory(), tradersInventory);
            tradersMoney.text = trader.GetMoney().ToString();
        }
        else
        {
            ProcessInventoryItems(currentSettlement.thisTown.inventory, tradersInventory);
            tradersMoney.text = currentSettlement.thisTown.Coffers.ToString();
        }
        tradeBalance = 0;
        yourMoney.text = PlayerController.Instance.GetCurrentMoney().ToString();
        dealResult.gameObject.SetActive(false);
        noMoney.gameObject.SetActive(false);
        justBoughtItems = new Inventory();
    }

    public void OnTradeBack()
    {
        tradingConfirmation.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() =>
        {
            tradingConfirmation.gameObject.SetActive(false);
        });
    }

    public void OnCommerceWindowOpen(bool _immediate = false)
    {
        isMenuOpen = true;
        commerceCanvas.gameObject.SetActive(true);
        settlementCanvas.gameObject.SetActive(false);
        commerceCanvas.DOFade(1, 0.75f).SetUpdate(true);
        if (_immediate)
            commerceCanvas.DOFade(1, 0).SetUpdate(true);
        else
            commerceCanvas.DOFade(1, 0.75f).SetUpdate(true);
        businessUIParent.transform.DestroyAllChildrenImmediate();
        var businesses = currentSettlement.thisTown.GetBusinesses();
        for (int i = 0; i < (currentSettlement.IsVillage() ? 2: 4); i++)
        {
            if (i < businesses.Count)
            {
                var b = Instantiate(businessUIPrefab, businessUIParent);
                b.SetBusiness(businesses[i], i);
            }
            else
            {
                var b = Instantiate(businessUIPrefab, businessUIParent);
                b.SetEmpty(currentSettlement);
            }
        }
    }

    public void OnCommerceWindowClosed(bool _closeVillage = false)
    {
        if (!_closeVillage)
        {
            settlementCanvas.gameObject.SetActive(true);
        }
        commerceCanvas.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() =>
        {
            commerceCanvas.gameObject.SetActive(false);
        });
    }

    public void OnNewBusinessWindowOpen()
    {
        isMenuOpen = true;
        commerceCanvas.gameObject.SetActive(false);
        openCommerceCanvas.gameObject.SetActive(true);
        openCommerceCanvas.DOFade(1, 0.75f).SetUpdate(true);
        openUIParent.transform.DestroyAllChildrenImmediate();
        var businesses = currentSettlement.GetSuitableBusinesses();
        for (int i = 0; i < businesses.Count; i++)
        {
            var b = Instantiate(businessOptionPrefab, openUIParent);
            b.SetBusiness(businesses[i]);
            if (i == 0)
            {
                ChooseThisOption(b);
            }
        }
        openBusinessScrollView.SetActive(businesses.Count > 3);
        openBusinessSlider.value = 0;
        float cardWidth = 150;
        float spacing = 5;
        int count = businesses.Count;

        float totalWidth = count * cardWidth + (count - 1) * spacing;
        businessUIParentRect.sizeDelta = new Vector2(totalWidth, businessUIParentRect.sizeDelta.y);

    }

    public void OnNewBusinessWindowClosed(bool _refreshBusinessList = false)
    {
        if (_refreshBusinessList)
        {
            OnCommerceWindowOpen(true);
        }
        else
        {
            commerceCanvas.gameObject.SetActive(true);
        }
        openCommerceCanvas.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() =>
        {
            openCommerceCanvas.gameObject.SetActive(false);
            businessProduction.DestroyAllChildrenImmediate();
            businessRequirements.DestroyAllChildrenImmediate();
        });
    }

    public void OnConfirmPressed()
    {
        if (PlayerController.Instance.HasEnoughMoney(JsonReader.Instance.GetBusiness(currentType).GetBusinessPrice(true)))
        {
            ShowConfirmWindow(ConfirmationType.NEWBUSINESS);
        }
        else
        {
            ShowConfirmWindow(ConfirmationType.NOMONEY);
        }
    }

    public void ShowConfirmWindow(ConfirmationType type, bool isShowing = true)
    {
        confirmationType = type;
        notEnoughMoneyCanvas.DOKill();
        if (isShowing)
        {

            switch (type)
            {
                case ConfirmationType.NEWBUSINESS:
                    SetConfirmationUI("Opening new business", "Are you sure you want to open this business?", useOneButton: false);
                    break;

                case ConfirmationType.NOMONEY:
                    SetConfirmationUI("Not enough money!", "You don't have enough money to open this business now.", useOneButton: true);
                    break;
            }
            notEnoughMoneyCanvas.gameObject.SetActive(true);
            notEnoughMoneyCanvas.DOFade(1, 0.5f).SetUpdate(true);
        }
        else
        {
            notEnoughMoneyCanvas.DOFade(0, 0.25f).SetUpdate(true).OnComplete(() =>
            {
                notEnoughMoneyCanvas.gameObject.SetActive(false);
            });
        }
    }

    private void SetConfirmationUI(string title, string message, bool useOneButton)
    {
        confirmationTitle.text = L.G(title);
        confirmationText.text = L.G(message);
        oneButton.gameObject.SetActive(useOneButton);
        twoButtons.gameObject.SetActive(!useOneButton);
    }

    public void ConfirmWindowPressed(bool confirmed)
    {
        if (confirmationType == ConfirmationType.NEWBUSINESS && confirmed)
        {
            var business = JsonReader.Instance.GetBusiness(currentType);
            PlayerController.Instance.ChangeMoney(-business.GetBusinessPrice(true));
            currentSettlement.OpenNewBusiness(currentType);
            OnNewBusinessWindowClosed(true);
        }

        ShowConfirmWindow(ConfirmationType.NOMONEY, isShowing: false);
    }


    public void ChooseThisOption (BusinessOptionUI option)
    {
        currentType = option.type;
        chosenPicture.sprite = option.businessPicture.sprite;
        chosenTitle.text = option.businessTitle.text;
        chosenText.text = option.businessDescription.text;
        businessPriceText.text = JsonReader.Instance.GetBusiness(currentType).GetBusinessPrice(true).ToString();
        if (PlayerController.Instance.HasEnoughMoney(JsonReader.Instance.GetBusiness(currentType).GetBusinessPrice(true)))
        {
            businessPriceText.color = new Color32(255, 199, 45, 255);
        }
        else
        {
            businessPriceText.color = new Color32(70, 70, 70, 255);
        }
        businessProduction.DestroyAllChildrenImmediate();
        businessRequirements.DestroyAllChildrenImmediate();
        StartCoroutine(UpdateNewBusinessInterface(option));
    }

    private IEnumerator UpdateNewBusinessInterface(BusinessOptionUI option)
    {
        yield return new WaitForEndOfFrame();
        foreach (var entry in BusinessProductionManager.Instance.GetProducibleItems(option.type))
        {
            if (businessProduction.childCount < 4)
            {
                var i = Instantiate(itemPicture, businessProduction);
                i.sprite = TexturesContainer.Instance.GetSprite(entry.outputItemName);
                i.GetComponent<InterfaceElement>().SetText (entry.outputItemName);
            }
        }
        noProductionText.SetActive(businessProduction.transform.childCount < 1);
        if (BusinessProductionManager.Instance.GetRequiredItems(option.type) == null)
        {
            foreach (var e in JsonReader.Instance.GetBusiness(option.type).GetNecessaryItems().Split(','))
            {
                if (businessRequirements.childCount < 4)
                {
                    var i = Instantiate(itemPicture, businessRequirements);
                    i.sprite = TexturesContainer.Instance.GetSprite(e);
                    i.GetComponent<InterfaceElement>().SetText(e);
                }
            }
        }
        else
        {
            foreach (var entry in BusinessProductionManager.Instance.GetRequiredItems(option.type))
            {
                if (businessRequirements.childCount < 4)
                {
                    var i = Instantiate(itemPicture, businessRequirements);
                    i.sprite = TexturesContainer.Instance.GetSprite(entry.inputItemName);
                    i.GetComponent<InterfaceElement>().SetText(entry.inputItemName);
                }
            }
        }
        noRequirementsText.SetActive(businessRequirements.transform.childCount < 1);
    }

    public void OnBusinessManagementOpen(Business business)
    {
        currentManagedBusiness = business;
        isMenuOpen = true;
        businessManagementCanvas.gameObject.SetActive(true);
        businessManagementCanvas.DOFade(1f, 0.5f).SetUpdate(true);

        businessArt.sprite = TexturesContainer.Instance.GetBusinessPicture(business.businessType);
        businessTitleInput.text = business.title;
        businessDescriptionText.text = business.description;
        businessMoneyText.text = business.money.ToString();

        levelExperience.fillAmount = business.GetExperiencePercentage();
        levelExperienceText.text = string.Format("Level {0}", business.level);

        profitSplitSlider.value = business.taxPercentage;
        UpdateProfitSplitLabel(business.taxPercentage);

        profitSplitSlider.onValueChanged.RemoveAllListeners();
        profitSplitSlider.onValueChanged.AddListener(val =>
        {
            business.taxPercentage = val;
            UpdateProfitSplitLabel(val);
        });

        employeeSlotParent.DestroyAllChildrenImmediate();
        foreach (var emp in business.employees)
        {
            var slot = Instantiate(employeeSlotPrefab, employeeSlotParent);
            slot.SetEmployee(emp, business);
        }

        noPolicies.SetActive(business.thisTown.IsVillage());

        producedGoodsParent.DestroyAllChildrenImmediate();
        requiredGoodsParent.DestroyAllChildrenImmediate();

        foreach (var entry in BusinessProductionManager.Instance.GetProducibleItems(currentManagedBusiness.businessType))
        {
            if (producedGoodsParent.childCount < 4)
            {
                var i = Instantiate(itemPicture, producedGoodsParent);
                i.sprite = TexturesContainer.Instance.GetSprite(entry.outputItemName);
                i.GetComponent<InterfaceElement>().SetText(entry.outputItemName);
            }
        }
        noProductionTextManagement.SetActive(producedGoodsParent.transform.childCount < 1);
        if (BusinessProductionManager.Instance.GetRequiredItems(currentManagedBusiness.businessType) == null)
        {
            foreach (var e in currentManagedBusiness.GetNecessaryItems().Split(','))
            {
                if (requiredGoodsParent.childCount < 4)
                {
                    var i = Instantiate(itemPicture, requiredGoodsParent);
                    i.sprite = TexturesContainer.Instance.GetSprite(e);
                    i.GetComponent<InterfaceElement>().SetText(e);
                }
            }
        }
        else
        {
            foreach (var entry in BusinessProductionManager.Instance.GetRequiredItems(currentManagedBusiness.businessType))
            {
                if (requiredGoodsParent.childCount < 4)
                {
                    var i = Instantiate(itemPicture, requiredGoodsParent);
                    i.sprite = TexturesContainer.Instance.GetSprite(entry.inputItemName);
                    i.GetComponent<InterfaceElement>().SetText(entry.inputItemName);
                }
            }
        }
        noRequirementsTextManagement.SetActive(requiredGoodsParent.transform.childCount < 1);
    }

    private void UpdateProfitSplitLabel(float val)
    {
        float toPlayer = (1f - val) * 100f;
        float toBusiness = val * 100f;
        profitSplitLabel.text = $"<color=yellow>{toPlayer:0}%</color> to player / <color=grey>{toBusiness:0}%</color> reinvested";
    }

    public void OnBusinessManagementClosed()
    {
        currentManagedBusiness.title = businessTitleInput.text;
        currentManagedBusiness = null;
        OnCommerceWindowOpen(true);
        businessManagementCanvas.DOFade(0.5f, 0.5f).SetUpdate(true).OnComplete(() =>
        {
            businessManagementCanvas.gameObject.SetActive(false);
        });
    }

    public void OnBudgetButtonPressed()
    {
        if (currentManagedBusiness == null)
            return;

        Business business = currentManagedBusiness;
        TownManager town = business.thisTown;
        var productionProfile = BusinessProductionManager.Instance.GetProfile(business.businessType);

        float efficiency = business.GetProductionMultiplier();
        int cap = Mathf.CeilToInt(business.productionCap * efficiency);
        int totalExpenses = business.GetTotalEmployeeSalary();
        int materialCosts = 0;
        int projectedIncome = 0;

        if (productionProfile != null)
        {
            foreach (var item in productionProfile.producibleItems)
            {
                bool hasEnoughResources = true;

                foreach (var req in item.requiredResources)
                {
                    InventoryItem available = town.thisTown.inventory.GetItem(req.inputItemName);
                    if (available == null || available.amount < req.inputAmount * cap)
                    {
                        hasEnoughResources = false;
                        break;
                    }
                }

                if (!hasEnoughResources)
                    continue;

                foreach (var req in item.requiredResources)
                {
                    InventoryItem template = JsonReader.Instance.GetItemByName(req.inputItemName);
                    int unitPrice = town.GetPriceForItem(template);
                    materialCosts += unitPrice * req.inputAmount * cap;
                }

                if (business.producesProducts)
                {
                    InventoryItem result = JsonReader.Instance.GetItemByName(item.outputItemName);
                    if (result != null)
                    {
                        int price = town.GetPriceForItem(result);
                        projectedIncome += price * item.outputAmount * cap;
                    }
                }
            }

            if (!business.producesProducts)
            {
                projectedIncome = business.profitPerItem * cap;
            }
        }
        else
        {
            projectedIncome = business.profitPerItem * cap;
        }

        totalExpenses += materialCosts;
        int projectedProfit = projectedIncome - totalExpenses;

        budgetIncomeText.text = string.Format("<color=#FFC72D>+{0}</color>", projectedIncome);
        lastProfitText.text = currentManagedBusiness.lastProfit >= 0
            ? string.Format("<color=#FFC72D>+{0}</color>", currentManagedBusiness.lastProfit)
            : string.Format("<color=red>{0}</color>", currentManagedBusiness.lastProfit);
        budgetExpensesText.text = string.Format("<color=red>-{0}</color>", totalExpenses);
        budgetForecastText.text = cap.ToString();
        budgetProfitText.text = projectedProfit >= 0
            ? string.Format("<color=#FFC72D>+{0}</color>", projectedProfit)
            : string.Format("<color=red>{0}</color>", projectedProfit);

        businessInventory.DestroyAllChildrenImmediate();
        foreach (var item in currentManagedBusiness.thisInventory.items)
        {
            if (item.amount > 0)
            {
                var i = Instantiate(itemPrefab, businessInventory);
                i.ignoreClick = true;
                i.Initialize(item);
            }
        }

        budgetCanvas.gameObject.SetActive(true);
        budgetCanvas.DOFade(1, 0.75f).SetUpdate(true);
    }

    public void CloseBudgetWindow()
    {
        budgetCanvas.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() =>
        {
            budgetCanvas.gameObject.SetActive(false);
        });
    }

    public void SetPopUI()
    {
        popTimer = 5;
        popUI = true;
    }

    public void OpenPopupItem (InventoryItem item, float _sizeX, float _sizeY, bool _right, InterfaceElement toOverview)
    {
        popUpItem.gameObject.SetActive(true);
        popTimer = 5;
        popUI = true;
        if (toOverview.GetType() == ElementType.PRODUCED)
        {
            popUpItem.InitializePopup(item, -1, _right, _sizeX, _sizeY, toOverview);
            return;
        }
        if (trader != null)
        {
            if (item.owner == trader.gameObject)
            {
                popUpItem.InitializePopup(item, trader.GetPriceForItem(item), _right, _sizeX, _sizeY, toOverview);
            }
            else
            {
                if (justBoughtItems.HasItem (item.itemName))
                {
                    popUpItem.InitializePopup(item, trader.GetPriceForItem(item, false), _right, _sizeX, _sizeY, toOverview);
                }
                else
                {
                    popUpItem.InitializePopup(item, trader.GetPriceForItem(item, true), _right, _sizeX, _sizeY, toOverview);
                }
            }
        }
        else
        {
            if (item.owner == currentSettlement.gameObject)
            {
                popUpItem.InitializePopup(item, currentSettlement.GetPriceForItem(item), _right, _sizeX, _sizeY, toOverview);
            }
            else
            {
                if (justBoughtItems.HasItem(item.itemName))
                {
                    popUpItem.InitializePopup(item, currentSettlement.GetPriceForItem(item, false), _right, _sizeX, _sizeY, toOverview);
                }
                else
                {
                    popUpItem.InitializePopup(item, currentSettlement.GetPriceForItem(item, true), _right, _sizeX, _sizeY, toOverview);
                }
            }
        }
    }

    public void OpenPopup(string _text, float _sizeX, float _sizeY, bool _right, InterfaceElement toOverview)
    {
        popUp.gameObject.SetActive(true);
        switch (toOverview.GetType())
        {
            case ElementType.MODIFIER:
                if (currentSettlement != null)
                {
                    if (currentSettlement.thisTown.modifier != null)
                        _text = L.G(currentSettlement.thisTown.modifier.modifierDescription);
                }
                break;
            case ElementType.MORALE:
                _text = L.G(currentSettlement.GetMorale().description);
                break;
        }
        popUp.InitializePopup(_text, _right, _sizeX, _sizeY, toOverview);
        popTimer = 5;
        popUI = true;
    }

    public void ClosePopup(float _duration = 0.15f)
    {
        popUI = false;
        popUp.TurnOff(_duration);
    }

    public void ClosePopupItem (float _duration = 0.15f)
    {
        popUI = false;
        popUpItem.TurnOff(_duration);
    }

    public bool IsPointerOverUIElement()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            return false;
        if (popUp.gameObject.activeSelf)
        {
            return popUp.IsPopupOverUI();
        }
        if (popUpItem.gameObject.activeSelf)
        {
            return popUpItem.IsPopupOverUI();
        }
        return false;
    }
}

public enum AnswerActions { NONE, TRADE, FIGHT, BUYBUSINESS }

[System.Serializable]
public class AnswerButton
{
    public GameObject button;
    public TMP_Text text;
    public string answerFolder = "";
    public string negativeAnswerFolder = "";
    public string answerDialogue = "";
    public string negativeAnswerDialogue = "";
    public AnswerActions action;
    public DialogueCheck check;
}
