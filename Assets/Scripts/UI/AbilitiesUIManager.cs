using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilitiesUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform abilitySlotsRoot; // Reference to root object of ability objects
    [SerializeField] private GameObject abilitySlotPrefab; // Prefab of ability object

    [SerializeField] private List<AbilityInfo> abilities; // Contains a list of all of the abilities currently held by the player (This is serialized because we add the default attack in the editor)

    [Header("Variables")]
    [SerializeField] private float abilitySize; // Size of the ability UI element in pixels
    [SerializeField] private float abilitySpacing; // Distance in pixels between each ability UI element

    [HideInInspector] public AbilityHandler client; // Reference to the script that handles ability casting
    private List<Button> abilitySlots = new List<Button>(); // References to all abilities in UI

#region Unity Functions
    void Start()
    {
        GenerateAbilityUI();
        ShowAbilities(false);
        UpdateAbilityCooldownsUI(new List<AbilityHandler.AbilityCooldown>());
    }
#endregion

#region Public Functions
    /// <summary>
    /// Function that updates the displays of ability cooldowns
    /// </summary>
    /// <param name="cooldowns">The list of current cooldowns, it is found in the AbilityHandler</param>
    public void UpdateAbilityCooldownsUI(List<AbilityHandler.AbilityCooldown> cooldowns)
    {
        // We first presume that all of the abilities are ready
        foreach (Button slot in abilitySlots)
        {
            slot.GetComponentInChildren<TextMeshProUGUI>().text = "";
            slot.interactable = true;
        }

        // We iterate through all of the abilities
        for (int i = 0; i < abilitySlots.Count; i++)
        {
            Button currentSlot = abilitySlots[i];
            AbilityInfo currentAbility = abilities[i];

            // Here we try to find if the current ability has remaining cooldown inside our cooldown list
            List<AbilityHandler.AbilityCooldown> foundCooldown = cooldowns.Where(c => c.ability == currentAbility).ToList();
            if (foundCooldown.Count > 0)
            {
                // We then set the object as not interactable and assign it its cooldown to the display
                TextMeshProUGUI cooldownText = currentSlot.GetComponentInChildren<TextMeshProUGUI>();
                cooldownText.text = foundCooldown[0].remainingCooldown.ToString();
                currentSlot.interactable = false;
            }
        }
    }

    /// <summary>
    /// Function that regenerates the ability UI panel, this should usually be called when we update the ability list
    /// </summary>
    public void GenerateAbilityUI()
    {
        // Clear the reference holder for the slots
        abilitySlots.Clear();

        // Clear the parent object of the old UI
        foreach (Button child in abilitySlotsRoot.GetComponentsInChildren<Button>())
            Destroy(child.gameObject);

        // If we have no abilities, hide the UI (This is old since now we always have the melee attack by default)
        if (abilities.Count == 0)
        {
            ShowAbilities(false);
            return;
        }

        // If we have at least one ability we want to display the UI
        ShowAbilities(true);

        // This was changed since UI components handle all of the resizing
        for (int i = 0; i < abilities.Count; i++)
        {
            Button createdButton = Instantiate(abilitySlotPrefab, abilitySlotsRoot).GetComponent<Button>();
            
            // We do this since the reference to i will be lost after the loop is over
            int localI = i;
            createdButton.onClick.AddListener(delegate { OnAbilityClick(localI); });

            // Here we set the dimensions of the UI elements to match the parameters provided and the image of the ability
            createdButton.GetComponent<RectTransform>().sizeDelta = new Vector2(abilitySize, abilitySize);
            createdButton.GetComponentInChildren<TextMeshProUGUI>().GetComponent<RectTransform>().sizeDelta = new Vector2(abilitySize, abilitySize);
            createdButton.GetComponent<Image>().sprite = abilities[i].displayImage;
            
            // Here we prepare the information for the tooltip display: the information and the offset
            HoverHandler hoverLogic = createdButton.GetComponent<HoverHandler>();
            hoverLogic.abilityInfo = abilities[i];
            hoverLogic.position = abilitySlotsRoot.position + Vector3.up * abilitySize;
            
            // Finally, we add the instantiated slot to our ability list
            abilitySlots.Add(createdButton);
        }
        // Here we also want to update the ability cooldowns
        if (client)
            UpdateAbilityCooldownsUI(client.cooldowns);
    }
#endregion

#region Helper Functions
    /// <summary>
    /// Function for displaying and hiding the abilities panel
    /// </summary>
    /// <param name="shouldShow">Whether to show or hide the abilities panel</param>
    public void ShowAbilities(bool shouldShow)
    {
        abilitySlotsRoot.gameObject.SetActive(shouldShow);
    }

    /// <summary>
    /// Logic for checking whether a specific ability is inside our ability pool or not
    /// </summary>
    /// <param name="abilityInfo">Info of the ability to try to find</param>
    /// <returns></returns>
    public bool HasAbility(AbilityInfo abilityInfo)
    {
        return abilities.Contains(abilityInfo);
    }

    /// <summary>
    /// Logic for adding a specific ability to our ability pool
    /// </summary>
    /// <param name="abilityInfo">Info of the ability to be added</param>
    public void AddAbility(AbilityInfo abilityInfo)
    {
        abilities.Add(abilityInfo);
    }

    /// <summary>
    /// Logic for removing a specific ability from our ability pool
    /// </summary>
    /// <param name="abilityInfo">Info of the ability to be removed</param>
    public void RemoveAbility(AbilityInfo abilityInfo)
    {
        abilities.Remove(abilityInfo);
    }
#endregion

#region Button Functions
    /// <summary>
    /// Function that is called when clicking the corresponding UI element of the ability
    /// </summary>
    /// <param name="index">Index of the ability to be cast on click</param>
    private void OnAbilityClick(int index)
    {
        if (!client.playerController) // TODO: replace with actual instance of playercontroller
            return;
        // If we are moving or it is not our turn we want this button to do nothing (we may want to turn this into disabling the buttons but idk)
        if (!GameManager.instance.IsMyTurn() || client.playerController.isMoving())
            return;

        // Here we start the ability casting logic
        StartCoroutine(client.PrepareCasting(abilities[index]));
    }
#endregion
}
