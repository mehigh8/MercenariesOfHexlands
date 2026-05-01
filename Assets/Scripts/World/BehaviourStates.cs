using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base class of the behaviour state.<br/>
/// Provides abstract IEnumerator function which contains the actions to be taken by the NPC.
/// </summary>
public abstract class BehaviourStateBase
{
    public List<Pair<BehaviourSwitchConditionBase, BehaviourStateBase>> connections; // List containing pairs of switch conditions and states representing the links of the behaviour tree

    public NPCBehaviour npc; // Reference to the NPCBehaviour of this NPC

    /// <summary>
    /// Checks if the current state has to switch.
    /// </summary>
    /// <returns>The state that should be used</returns>
    public BehaviourStateBase UpdateState()
    {
        if (connections == null || connections.Count == 0)
            return this;

        foreach (var connection in connections)
        {
            if (connection != null && connection.item1.CheckCondition())
                return connection.item2;
        }
        return this;
    }

    /// <summary>
    /// IEnumerator containing the actions of the NPC.
    /// </summary>
    /// <returns>-</returns>
    public abstract IEnumerator DoStateActions();

    /// <summary>
    /// Function used to set the connections of this state
    /// </summary>
    /// <param name="connections">Connections to be stored and used by this state</param>
    public void SetConnections(List<Pair<BehaviourSwitchConditionBase, BehaviourStateBase>> connections)
    {
        this.connections = connections;
    }
}

