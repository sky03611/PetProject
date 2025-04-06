using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; // <-- Needed for pointer events

public class EmployeeUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text roleNameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text salaryText;

    private Business currentBusiness;
    private Employee employee;

    public void SetEmployee(Employee employee, Business business)
    {
        this.employee = employee;
        currentBusiness = business;

        roleNameText.text = L.G(employee.employeeName);
        UpdateUI();
    }

    public void OnHireClicked()
    {
        if (!currentBusiness.CanHire(employee))
            return;
        if (currentBusiness.HireEmployee(employee))
        {
            UpdateUI();
            InterfaceHandler.Instance.SetPopUI();
        }
    }

    public void OnFireClicked()
    {
        if (!currentBusiness.CanFire(employee))
            return;
        if (currentBusiness.FireEmployee(employee))
        {
            UpdateUI();
            InterfaceHandler.Instance.SetPopUI();
        }
    }

    private void UpdateUI()
    {
        countText.text = $"{employee.count}/{employee.maxCount}";
        salaryText.text = $"{employee.salary}g";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnFireClicked();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnHireClicked();
        }
    }
}