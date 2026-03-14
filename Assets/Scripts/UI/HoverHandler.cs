using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TooltipType
    {
        Ability,
        Item,
    }

    [SerializeField] private TooltipType tooltipType;

    [HideInInspector] public AbilityInfo abilityInfo;
    [HideInInspector] public ItemInfo itemInfo;
    [HideInInspector] public Vector3 position;

    public void OnPointerEnter(PointerEventData eventData)
    {
        switch (tooltipType)
        {
            case TooltipType.Ability:
                if (!abilityInfo)
                {
                    Debug.LogError("Ability info was not set for one of the ability slots");
                    return;
                }
                UIManager.instance.tooltipHandler.ShowAbilityTooltip(abilityInfo, position);
                break;
            case TooltipType.Item:
                if (!itemInfo || UIManager.instance.inventoryUIManager.HasHeldItem())
                {
                    Debug.Log("Is Holding an item or slot is empty");
                    return;
                }
                UIManager.instance.tooltipHandler.ShowItemTooltip(itemInfo, position);
                break;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.instance.tooltipHandler.HideTooltip();
    }
}