/// <summary>
/// In this state the NPC will wonder around randomly and heal if possible
/// </summary>
public class WanderState : BehaviourStateBase
{ 
    public WanderState(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override IEnumerator DoStateActions()
    {
        Debug.Log(npc.npcName.Value + ": Wander state");

        // Get all hexes in range that are not obstacles or occupied
        List<HexGridLayout.HexNode> hexesInRange = HexGridLayout.instance.hexNodes.Where(hex => hex.Distance(npc.currentHexNode) <= npc.npcInfo.movement && !hex.hexRenderer.IsObstacle() && hex.hexRenderer.occupying.Value == null).ToList();

        // Randomly choose a hex in the list that can be reached with the NPC's movement value. Keep searching until one such hex is found or there are no more hexes to choose from
        List<HexGridLayout.HexNode> path = null;
        HexGridLayout.HexNode dest = null;
        while ((path == null || path.Count > npc.npcInfo.movement) && hexesInRange.Count > 0)
        {
            dest = hexesInRange[Random.Range(0, hexesInRange.Count)];
            hexesInRange.Remove(dest);

            // Calculate path to the chosen hex
            path = Pathfinder.FindPath(npc.currentHexNode, dest);
        }

        // If a path was found start the movement process
        if (path.Count <= npc.npcInfo.movement)
        {
            npc.Move(path);
            // Wait for the movement to finish
            yield return new WaitWhile(delegate { return npc.isMoving; });
        }
        else
        {
            // If a path could not be found, the movement action will be skipped for now and a warning will be sent
            Debug.LogWarning("Could not find hex to move to");
        }

        // Try to heal
        npc.Heal();

        npc.EndTurn();
    }
}

/// <summary>
/// In this state, the NPC will run away from the threat and heal if possible
/// </summary>
public class RunState : BehaviourStateBase
{
    public RunState(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override IEnumerator DoStateActions()
    {
        if (npc.threat != null)
        {
            Debug.Log(npc.npcName.Value + ": Run state");
            // Get all hexes in range that are not obstacles or occupied and that are further away from the threat than the current hex
            List<HexGridLayout.HexNode> hexesInRange = HexGridLayout.instance.hexNodes.Where(hex => hex.Distance(npc.currentHexNode) <= npc.npcInfo.movement
                && !hex.hexRenderer.IsObstacle()
                && hex.hexRenderer.occupying.Value == null
                && hex.Distance(npc.threat.currentPosition) > npc.currentHexNode.Distance(npc.threat.currentPosition)).ToList();

            // Sort possible hexes descending by distance to threat
            hexesInRange.Sort((a, b) => b.Distance(npc.threat.currentPosition) - a.Distance(npc.threat.currentPosition));

            // Try each hex one by one
            List<HexGridLayout.HexNode> path = null;
            foreach (HexGridLayout.HexNode hex in hexesInRange)
            {
                path = Pathfinder.FindPath(npc.currentHexNode, hex);
                if (path != null && path.Count <= npc.npcInfo.movement)
                    break;
            }

            npc.Move(path);
            // Wait for the movement to finish
            yield return new WaitWhile(delegate { return npc.isMoving; });
        }
        else
        {
            Debug.LogWarning("There is no threat to run from");
        }
        
        // Try to heal
        npc.Heal();

        npc.EndTurn();
    }
}

/// <summary>
/// In this state, the NPC will go towards the threat and attack
/// </summary>
public class AttackState : BehaviourStateBase
{
    public AttackState(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override IEnumerator DoStateActions()
    {
        if (npc.threat != null)
        {
            Debug.Log(npc.npcName.Value + ": Attack state");
            // Get all hexes in range that are not obstacles or occupied and that are closer to the threat than the current hex
            List<HexGridLayout.HexNode> hexesInRange = HexGridLayout.instance.hexNodes.Where(hex => hex.Distance(npc.currentHexNode) <= npc.npcInfo.movement
                && !hex.hexRenderer.IsObstacle()
                && hex.hexRenderer.occupying.Value == null
                && hex.Distance(npc.threat.currentPosition) < npc.currentHexNode.Distance(npc.threat.currentPosition)).ToList();

            // Sort possible hexes ascending by distance to threat
            hexesInRange.Sort((a, b) => a.Distance(npc.threat.currentPosition) - b.Distance(npc.threat.currentPosition));

            // Try each hex one by one
            List<HexGridLayout.HexNode> path = null;
            foreach (HexGridLayout.HexNode hex in hexesInRange)
            {
                path = Pathfinder.FindPath(npc.currentHexNode, hex);
                if (path != null && path.Count <= npc.npcInfo.movement)
                    break;
            }

            npc.Move(path);
            // Wait for the movement to finish
            yield return new WaitWhile(delegate { return npc.isMoving; });
        }
        else
        {
            Debug.LogWarning("There is no threat to attack");
        }

        // Try to attack. If not able to, heal
        if (!npc.Attack())
            npc.Heal();

        npc.EndTurn();
    }
}

public class SuicideState : BehaviourStateBase
{
    public SuicideState(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override IEnumerator DoStateActions()
    {
        if (npc.threat != null)
        {
            Debug.Log(npc.npcName.Value + ": Attack state");
            // Get all hexes in range that are not obstacles or occupied and that are closer to the threat than the current hex
            List<HexGridLayout.HexNode> hexesInRange = HexGridLayout.instance.hexNodes.Where(hex => hex.Distance(npc.currentHexNode) <= npc.npcInfo.movement
                && !hex.hexRenderer.IsObstacle()
                && hex.hexRenderer.occupying.Value == null
                && hex.Distance(npc.threat.currentPosition) < npc.currentHexNode.Distance(npc.threat.currentPosition)).ToList();

            // Sort possible hexes ascending by distance to threat
            hexesInRange.Sort((a, b) => a.Distance(npc.threat.currentPosition) - b.Distance(npc.threat.currentPosition));

            // Try each hex one by one
            List<HexGridLayout.HexNode> path = null;
            foreach (HexGridLayout.HexNode hex in hexesInRange)
            {
                path = Pathfinder.FindPath(npc.currentHexNode, hex);
                if (path != null && path.Count <= npc.npcInfo.movement)
                    break;
            }

            npc.Move(path);
            // Wait for the movement to finish
            yield return new WaitWhile(delegate { return npc.isMoving; });

            AbilityInfo selfDestruct = null;
            foreach (AbilityInfo ability in npc.npcInfo.abilities)
                if (ability.name == "Self Destruct")
                    selfDestruct = ability;

            if (selfDestruct == null)
            {
                Debug.LogError("This state requires the <Self Destruct> ability");
            }
            else
            {
                if (npc.currentHexNode.Distance(npc.threat.currentPosition) <= selfDestruct.areOfEffect)
                {
                    npc.Attack();
                }
            }
        }
        else
        {
            Debug.LogWarning("There is no threat to attack");
        }

        npc.EndTurn();
    }
}
