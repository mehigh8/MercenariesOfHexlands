using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipHandler : MonoBehaviour
{
    [Header("General Variables")]
    [SerializeField] private GameObject tooltip;


    [Header("Ability Tooltip")]
    [SerializeField] private GameObject abilityTooltip;
    [SerializeField] private TextMeshProUGUI abilityName;
    [SerializeField] private TextMeshProUGUI abilityDescription;
    [SerializeField] private TextMeshProUGUI abilityDamage;
    [SerializeField] private TextMeshProUGUI abilityCooldown;
    [SerializeField] private TextMeshProUGUI abilityRange;
    [SerializeField] private TextMeshProUGUI abilityAOE;
    [SerializeField] private TextMeshProUGUI abilityLinger;
    [SerializeField] private TextMeshProUGUI abilityModifiers;

    [Header("Item Tooltip")]
    [SerializeField] private GameObject itemTooltip;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemStats;
    [SerializeField] private Transform abilitiesHolder;
    [SerializeField] private GameObject abilityItemPrefab;

    private GameObject currentTooltip;

    public void HideTooltip()
    {
        if (currentTooltip)
            currentTooltip.SetActive(false);
        tooltip.SetActive(false);
    }

    public void ShowAbilityTooltip(AbilityInfo abilityInfo, Vector3 position)
    {
        tooltip.SetActive(true);
        currentTooltip = abilityTooltip;

        tooltip.transform.position = position;

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

    public void ShowItemTooltip(ItemInfo itemInfo, Vector3 position)
    {
        tooltip.SetActive(true);
        currentTooltip = itemTooltip;

        tooltip.transform.position = position;

        RectTransform rectTooltip = itemTooltip.GetComponent<RectTransform>();
        rectTooltip.sizeDelta = new Vector2(rectTooltip.sizeDelta.x, 75 + 125 * itemInfo.abilities.Count);

        foreach (Transform child in abilitiesHolder)
            Destroy(child.gameObject);

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
}
