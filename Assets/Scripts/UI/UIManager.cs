using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [HideInInspector] public AbilitiesUIManager abilitiesUIManager; // Reference to the ability UI manager
    [HideInInspector] public InventoryUIManager inventoryUIManager; // Reference to the inventory UI manager
    [HideInInspector] public TooltipHandler tooltipHandler; // Reference to the tooltip UI manager

    void Start()
    {
        // Assigning the references to the other scripts
        abilitiesUIManager = GetComponent<AbilitiesUIManager>();
        inventoryUIManager = GetComponent<InventoryUIManager>();
        tooltipHandler = GetComponent<TooltipHandler>();
    }

    void Awake()
    {
        // Just singleton
        instance = this;
    }

}
