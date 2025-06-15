using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    public Button button;
    public Image image;
    public bool isEquipment;
    [Tooltip("Only relevant when isEquipment is true")]
    public ItemInfo.EquipmentSlot equipmentType;
    private ItemInfo item = null;

    public void SetItem(ItemInfo item)
    {
        this.item = item;
    }

    public ItemInfo GetItem() { return this.item; }
}
