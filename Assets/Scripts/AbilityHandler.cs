using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilityHandler : MonoBehaviour
{
    [HideInInspector] public AbilityInfo currentAbility;
    [HideInInspector] public PlayerController playerController;
    private List<HexGridLayout.HexNode> validHexes = new List<HexGridLayout.HexNode>();

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

    public void ConfirmCasting(HexGridLayout.HexNode centerNode)
    {
        Debug.Log("Cast Ability");
        UIManager.instance.abilitiesUIManager.ShowAbilities(true);
        UIManager.instance.tooltipHandler.HideTooltip();

        foreach (HexGridLayout.HexNode hex in validHexes)
            hex.hexRenderer.ChangeColorToOriginal();
        validHexes = new List<HexGridLayout.HexNode>();

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
