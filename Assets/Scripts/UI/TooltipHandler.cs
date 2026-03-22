using System;
using TMPro;
using UnityEngine;

public class TooltipHandler : MonoBehaviour
{
    [Header("General Variables")]
    [SerializeField] private GameObject tooltip; // Reference to the base game object of the tooltip


    [Header("Ability Tooltip")]
    [SerializeField] private GameObject abilityTooltip; // Reference to the root object ability tooltip
    [SerializeField] private TextMeshProUGUI abilityName; // Reference to the ability name of the tooltip
    [SerializeField] private TextMeshProUGUI abilityDescription; // Reference to the ability description of the tooltip
    [SerializeField] private TextMeshProUGUI abilityDamage; // Reference to the ability damage of the tooltip
    [SerializeField] private TextMeshProUGUI abilityCooldown; // Reference to the ability cooldown of the tooltip
    [SerializeField] private TextMeshProUGUI abilityRange; // Reference to the ability range of the tooltip
    [SerializeField] private TextMeshProUGUI abilityAOE; // Reference to the ability AOE of the tooltip
    [SerializeField] private TextMeshProUGUI abilityLinger; // Reference to the ability linger of the tooltip
    [SerializeField] private TextMeshProUGUI abilityModifiers; // Reference to the ability modifiers of the tooltip

    [Header("Item Tooltip")]
    [SerializeField] private GameObject itemTooltip; // Reference to the root object item tooltip
    [SerializeField] private TextMeshProUGUI itemName; // Reference to the item name of the tooltip
    [SerializeField] private TextMeshProUGUI itemStats; // Reference to the item stats of the tooltip
    [SerializeField] private Transform abilitiesHolder; // Reference the the ability preview holder
    [SerializeField] private GameObject abilityItemPrefab; // Prefab for the item ability preview

    private GameObject currentTooltip; // Stores the current tooltip type ability/item

#region Public Functions
    /// <summary>
    /// Function that hides the tooltip
    /// </summary>
    public void HideTooltip()
    {
        if (currentTooltip)
            currentTooltip.SetActive(false);
        tooltip.SetActive(false);
    }

    /// <summary>
    /// Function that displays the ability information tooltip
    /// </summary>
    /// <param name="abilityInfo">The information that should be displayed inside the tooltip</param>
    /// <param name="position">Where the tooltip should be displayed on the screen</param>
    public void ShowAbilityTooltip(AbilityInfo abilityInfo, Vector3 position)
    {
        // Enabling the tooltip display
        tooltip.SetActive(true);
        currentTooltip = abilityTooltip;

        // Setting the position of the tooltip to the target location
        tooltip.transform.position = position;

        // Setting all of the display information to the ability information
        abilityName.text = String.Format("{0} ({1})", abilityInfo.name, Enum.GetName(typeof(AbilityInfo.Element), abilityInfo.element));
        abilityDescription.text = (abilityInfo.chargeToTarget ? "Charges towards the target. " : "") + (abilityInfo.requiresTarget ? "Requires target. " : "") + abilityInfo.description;
        abilityDamage.text = (abilityInfo.useWeaponDamage ? abilityInfo.GetDamage(1 /* TODO: replace with actual weapon damage */) + " W" : abilityInfo.GetDamage() + " ") + (abilityInfo.isHeal ? "H" : "D");

        abilityCooldown.text = abilityInfo.cooldown + " T";
        abilityRange.text = abilityInfo.range + " R";
        abilityAOE.text = abilityInfo.areOfEffect + " AOE";
        abilityLinger.text = (abilityInfo.lingeringDuration <= 0) ? "" : (abilityInfo.lingeringDuration + " L");
        abilityModifiers.text = "";
        foreach (AbilityInfo.EffectClass effectClass in abilityInfo.appliedEffects)
            abilityModifiers.text += effectClass.effectDuration + " " + Enum.GetName(typeof(AbilityInfo.Effects), effectClass.effect) + " ";

        currentTooltip.SetActive(true);
    }

    /// <summary>
    /// Function that displays the item information tooltip
    /// </summary>
    /// <param name="itemInfo">The information that should be displayed inside the tooltip</param>
    /// <param name="position">Where the tooltip should be displayed on the screen</param>
    public void ShowItemTooltip(ItemInfo itemInfo, Vector3 position)
    {
        // Enabling the tooltip display
        tooltip.SetActive(true);
        currentTooltip = itemTooltip;

        // Setting the position of the tooltip to the target location
        tooltip.transform.position = position;

        // Preparing the display for all of the abilities of the item
        RectTransform rectTooltip = itemTooltip.GetComponent<RectTransform>();
        rectTooltip.sizeDelta = new Vector2(rectTooltip.sizeDelta.x, 75 + 125 * itemInfo.abilities.Count);

        // Clearing the abilities holder from the previous objects
        foreach (Transform child in abilitiesHolder)
            Destroy(child.gameObject);

        // Setting all of the display information to the item information
        itemName.text = String.Format("{0} ({1})", itemInfo.itemName, Enum.GetName(typeof(ItemInfo.EquipmentSlot), itemInfo.equipmentSlot));
        itemStats.text = "";
        foreach (ItemInfo.ModifyStat stat in itemInfo.modifiedStats)
            itemStats.text += (stat.value > 0 ? "+" : "") + stat.value + " " + Enum.GetName(typeof(ItemInfo.AffectedStat), stat.stat) + " ";

        for (int i = 0; i < itemInfo.abilities.Count; i++)
        {
            AbilityReferenceHolder currentAbilityUI = Instantiate(abilityItemPrefab, abilitiesHolder.position - Vector3.up * i * 125, Quaternion.identity, abilitiesHolder).GetComponent<AbilityReferenceHolder>();
            currentAbilityUI.FillInFields(itemInfo.abilities[i]);
        }

        currentTooltip.SetActive(true);
    }
#endregion
}
