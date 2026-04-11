using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private Slider healthBar; // Reference to the slider used to display the current player's health
    [SerializeField] private TMP_Text healthText; // Reference to the text used to display the current player's health in numbers
    [SerializeField] private TMP_Text damageText; // Reference to the text used to display the current player's damage
    [SerializeField] private TMP_Text critText; // Reference to the text used to display the current player's critical chance
    [SerializeField] private TMP_Text defenceText; // Reference to the text used to display the current player's defence
    [SerializeField] private TMP_Text movementText; // Reference to the text used to display the current player's movement

    [Header("Item Slots")]
    [SerializeField] private ItemSlot helmetSlot; // Reference to the item slot used for helmets
    [SerializeField] private ItemSlot weaponSlot; // Reference to the item slot used for weapons
    [SerializeField] private ItemSlot gloveSlot; // Reference to the item slot used for gloves
    [SerializeField] private ItemSlot chestplateSlot; // Reference to the item slot used for chestplates
    [SerializeField] private ItemSlot leggingsSlot; // Reference to the item slot used for leggings
    [SerializeField] private ItemSlot bootsSlot; // Reference to the item slot used for boots
    [SerializeField] private List<ItemSlot> itemSlots; // Reference to the rest of the item slots
    [SerializeField] private ItemSlot dropSlot; // Reference to the item slot used to drop items

    [Header("Others")]
    [SerializeField] private GameObject inventoryObject; // Reference to the whole invetory as an object. Used to enable and disable the graphics
    [SerializeField] private Image heldItemImage; // Reference to the image that shows under the cursor when an item is picked

    private PlayerInfo playerInfo; // Reference to the player info script
    private PlayerController playerController; // Reference to the player controller script
    [HideInInspector] public bool isOpened = false; // Bool to check if the inventory is opened

    private ItemSlot heldItem = null; // Reference to the item slot from which the item was taken and is now held on the cursor

    #region Unity Functions
    void Update()
    {
        // Update the position of the item image to be at the cursor's position
        heldItemImage.transform.position = Input.mousePosition;
    }
    #endregion

    #region Inventory Interaction
    /// <summary>
    /// Function used to open the inventory
    /// </summary>
    /// <param name="playerInfo">Player info of the player who opened the inventory</param>
    /// <param name="playerController">Player controller of the player who opened the inventory</param>
    public void OpenInventory(PlayerInfo playerInfo, PlayerController playerController)
    {
        // Update references
        this.playerInfo = playerInfo;
        this.playerController = playerController;
        // Display inventory
        inventoryObject.SetActive(true);
        isOpened = true;

        // Remove old listeners from all of the slots' buttons
        helmetSlot.button.onClick.RemoveAllListeners();
        weaponSlot.button.onClick.RemoveAllListeners();
        gloveSlot.button.onClick.RemoveAllListeners();
        chestplateSlot.button.onClick.RemoveAllListeners();
        leggingsSlot.button.onClick.RemoveAllListeners();
        bootsSlot.button.onClick.RemoveAllListeners();
        itemSlots.ForEach(i => i.button.onClick.RemoveAllListeners());
        dropSlot.button.onClick.RemoveAllListeners();

        // Add corresponding listeners to the slots' buttons
        helmetSlot.button.onClick.AddListener(delegate { ItemInteract(helmetSlot); });
        weaponSlot.button.onClick.AddListener(delegate { ItemInteract(weaponSlot); });
        gloveSlot.button.onClick.AddListener(delegate { ItemInteract(gloveSlot); });
        chestplateSlot.button.onClick.AddListener(delegate { ItemInteract(chestplateSlot); });
        leggingsSlot.button.onClick.AddListener(delegate { ItemInteract(leggingsSlot); });
        bootsSlot.button.onClick.AddListener(delegate { ItemInteract(bootsSlot); });
        itemSlots.ForEach(i => i.button.onClick.AddListener(delegate { ItemInteract(i); }));
        dropSlot.button.onClick.AddListener(DropInteract);

        // Update player stats
        UpdateInfo();
    }

    /// <summary>
    /// Function used to close the inventory
    /// </summary>
    public void CloseInventory()
    {
        // Stop displaying the inventory
        inventoryObject.SetActive(false);
        isOpened = false;

        // Check if there is an item held, in which case set the image back and disable the cursor image
        if (heldItem != null)
        {
            heldItem.image.sprite = heldItem.GetItem().itemImage;
            heldItem.image.color = Color.white;
            heldItem = null;
            heldItemImage.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Slots Interaction
    /// <summary>
    /// Function used by the slots' buttons to pick and place items inside the inventory by interacting with the slots
    /// </summary>
    /// <param name="slot">Reference to the slot</param>
    public void ItemInteract(ItemSlot slot)
    {
        // If there is no item held, pick the item from this slot
        if (heldItem == null)
        {
            if (slot.GetItem() == null)
                return;
            UIManager.instance.tooltipHandler.HideTooltip();
            // Set reference to this slot and change color to gray
            heldItem = slot;
            heldItem.image.sprite = null;
            heldItem.image.color = new Color(0.254717f, 0.254717f, 0.254717f);
            // Update image under the cursor
            heldItemImage.sprite = heldItem.GetItem().itemImage;
            heldItemImage.gameObject.SetActive(true);
        }
        else
        {
            // If there is an item already held, try to place it in this slot

            // If the item held comes from an equipment slot, check if the item type matches this slot type
            if (heldItem.isEquipment)
            {
                if (slot.isEquipment && slot != heldItem)
                    return;

                // If it matches, swap this slot with the slot referenced by the held item
                if (slot.GetItem() == null || slot.GetItem().equipmentSlot == heldItem.equipmentType)
                {
                    SwapItems(heldItem, slot);
                    heldItemImage.gameObject.SetActive(false);
                    heldItem = null;
                }
            }
            else
            {
                // If the item held is not an equipment, check if this slot requires an equipment type
                if (slot.isEquipment && slot.equipmentType != heldItem.GetItem().equipmentSlot)
                    return;

                SwapItems(heldItem, slot);
                heldItemImage.gameObject.SetActive(false);
                heldItem = null;
            }
        }
    }
    /// <summary>
    /// Function used by the drop slot to drop an item on the ground
    /// </summary>
    public void DropInteract()
    {
        // Check if an item is held
        if (heldItem != null)
        {
            // Check if the hex tile the player is standing on does not have an item
            if (playerController.currentPosition.hexRenderer.hasItem.Value == -1)
            {
                ItemInfo droppedItem = heldItem.GetItem();
                // Empty the slot from which we drop the item
                _StoreItem(null, heldItem);
                // Update the player's stats after unequipping the item
                if (heldItem.isEquipment)
                    playerInfo.UnequipItem(droppedItem);
                UpdateInfo();

                // Update image under the cursor
                heldItemImage.gameObject.SetActive(false);
                heldItem = null;
                // Place item on the ground
                playerController.DropItem(GameManager.instance.allExistingItems.IndexOf(droppedItem), playerController.currentPosition.hexObj.name);

                // Update HUD
                UIManager.instance.hudManager.ClearButtons();
                UIManager.instance.hudManager.AddButton("Pick-up item", playerController.PickupItem);
            }
        }
    }

    /// <summary>
    /// Function used to swap the items between 2 slots
    /// </summary>
    /// <param name="aSlot">First slot</param>
    /// <param name="bSlot">Second slot</param>
    private void SwapItems(ItemSlot aSlot, ItemSlot bSlot)
    {
        ItemInfo aItem = aSlot.GetItem();
        ItemInfo bItem = bSlot.GetItem();

        // Swap the 2 items
        _StoreItem(aItem, bSlot);
        _StoreItem(bItem, aSlot);

        // Check if slots were equipment types and update stats accordingly
        if (aSlot.isEquipment)
        {
            playerInfo.UnequipItem(aItem);
            playerInfo.EquipItem(bItem);
        }

        if (bSlot.isEquipment)
        {
            playerInfo.UnequipItem(bItem);
            playerInfo.EquipItem(aItem);
        }

        UpdateInfo();
    }

    /// <summary>
    /// Function used to store an item in the first empty slot from the inventory
    /// </summary>
    /// <param name="item">Item to be stored</param>
    /// <returns>True - there was a slot to place the item; False - No slot could be found</returns>
    public bool StoreItem(ItemInfo item)
    {
        foreach (ItemSlot slot in itemSlots)
        {
            if (slot.GetItem() == null)
            {
                _StoreItem(item, slot);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Internal function used to store an item to a specific slot
    /// </summary>
    /// <param name="item">Item to be stored</param>
    /// <param name="slot">Slot in which to store the item</param>
    private void _StoreItem(ItemInfo item, ItemSlot slot)
    {
        // Store item and update visuals
        slot.SetItem(item);
        if (item != null)
        {
            slot.image.sprite = item.itemImage;
            slot.image.color = Color.white;
        }
        else
        {
            slot.image.sprite = null;
            slot.image.color = new Color(0.254717f, 0.254717f, 0.254717f);
        }
    }
    #endregion

    #region Other Functions
    /// <summary>
    /// Function used to update the player stats in the inventory
    /// </summary>
    public void UpdateInfo()
    {
        // Check if the player info reference is set
        if (playerInfo == null)
        {
            healthBar.value = 0;
            healthText.text = "";
            damageText.text = "";
            critText.text = "";
            defenceText.text = "";
            movementText.text = "";
            return;
        }

        healthBar.value = (float)playerInfo.currentHealth.Value / playerInfo.maxHealth.Value;
        healthText.text = playerInfo.currentHealth.Value + " / " + playerInfo.maxHealth.Value;
        damageText.text = playerInfo.damage + " DMG";
        critText.text = (int)(playerInfo.critChance * 100) + " CRT";
        defenceText.text = playerInfo.defence + " DEF";
        movementText.text = playerInfo.movementPerTurn + " MOV";
    }

    /// <summary>
    /// Function used to check if an item is held
    /// </summary>
    /// <returns>True - An item is held; False - No item is held</returns>
    public bool HasHeldItem()
    {
        return heldItem;
    }
    #endregion
}
