using UnityEngine;
using UnityEngine.EventSystems;

public class HoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TooltipType
    {
        Ability,
        Item,
    }

    [SerializeField] private TooltipType tooltipType; // Contains information as to whether the tool tip is for an ability or an item (this could've been done by just checking their respective references but idk)

    [HideInInspector] public AbilityInfo abilityInfo; // Information relating to the ability that is held inside the tooltip
    [HideInInspector] public ItemInfo itemInfo; // Information relating to the item that is held inside the tooltip
    [HideInInspector] public Vector3 position; // Position where the tooltip should be displayed when hovering

    /// <summary>
    /// Event that is called when the cursor enters the bounds of the UI element
    /// </summary>
    /// <param name="eventData"></param>
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
    
    /// <summary>
    /// Event that is called when the cursor exits the bounds of the UI element
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.instance.tooltipHandler.HideTooltip();
    }
}
