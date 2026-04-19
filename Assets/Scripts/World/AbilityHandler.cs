using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

public class AbilityHandler : MonoBehaviour
{
    [System.Serializable]
    public class AbilityCooldown
    {
        public AbilityInfo ability;
        public int remainingCooldown;

        public AbilityCooldown() { }
        public AbilityCooldown(AbilityInfo ability, int remainingCooldown)
        {
            this.ability = ability;
            this.remainingCooldown = remainingCooldown;
        }
    }

    [HideInInspector] public List<AbilityCooldown> cooldowns = new List<AbilityCooldown>(); // Cooldowns of abilities
    [HideInInspector] public AbilityInfo currentAbility; // Ability that is going to be cast
    [HideInInspector] public PlayerController playerController; //TODO: singleton Reference to the player controller class
    private List<HexGridLayout.HexNode> validHexes = new List<HexGridLayout.HexNode>(); // List of hexes that the ability currently being cast is able to be used on

#region Helper Functions
    /// <summary>
    /// Checks if the hex is within range
    /// </summary>
    /// <param name="hex">Hex to check</param>
    /// <returns>Returns true if the provided hex is within range</returns>
    public bool IsWithinRange(HexGridLayout.HexNode hex)
    {
        if (validHexes.IndexOf(hex) == -1)
            return false;
        return true;
    }

    /// <summary>
    /// Checks if the provided hex is a valid target for the ability
    /// </summary>
    /// <param name="hex">Hex to check</param>
    /// <returns>Returns true if the provided hex is a valid target for the ability</returns>
    public bool IsHexValid(HexGridLayout.HexNode hex)
    {
        if (!IsWithinRange(hex))
            return false;
        // Here we handle the abilities that require targets
        if (currentAbility.requiresTarget && hex.hexRenderer.occupying.Value == null)
            return false;
        return true;
    }
#endregion

#region Public Functions
    /// <summary>
    /// Function that selects an ability for casting
    /// </summary>
    /// <param name="selectedAbility">Information of the ability that is going to be cast</param>
    /// <returns></returns>
    public IEnumerator PrepareCasting(AbilityInfo selectedAbility)
    {
        // Store the ability for later use
        currentAbility = selectedAbility;

        yield return null;

        // We want to disable the ability UI and enable the tooltip to constantly show what the ability is doing
        UIManager.instance.abilitiesUIManager.ShowAbilities(false);
        UIManager.instance.tooltipHandler.ShowAbilityTooltip(currentAbility, new Vector3(Screen.width / 2, 0, 0));

        // Display the range of the ability
        ShowRange();
    }

    /// <summary>
    /// Function that handles the cancellation of an ability
    /// </summary>
    public void CancelCasting()
    {
        Debug.Log("Cancelled Ability");

        // Hide the ability tooltip and show the ability UI again
        UIManager.instance.abilitiesUIManager.ShowAbilities(true);
        UIManager.instance.tooltipHandler.HideTooltip();

        // Reset the range visuals to their original color
        foreach (HexGridLayout.HexNode hex in validHexes)
            hex.hexRenderer.ChangeColorToOriginal();
        
        // Empty the validHexes list
        validHexes = new List<HexGridLayout.HexNode>();

        // Reset the current ability
        currentAbility = null;
    }

    /// <summary>
    /// Function that confirms the casting of the ability
    /// </summary>
    /// <param name="affectedNodes">List of hexes that will be affected by the ability</param>
    /// <param name="centerNode">Center hex of the ability</param>
    public void ConfirmCasting(List<HexGridLayout.HexNode> affectedNodes, HexGridLayout.HexNode centerNode)
    {
        Debug.Log("Cast Ability");
        // Show the abilities UI and disable the tooltip
        UIManager.instance.abilitiesUIManager.ShowAbilities(true);
        UIManager.instance.tooltipHandler.HideTooltip();

        // Revert the hexes back to their original color
        foreach (HexGridLayout.HexNode hex in validHexes)
            hex.hexRenderer.ChangeColorToOriginal();
        validHexes = new List<HexGridLayout.HexNode>();


        List<HexRenderer> hexRenderers = new List<HexRenderer>();
        foreach (HexGridLayout.HexNode hex in affectedNodes)
            hexRenderers.Add(hex.hexRenderer);

        // Applying the effects of the abilities to each hex
        ApplyHexEffects(hexRenderers,
                        currentAbility.useWeaponDamage ? currentAbility.GetDamage(1) /* TODO: replace with actual weapon damage */ : currentAbility.GetDamage(),
                        currentAbility.lingeringDuration,
                        currentAbility.element,
                        currentAbility.isHeal);

        // Handle the charge to target ability case
        if (currentAbility.chargeToTarget && centerNode != playerController.currentPosition)
        {
            List<HexGridLayout.HexNode> tempPath = Pathfinder.FindPath(playerController.currentPosition, centerNode);
            if (centerNode.hexRenderer.occupying.Value)
                tempPath.RemoveAt(tempPath.Count - 1);
            if (tempPath.Count > 0)
            {
                playerController.UpdateHex(playerController.currentPosition.hexObj.name, null);
                playerController.UpdateCurrentlyOn(tempPath.Last().hexRenderer.name);
                playerController.currentPosition = tempPath.Last();
                playerController.UpdateHex(playerController.currentPosition.hexObj.name, gameObject);

                playerController.path = tempPath;
            }
        }

        // Add the cooldown for the ability we just used
        cooldowns.Add(new AbilityCooldown(currentAbility, currentAbility.cooldown));
        UIManager.instance.abilitiesUIManager.UpdateAbilityCooldownsUI(cooldowns);

        currentAbility = null;

        // Force the end turn (maybe we will change this idk)
        playerController.EndTurn();
    }

