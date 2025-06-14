using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public AbilityInfo abilityInfo;
    [HideInInspector] public Vector3 position;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!abilityInfo)
        {
            Debug.LogError("Ability info was not set for one of the ability slots");
            return;
        }
        UIManager.instance.tooltipHandler.ShowAbilityTooltip(abilityInfo, position);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.instance.tooltipHandler.HideTooltip();
    }
}
