using FishNet.Managing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using FishNet.Object;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text critText;
    [SerializeField] private TMP_Text defenceText;
    [SerializeField] private TMP_Text movementText;

    [Header("Item Slots")]
    [SerializeField] private ItemSlot helmetSlot;
    [SerializeField] private ItemSlot weaponSlot;
    [SerializeField] private ItemSlot gloveSlot;
    [SerializeField] private ItemSlot chestplateSlot;
    [SerializeField] private ItemSlot leggingsSlot;
    [SerializeField] private ItemSlot bootsSlot;
    [SerializeField] private List<ItemSlot> itemSlots;
    [SerializeField] private ItemSlot dropSlot;

    [Header("Others")]
    [SerializeField] private GameObject inventoryObject;
    [SerializeField] private Image heldItemImage;

    private PlayerInfo playerInfo;
    private PlayerController playerController;
    [HideInInspector] public bool isOpened = false;

    private ItemSlot heldItem = null;

    public bool HasHeldItem()
    {
        return heldItem;
    }

    void Update()
    {
        heldItemImage.transform.position = Input.mousePosition;
    }

    public void OpenInventory(PlayerInfo playerInfo, PlayerController playerController)
    {
        this.playerInfo = playerInfo;
        this.playerController = playerController;
        inventoryObject.SetActive(true);
        isOpened = true;

        helmetSlot.button.onClick.RemoveAllListeners();
        weaponSlot.button.onClick.RemoveAllListeners();
        gloveSlot.button.onClick.RemoveAllListeners();
        chestplateSlot.button.onClick.RemoveAllListeners();
        leggingsSlot.button.onClick.RemoveAllListeners();
        bootsSlot.button.onClick.RemoveAllListeners();
        itemSlots.ForEach(i => i.button.onClick.RemoveAllListeners());
        dropSlot.button.onClick.RemoveAllListeners();

        helmetSlot.button.onClick.AddListener(delegate { ItemInteract(helmetSlot); });
        weaponSlot.button.onClick.AddListener(delegate { ItemInteract(weaponSlot); });
        gloveSlot.button.onClick.AddListener(delegate { ItemInteract(gloveSlot); });
        chestplateSlot.button.onClick.AddListener(delegate { ItemInteract(chestplateSlot); });
        leggingsSlot.button.onClick.AddListener(delegate { ItemInteract(leggingsSlot); });
        bootsSlot.button.onClick.AddListener(delegate { ItemInteract(bootsSlot); });
        itemSlots.ForEach(i => i.button.onClick.AddListener(delegate { ItemInteract(i); }));
        dropSlot.button.onClick.AddListener(DropInteract);

        UpdateInfo();
    }

    public void CloseInventory()
    {
        inventoryObject.SetActive(false);
        isOpened = false;

        if (heldItem != null)
        {
            heldItem.image.sprite = heldItem.GetItem().itemImage;
            heldItem.image.color = Color.white;
            heldItem = null;
            heldItemImage.gameObject.SetActive(false);
        }
    }

    public void ItemInteract(ItemSlot slot)
    {
        if (heldItem == null)
        {
            if (slot.GetItem() == null)
                return;
            UIManager.instance.tooltipHandler.HideTooltip();
            heldItem = slot;
            heldItem.image.sprite = null;
            heldItem.image.color = new Color(0.254717f, 0.254717f, 0.254717f);
            heldItemImage.sprite = heldItem.GetItem().itemImage;
            heldItemImage.gameObject.SetActive(true);
        }
        else
        {
            if (heldItem.isEquipment)
            {
                if (slot.isEquipment && slot != heldItem)
                    return;

                if (slot.GetItem() == null || slot.GetItem().equipmentSlot == heldItem.equipmentType)
                {
                    SwapItems(heldItem, slot);
                    heldItemImage.gameObject.SetActive(false);
                    heldItem = null;
                }
            }
            else
            {
                if (slot.isEquipment && slot.equipmentType != heldItem.GetItem().equipmentSlot)
                    return;

                SwapItems(heldItem, slot);
                heldItemImage.gameObject.SetActive(false);
                heldItem = null;
            }
        }
    }

    public void DropInteract()
    {
        if (heldItem != null)
        {
            if (playerController.currentPosition.hexRenderer.hasItem.Value == -1) // Has no item
            {
                ItemInfo droppedItem = heldItem.GetItem();
                _StoreItem(null, heldItem);
                heldItemImage.gameObject.SetActive(false);
                heldItem = null;
                playerController.DropItem(GameManager.instance.allExistingItems.IndexOf(droppedItem), playerController.currentPosition.hexObj.name);
            }
        }
    }

    private void SwapItems(ItemSlot aSlot, ItemSlot bSlot)
    {
        ItemInfo aItem = aSlot.GetItem();
        ItemInfo bItem = bSlot.GetItem();

        _StoreItem(aItem, bSlot);
        _StoreItem(bItem, aSlot);

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

    public void UpdateInfo()
    {
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

    private void _StoreItem(ItemInfo item, ItemSlot slot)
    {
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
}
