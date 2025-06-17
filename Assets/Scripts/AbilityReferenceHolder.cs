using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityReferenceHolder : MonoBehaviour
{
    [SerializeField] private Image abilityImage;
    [SerializeField] private TextMeshProUGUI abilityName;
    [SerializeField] private TextMeshProUGUI abilityDescription;
    [SerializeField] private TextMeshProUGUI abilityCooldown;
    [SerializeField] private TextMeshProUGUI abilityDamage;
    [SerializeField] private TextMeshProUGUI abilityRange;
    [SerializeField] private TextMeshProUGUI abilityAOE;
    [SerializeField] private TextMeshProUGUI abilityLinger;
    [SerializeField] private TextMeshProUGUI abilityModifiers;


    public void FillInFields(AbilityInfo abilityInfo)
    {
        abilityImage.sprite = abilityInfo.displayImage;
        abilityName.text = String.Format("{0} ({1})", abilityInfo.name, Enum.GetName(typeof(AbilityInfo.Element), abilityInfo.element));
        abilityDescription.text = (abilityInfo.chargeToTarget ? "Charges towards the target. " : "") + (abilityInfo.requiresTarget ? "Requires target. " : "") + abilityInfo.description;
        abilityDamage.text = (abilityInfo.useWeaponDamage ? abilityInfo.GetDamage(1 /* TODO: replace with actual weapon damage */) + " W" : abilityInfo.GetDamage() + " ") + (abilityInfo.isHeal ? "H" : "D");

        abilityCooldown.text = abilityInfo.cooldown + " T";
        abilityRange.text = abilityInfo.range + " R";
        abilityAOE.text = abilityInfo.areOfEffect + " AOE";
        abilityLinger.text = (abilityInfo.lingeringDuration <= 0) ? "" : (abilityInfo.lingeringDuration + " L");
        abilityModifiers.text = "";
        foreach (AbilityInfo.EffectClass effectClass in abilityInfo.appliedEffects)
            abilityModifiers.text += effectClass.effectDuration + " " + Enum.GetName(typeof(AbilityInfo.Effects), effectClass.effect) + "\n";
    }
}
