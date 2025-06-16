using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

public class AbilityHandler : NetworkBehaviour
{
    [HideInInspector] public AbilityInfo currentAbility;
    [HideInInspector] public PlayerController playerController;
    private List<HexGridLayout.HexNode> validHexes = new List<HexGridLayout.HexNode>();

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

    [HideInInspector] public List<AbilityCooldown> cooldowns = new List<AbilityCooldown>();

    public void ReduceCooldowns(int turn)
    {
        if (!GameManager.instance.IsMyTurn())
            return;
        Debug.Log($"{turn} Reducing Cooldowns");
        List<AbilityCooldown> newCooldowns = new List<AbilityCooldown>();
        foreach (AbilityCooldown abilityCooldown in cooldowns)
        {
            abilityCooldown.remainingCooldown -= 1;
            if (abilityCooldown.remainingCooldown > 0)
                newCooldowns.Add(abilityCooldown);
        }
        cooldowns = newCooldowns;

        UIManager.instance.abilitiesUIManager.UpdateAbilityCooldownsUI(cooldowns);
    }

    public bool IsWithinRange(HexGridLayout.HexNode hex)
    {
        if (validHexes.IndexOf(hex) == -1)
            return false;
        return true;
    }

    public bool IsHexValid(HexGridLayout.HexNode hex)
    {
        if (!IsWithinRange(hex))
            return false;
        if (currentAbility.requiresTarget && hex.hexRenderer.occupying.Value == null)
            return false;
        return true;
    }

    private void ShowRange()
    {
        List<HexGridLayout.HexNode> filteredHexes = HexGridLayout.instance.hexNodes.Where(h => h.Distance(GetComponent<PlayerController>().currentPosition) <= currentAbility.range).ToList();

        foreach (HexGridLayout.HexNode hex in filteredHexes)
        {
            if (currentAbility.chargeToTarget && hex != playerController.currentPosition)
            {
                if (hex.hexRenderer.IsObstacle())
                    continue;
                List<HexGridLayout.HexNode> path = playerController.pathfinder.FindPath(playerController.currentPosition, HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hex.hexRenderer.gameObject));
                if (path.Count > currentAbility.range)
                    path.RemoveRange(currentAbility.range, path.Count - currentAbility.range);

                HexRenderer finalHex = path[path.Count - 1].hexRenderer;
                if (finalHex != hex.hexRenderer)
                    continue;
            }
            validHexes.Add(hex);
        }

        foreach (HexGridLayout.HexNode hex in validHexes)
            hex.hexRenderer.ChangeColor(hex.hexRenderer.GetColor() + new Color(0.2f, 0.2f, 0.2f, 1f));
    }

    public void CancelCasting()
    {
        Debug.Log("Cancelled Ability");
        UIManager.instance.abilitiesUIManager.ShowAbilities(true);
        UIManager.instance.tooltipHandler.HideTooltip();

        foreach (HexGridLayout.HexNode hex in validHexes)
            hex.hexRenderer.ChangeColorToOriginal();
        validHexes = new List<HexGridLayout.HexNode>();

        currentAbility = null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ApplyHexEffects(List<HexRenderer> hexes, int damageAmount, int lingeringDuration, AbilityInfo.Element element, bool isHeal)
    {
        foreach (HexRenderer hex in hexes)
        {
            if (lingeringDuration > 0)
                hex.lingeringEffect.Value = new HexRenderer.LingeringEffect(GameManager.instance.LocalConnection.ClientId, element, lingeringDuration);
            if (hex.occupying.Value && hex.occupying.Value.TryGetComponent<PlayerInfo>(out PlayerInfo playerInfo))
            {
                if (isHeal)
                    playerInfo.Heal(damageAmount);
                else
                    playerInfo.TakeDamage(damageAmount);
            }
        }
    }

    public void ConfirmCasting(List<HexGridLayout.HexNode> affectedNodes, HexGridLayout.HexNode centerNode)
    {
        Debug.Log("Cast Ability");
        UIManager.instance.abilitiesUIManager.ShowAbilities(true);
        UIManager.instance.tooltipHandler.HideTooltip();

        foreach (HexGridLayout.HexNode hex in validHexes)
            hex.hexRenderer.ChangeColorToOriginal();
        validHexes = new List<HexGridLayout.HexNode>();

        List<HexRenderer> hexRenderers = new List<HexRenderer>();
        foreach (HexGridLayout.HexNode hex in affectedNodes)
            hexRenderers.Add(hex.hexRenderer);

        ApplyHexEffects(hexRenderers,
                        currentAbility.useWeaponDamage ? currentAbility.GetDamage(1) /* TODO: replace with actual weapon damage */ : currentAbility.GetDamage(),
                        currentAbility.lingeringDuration,
                        currentAbility.element,
                        currentAbility.isHeal);

        if (currentAbility.chargeToTarget)
        {
            List<HexGridLayout.HexNode> tempPath = playerController.pathfinder.FindPath(playerController.currentPosition, centerNode);
            if (centerNode.hexRenderer.occupying.Value)
                tempPath.RemoveAt(tempPath.Count - 1);
            if (tempPath.Count > 0)
            {
                playerController.UpdateHex(playerController.currentlyOn.name, null);
                playerController.currentlyOn = tempPath.Last().hexRenderer;
                playerController.currentPosition = tempPath.Last();
                playerController.UpdateHex(playerController.currentlyOn.name, gameObject);

                playerController.path = tempPath;
            }
        }

        cooldowns.Add(new AbilityCooldown(currentAbility, currentAbility.cooldown));
        UIManager.instance.abilitiesUIManager.UpdateAbilityCooldownsUI(cooldowns);

        currentAbility = null;
    }

    public IEnumerator PrepareCasting(AbilityInfo selectedAbility)
    {
        currentAbility = selectedAbility;

        yield return null;

        UIManager.instance.abilitiesUIManager.ShowAbilities(false);
        UIManager.instance.tooltipHandler.ShowAbilityTooltip(currentAbility, new Vector3(Screen.width / 2, 0, 0));

        ShowRange();
    }
}
