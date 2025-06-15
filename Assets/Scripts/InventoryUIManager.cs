using FishNet.Managing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    [SerializeField] private ItemSlot ringSlot;
    [SerializeField] private ItemSlot gloveSlot;
    [SerializeField] private ItemSlot chestplateSlot;
    [SerializeField] private ItemSlot leggingsSlot;
    [SerializeField] private ItemSlot bootsSlot;
    [SerializeField] private List<ItemSlot> itemSlots;

    [Header("Others")]
    [SerializeField] private GameObject inventoryObject;

    private PlayerInfo playerInfo;
    [HideInInspector] public bool isOpened = false;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void OpenInventory(PlayerInfo playerInfo)
    {
        this.playerInfo = playerInfo;
        inventoryObject.SetActive(true);
        isOpened = true;

        helmetSlot.button.onClick.RemoveAllListeners();
        ringSlot.button.onClick.RemoveAllListeners();
        gloveSlot.button.onClick.RemoveAllListeners();
        chestplateSlot.button.onClick.RemoveAllListeners();
        leggingsSlot.button.onClick.RemoveAllListeners();
        bootsSlot.button.onClick.RemoveAllListeners();
        itemSlots.ForEach(i => i.button.onClick.RemoveAllListeners());

        helmetSlot.button.onClick.AddListener(EquipmentInteract);
        ringSlot.button.onClick.AddListener(EquipmentInteract);
        gloveSlot.button.onClick.AddListener(EquipmentInteract);
        chestplateSlot.button.onClick.AddListener(EquipmentInteract);
        leggingsSlot.button.onClick.AddListener(EquipmentInteract);
        bootsSlot.button.onClick.AddListener(EquipmentInteract);
        itemSlots.ForEach(i => i.button.onClick.AddListener(ItemInteract));

        UpdateInfo();
    }

    public void CloseInventory()
    {
        inventoryObject.SetActive(false);
        isOpened = false;
    }

    public void EquipmentInteract()
    {

    }

    public void ItemInteract()
    {

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

        healthBar.value = (float)playerInfo.currentHealth / playerInfo.maxHealth;
        healthText.text = playerInfo.currentHealth + " / " + playerInfo.maxHealth;
        damageText.text = playerInfo.damage + " DMG";
        critText.text = (int)(playerInfo.critChance * 100) + " CRT";
        defenceText.text = playerInfo.defence + " DEF";
        movementText.text = playerInfo.movementPerTurn + " MOV";
    }
}
