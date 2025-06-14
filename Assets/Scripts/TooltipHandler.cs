using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    private GameObject currentTooltip;

    public int HandleWeaponModifier(int weaponDamage, float modifierAmount, AbilityInfo.WeaponDamageModifiers modifier)
    {
        switch (modifier)
        {
            case AbilityInfo.WeaponDamageModifiers.Addition:
                return (int)(weaponDamage + modifierAmount);
            case AbilityInfo.WeaponDamageModifiers.Multiplication:
                return (int)(weaponDamage * modifierAmount);
            case AbilityInfo.WeaponDamageModifiers.Subtraction:
                return (int)(weaponDamage - modifierAmount);
            case AbilityInfo.WeaponDamageModifiers.Division:
                return (int)(weaponDamage / modifierAmount);
            default:
                return weaponDamage;
        }
    }

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
        abilityDamage.text = (abilityInfo.useWeaponDamage ? HandleWeaponModifier(1 /* TODO: replace with actual weapon damage */,
                                                                                abilityInfo.weaponDamageModiferAmount,
                                                                                abilityInfo.weaponDamageModifiers) + " W" : abilityInfo.damage + " ") + (abilityInfo.isHeal ? "H" : "D");

        abilityCooldown.text = abilityInfo.cooldown + " T";
        abilityRange.text = abilityInfo.range + " R";
        abilityAOE.text = abilityInfo.areOfEffect + " AOE";
        abilityLinger.text = (abilityInfo.lingeringDuration <= 0) ? "" : (abilityInfo.lingeringDuration + " L");
        abilityModifiers.text = "";
        foreach (AbilityInfo.EffectClass effectClass in abilityInfo.appliedEffects)
            abilityModifiers.text += effectClass.effectDuration + " " + Enum.GetName(typeof(AbilityInfo.Effects), effectClass.effect) + " ";

        currentTooltip.SetActive(true);
    }
}
