using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    [Header("References")]
    public Button button; // Reference to the slot's button
    public Image image; // Reference to the slot's image
    public bool isEquipment; // Bool to specify if this slot is for a specific equipment type only
    [Tooltip("Only relevant when isEquipment is true")]
    public ItemInfo.EquipmentSlot equipmentType; // Specifies the equipment type

    private ItemInfo item = null; // Reference to the item held by this slot
    private HoverHandler hoverHandler; // Reference to the hover handler script

    #region Unity Functions
    private void Awake()
    {
        // Set reference and position for the hover tooltip
        if (!hoverHandler)
            hoverHandler = GetComponent<HoverHandler>();
        hoverHandler.itemInfo = item;
        hoverHandler.position = transform.position + Vector3.right * GetComponent<RectTransform>().sizeDelta.x / 2;
    }
    #endregion
    #region Slot Functions
    /// <summary>
    /// Function used to place an item inside the slot
    /// </summary>
    /// <param name="item">Item to be placed</param>
    public void SetItem(ItemInfo item)
    {
        this.item = item;
        if (!hoverHandler)
            hoverHandler = GetComponent<HoverHandler>();
        // Update hover script
        hoverHandler.itemInfo = item;
    }

    /// <summary>
    /// Function used to get the item from the slot
    /// </summary>
    /// <returns>Item held by this slot</returns>
    public ItemInfo GetItem() { return this.item; }
    #endregion
}
