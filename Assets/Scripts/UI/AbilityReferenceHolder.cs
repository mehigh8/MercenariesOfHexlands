using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This is a script attached to the UI element that gives a brief description of abilities inside an item tooltip
/// </summary>
public class AbilityReferenceHolder : MonoBehaviour
{
    [SerializeField] private Image abilityImage; // Reference to the image display of the ability
    [SerializeField] private TextMeshProUGUI abilityName; // Reference to the ability name
    [SerializeField] private TextMeshProUGUI abilityDescription; // Reference to the ability description
    [SerializeField] private TextMeshProUGUI abilityCooldown; // Reference to the cooldown display
    [SerializeField] private TextMeshProUGUI abilityDamage; // Reference to the damage display
    [SerializeField] private TextMeshProUGUI abilityRange; // Reference to the range display
    [SerializeField] private TextMeshProUGUI abilityAOE; // Reference to the AOE display
    [SerializeField] private TextMeshProUGUI abilityLinger; // Reference to the lingering display
    [SerializeField] private TextMeshProUGUI abilityModifiers; // Reference to the modifiers display


    /// <summary>
    /// Function that updates all of the UI elements to the provided ability info
    /// </summary>
    /// <param name="abilityInfo">Ability object that contains all relevant information</param>
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
