using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilitiesUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform abilitySlotsRoot;
    [SerializeField] private GameObject abilitySlotPrefab;

    [SerializeField] private List<Button> abilitySlots;
    [SerializeField] private List<AbilityInfo> abilities;

    [Header("Variables")]
    [SerializeField] private float abilitySize;
    [SerializeField] private float abilitySpacing;

    [HideInInspector] public AbilityHandler client;

    public void UpdateAbilityCooldownsUI(List<AbilityHandler.AbilityCooldown> cooldowns)
    {
        foreach (Button slot in abilitySlots)
        {
            slot.GetComponentInChildren<TextMeshProUGUI>().text = "";
            slot.interactable = true;
        }

        for (int i = 0; i < abilitySlots.Count; i++)
        {
            Button currentSlot = abilitySlots[i];
            AbilityInfo currentAbility = abilities[i];
            List<AbilityHandler.AbilityCooldown> foundCooldown = cooldowns.Where(c => c.ability == currentAbility).ToList();
            if (foundCooldown.Count > 0)
            {
                TextMeshProUGUI cooldownText = currentSlot.GetComponentInChildren<TextMeshProUGUI>();
                cooldownText.text = foundCooldown[0].remainingCooldown.ToString();
                currentSlot.interactable = false;
            }
        }
    }

    private void OnAbilityClick(int index)
    {
        if (!client.playerController)
            return;
        if (!GameManager.instance.IsMyTurn() || client.playerController.isMoving())
            return;
        StartCoroutine(client.PrepareCasting(abilities[index]));
    }

    public void GenerateAbilityUI()
    {
        abilitySlots.Clear();
        foreach (Button child in abilitySlotsRoot.GetComponentsInChildren<Button>())
            Destroy(child.gameObject);
        if (abilities.Count == 0)
        {
            ShowAbilities(false);
            return;
        }
        ShowAbilities(true);
        RectTransform rootRect = abilitySlotsRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2((abilitySize + abilitySpacing) * abilities.Count + abilitySpacing, abilitySize + abilitySpacing * 2);
        Vector3 cursor = rootRect.position - Vector3.right * (rootRect.sizeDelta.x / 2 - abilitySpacing - abilitySize / 2) + Vector3.up * rootRect.sizeDelta.y / 2;
        for (int i = 0; i < abilities.Count; i++)
        {
            Button createdButton = Instantiate(abilitySlotPrefab, cursor, Quaternion.identity, abilitySlotsRoot).GetComponent<Button>();
            int localI = i;
            createdButton.onClick.AddListener(delegate { OnAbilityClick(localI); });
            HoverHandler hoverLogic = createdButton.GetComponent<HoverHandler>();
            hoverLogic.abilityInfo = abilities[i];
            hoverLogic.position = cursor + Vector3.up * abilitySize;
            cursor += Vector3.right * (abilitySize + abilitySpacing);
            createdButton.GetComponent<RectTransform>().sizeDelta = new Vector2(abilitySize, abilitySize);
            createdButton.GetComponentInChildren<TextMeshProUGUI>().GetComponent<RectTransform>().sizeDelta = new Vector2(abilitySize, abilitySize);
            createdButton.GetComponent<Image>().sprite = abilities[i].displayImage;
            abilitySlots.Add(createdButton);
        }
        if (client)
            UpdateAbilityCooldownsUI(client.cooldowns);
    }

    public void ShowAbilities(bool shouldShow)
    {
        abilitySlotsRoot.gameObject.SetActive(shouldShow);
    }

    void Start()
    {
        GenerateAbilityUI();
        ShowAbilities(false);
        UpdateAbilityCooldownsUI(new List<AbilityHandler.AbilityCooldown>());
    }

    public bool HasAbility(AbilityInfo abilityInfo)
    {
        return abilities.Contains(abilityInfo);
    }

    public void AddAbility(AbilityInfo abilityInfo)
    {
        abilities.Add(abilityInfo);
    }

    public void RemoveAbility(AbilityInfo abilityInfo)
    {
        abilities.Remove(abilityInfo);
    }
}
