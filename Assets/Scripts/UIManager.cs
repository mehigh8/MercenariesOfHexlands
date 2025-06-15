using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [HideInInspector] public AbilitiesUIManager abilitiesUIManager;
    [HideInInspector] public InventoryUIManager inventoryUIManager;
    [HideInInspector] public TooltipHandler tooltipHandler;

    void Start()
    {
        abilitiesUIManager = GetComponent<AbilitiesUIManager>();
        inventoryUIManager = GetComponent<InventoryUIManager>();
        tooltipHandler = GetComponent<TooltipHandler>();
    }

    public static UIManager instance;
    void Awake()
    {
        instance = this;
    }

}