    /// <summary>
    /// Function that handles the reduction of ability cooldowns at the start of the turn
    /// </summary>
    /// <param name="turn"></param>
    public void ReduceCooldowns(int turn) // TODO: remove the turn parameter
    {
        if (!GameManager.instance.IsMyTurn())
            return;
        Debug.Log($"{turn} Reducing Cooldowns");

        // Here we want to create a new list and add the cooldowns that are still remaining (> 0 turns)
        List<AbilityCooldown> newCooldowns = new List<AbilityCooldown>();
        foreach (AbilityCooldown abilityCooldown in cooldowns)
        {
            abilityCooldown.remainingCooldown -= 1;
            if (abilityCooldown.remainingCooldown > 0)
                newCooldowns.Add(abilityCooldown);
        }
        cooldowns = newCooldowns;

        // Then we want to update the UI of each ability
        UIManager.instance.abilitiesUIManager.UpdateAbilityCooldownsUI(cooldowns);
    }
#endregion

#region Private Functions
    /// <summary>
    /// Function that builds the validHexes list by ability range and conditions
    /// </summary>
    private void ShowRange()
    {
        // Firstly we get all of the hexes that are within the ability range
        List<HexGridLayout.HexNode> filteredHexes = HexGridLayout.instance.hexNodes.Where(h => h.Distance(GetComponent<PlayerController>().currentPosition) <= currentAbility.range).ToList();

        foreach (HexGridLayout.HexNode hex in filteredHexes)
        {
            // If the ability charges to target we filter out the hexes that are unreachable
            if (currentAbility.chargeToTarget && hex != playerController.currentPosition)
            {
                // If it's an obstacle it is unreachable
                if (hex.hexRenderer.IsObstacle())
                    continue;
                
                // We try to form a path to the given hex
                List<HexGridLayout.HexNode> path = Pathfinder.FindPath(playerController.currentPosition, HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hex.hexRenderer.gameObject));
                if (path.Count > currentAbility.range)
                    path.RemoveRange(currentAbility.range, path.Count - currentAbility.range);

                // If the last hex of the path is different from the hex we are checking we remove it as well
                HexRenderer finalHex = path[path.Count - 1].hexRenderer;
                if (finalHex != hex.hexRenderer)
                    continue;
            }
            // If the hex is reachable we add it to our list
            validHexes.Add(hex);
        }

        // We color the hexes to a lighter shade in order to show the range
        foreach (HexGridLayout.HexNode hex in validHexes)
            hex.hexRenderer.ChangeColor(hex.hexRenderer.GetColor() + new Color(0.2f, 0.2f, 0.2f, 1f));
    }

    /// <summary>
    /// Function that handles the ability effects on affected hexes
    /// </summary>
    /// <param name="hexes">Hexes that will be affected by the ability</param>
    /// <param name="damageAmount">Value of the damage/heal of the ability</param>
    /// <param name="lingeringDuration">Lingering duration of ability</param>
    /// <param name="element">Element of the lingering</param>
    /// <param name="isHeal">Whether or not the ability is a heal</param>
    private void ApplyHexEffects(List<HexRenderer> hexes, int damageAmount, int lingeringDuration, AbilityInfo.Element element, bool isHeal)
    {
        foreach (HexRenderer hex in hexes)
        {
            // If the ability is a lingering one we apply the lingering effect to all of them
            if (lingeringDuration > 0)
                hex.ApplyLingering(GameManager.instance.LocalConnection.ClientId, element, lingeringDuration);

            // If a player is occupying a tile we apply the heal/damage to them
            if (hex.occupying.Value && hex.occupying.Value.TryGetComponent<PlayerInfo>(out PlayerInfo playerInfo))
            {
                if (isHeal)
                    playerInfo.Heal(damageAmount);
                else
                    playerInfo.TakeDamage(damageAmount);
            }
        }
    }
#endregion
}
