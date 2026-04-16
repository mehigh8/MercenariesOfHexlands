using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BehaviourStateBase
{
    public List<Pair<BehaviourSwitchConditionBase, BehaviourStateBase>> connections;

    public NPCBehaviour npc;

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

    public abstract void DoStateActions();

    public void SetConnections(List<Pair<BehaviourSwitchConditionBase, BehaviourStateBase>> connections)
    {
        this.connections = connections;
    }
}

public class WanderState : BehaviourStateBase
{ 
    public WanderState(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override void DoStateActions()
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
        }
        else
        {
            // If a path could not be found, the movement action will be skipped for now and a warning will be sent
            Debug.LogWarning("Could not find hex to move to");
        }

        npc.Heal();
    }
}

public class RunState : BehaviourStateBase
{
    public RunState(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override void DoStateActions()
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
        }
        else
        {
            Debug.LogWarning("There is no threat to run from");
        }

        npc.Heal();
    }
}

public class AttackState : BehaviourStateBase
{
    public AttackState(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override void DoStateActions()
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
        }
        else
        {
            Debug.LogWarning("There is no threat to run from");
        }

        npc.Attack();
        // TODO: Add check to see if any skill is in range. If not, heal (if able)
    }
}
