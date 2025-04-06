using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum ItemType { FOOD, MATERIAL, ARMOR, WEAPON }

[System.Serializable]
public class Inventory
{
    public List<InventoryItem> items = new List<InventoryItem>();
    public int maxAmount = 40;
    public GameObject owner = null;

    public Inventory()
    {

    }

    public void SetOwner (GameObject _owner)
    {
        owner = _owner;
    }

    public Inventory(Inventory other)
    {
        maxAmount = other.maxAmount;

        items = new List<InventoryItem>();
        foreach (InventoryItem item in other.items)
        {
            InventoryItem copiedItem = new InventoryItem(item);
            items.Add(copiedItem);
        }
    }

    public bool HasSpace ()
    {
        if (items == null)
            items = new List<InventoryItem>();
        return items.Count < maxAmount;
    }

    public void AddAllItems(List<InventoryItem> _items, VillagerScript _previousOwner = null)
    {
        foreach (InventoryItem item in _items)
        {
            AddItem(item, 0, _previousOwner);
        }
    }

    public void AddItem (InventoryItem item, int _amount = 0, VillagerScript _previousOwner = null, int _newPrice = 0)
    {
        if (!HasSpace())
        {
            return;
        }
        item.toRemove = false;
        if (items.Find(x => x.itemName == item.itemName) != null)
        {
            if (_amount != 0)
            {
                items.Find(x => x.itemName == item.itemName).ChangeAmount(_amount);
            }
            else
            {
                items.Find(x => x.itemName == item.itemName).ChangeAmount(item.amount);
            }
        }
        else
        {
            var newItem = new InventoryItem(item.itemName, item.itemDescription, false, item.type, item.businessTypess, _amount, item.defaultPrice, TexturesContainer.Instance.GetSprite (item.itemName));
            if (_newPrice != 0)
            {
                newItem.newPrice = _newPrice;
            }
            if (_previousOwner != null)
            {
                newItem.previousOwner = _previousOwner;
            }
            newItem.owner = owner;
            if (_amount != 0)
            {
                items.Add(newItem);
            }
            else
            {
                newItem.amount = item.amount;
                items.Add(newItem);
            }
        }
    }

    public void RemoveItem (InventoryItem item, int _amount = 0)
    {
        if (!HasItem (item.itemName))
        {
            return;
        }
        if (_amount > 0)
        {
            GetItem(item.itemName).ChangeAmount(-_amount);
        }
        else
        {
            GetItem(item.itemName).ChangeAmount(-item.amount);
        }
    }

    public void RemoveItemImmediately(InventoryItem item)
    {
        items.Remove(item);
    }

    public InventoryItem GetRandomItem()
    {
        return items[UnityEngine.Random.Range (0, items.Count)];
    }

    public InventoryItem GiveRandomItem()
    {
        if (items.Count > 0)
        {
            var randomItem = items[Random.Range(0, items.Count)];
            RemoveItem(randomItem, 1);
            return randomItem;
        }
        return null;
    }

    public InventoryItem GetItem (string _itemName)
    {
        return items.Find(x => x.itemName == _itemName);
    }

    public List<InventoryItem> GetAllItemsOfType (ItemType _type)
    {
        return items.FindAll (x => x.type == _type);
    }

    public List<InventoryItem> GetAllItemsOfName(string _name)
    {
        return items.FindAll (x => x.itemName.Equals (_name));
    }

    public bool HasItem (string _itemName)
    {
        return GetItem(_itemName) != null;
    }
}

[System.Serializable]
public class InventoryItem
{
    public string itemName, itemDescription;
    public Sprite sprite;
    public bool toRemove = false;
    public string businessTypess = "";
    public List<BusinessType> businessTypes = new List<BusinessType>();
    public ItemType type;
    public int amount = 1;
    public int defaultPrice, newPrice;
    public VillagerScript previousOwner;
    public GameObject owner = null;

    public bool IsSuitableForBusiness (BusinessType businessType)
    {
        if (businessTypess != "" && businessTypes.Count < 1)
        {
            foreach (var bt in businessTypess.Split(','))
            {
                businessTypes.Add((BusinessType)int.Parse(bt));
            }
        }
        if (businessTypes != null)
            return businessTypes.Contains(businessType);
        return false;
    }

    public InventoryItem()
    {

    }

    public InventoryItem (string itemName, string itemDescription, bool toRemove, ItemType type, string businessTypess, int amount, int defaultPrice, Sprite sprite)
    {
        this.itemName = itemName;
        this.itemDescription = itemDescription;
        this.toRemove = toRemove;
        this.type = type;
        this.businessTypess = businessTypess;
        this.amount = amount;
        this.defaultPrice = defaultPrice;
        this.sprite = sprite;
        businessTypes.Clear();
        if (businessTypess != "")
        {
            foreach (var bt in businessTypess.Split(','))
            {
                businessTypes.Add((BusinessType)int.Parse(bt));
            }
        }
    }

    public InventoryItem(InventoryItem other)
    {
        this.itemName = other.itemName;
        this.itemDescription = other.itemDescription;
        this.toRemove = other.toRemove;
        this.type = other.type;
        this.businessTypess = other.businessTypess;
        this.amount = other.amount;
        this.defaultPrice = other.defaultPrice;
        this.sprite = other.sprite;
        this.previousOwner = other.previousOwner;
        this.owner = other.owner;
        try
        {
            if (businessTypess == null)
            {
                businessTypess = "";
            }
            businessTypes.Clear();
            foreach (var bt in businessTypess.Split(','))
            {
                businessTypes.Add((BusinessType)int.Parse(bt));
            }
        }
        catch
        {

        }
    }

    public void ChangeAmount (int _amount)
    {
        amount += _amount;
        if (amount <= 0)
        {
            toRemove = true;
        }
    }
}