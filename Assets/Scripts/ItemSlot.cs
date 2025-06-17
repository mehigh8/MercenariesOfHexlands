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
    private HoverHandler hoverHandler;

    public void SetItem(ItemInfo item)
    {
        this.item = item;
        if (!hoverHandler)
            hoverHandler = GetComponent<HoverHandler>();
        hoverHandler.itemInfo = item;
    }

    public ItemInfo GetItem() { return this.item; }

    private void Awake()
    {
        if (!hoverHandler)
            hoverHandler = GetComponent<HoverHandler>();
        hoverHandler.itemInfo = item;
        hoverHandler.position = transform.position + Vector3.right * GetComponent<RectTransform>().sizeDelta.x / 2;
    }
}
