using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="New Ability", menuName="Ability")]
public class AbilityInfo : ScriptableObject
{
    [System.Serializable]
    public class AbilityAnimations
    {
        public bool test;
        public AbilityAnimations() { test = false; }
    }

    public enum Element
    {
        None,
        Fire,
        Lightning,
        Water,
        Void,
    }

    public enum WeaponDamageModifiers
    {
        None,
        Addition,
        Subtraction,
        Multiplication,
        Division,
    }

    public enum Effects // TODO: These are just placeholders, actual buffs will be designed later
    {
        None,
        Poison,
        Regeneration,
        Vulnerable,
        Protected,
    }

    [Header("Visual Information")]
    [Tooltip("What the ability is called")]
    public new string name;

    [Tooltip("Sample text that describes what the ability does")]
    public string description;

    [Tooltip("The Image that should be displayed for the ability")]
    public Sprite displayImage;

    [Tooltip("Flair that should occur when casting the ability")]
    public AbilityAnimations flairEvents;

    [Header("Basic Stats")]
    [Tooltip("How much damage the ability does (ignored if useWeaponDamage is true)")]
    public int damage;

    [Tooltip("How many hexes away the abilities can be cast from")]
    public int range;

    [Tooltip("How many hexes around the casting point are affected by the ability")]
    public int areOfEffect;

    [Tooltip("How many turns should the hit hexes be afected for (-1 if no lingering)")]
    public int lingeringDuration;

    [Header("Ability Modifiers")]
    [Tooltip("The element of the ability")]
    public Element element;

    [Tooltip("Whether or not this requires a target")]
    public bool requiresTarget;

    [Tooltip("Charges to target")]
    public bool chargeToTarget;

    [Tooltip("Whether damage should be treated as healing instead")]
    public bool isHeal;

    [Tooltip("Whether or not to use weapon damage")]
    public bool useWeaponDamage;

    [Tooltip("How the weapon damage is changed by this ability")]
    public WeaponDamageModifiers weaponDamageModifiers;

    [Tooltip("The value by which the weapon damage is changed by the ability")]
    public float weaponDamageModiferAmount;

    [Tooltip("Which effects this spell should inflict")]
    public Effects[] appliedEffects;
}
