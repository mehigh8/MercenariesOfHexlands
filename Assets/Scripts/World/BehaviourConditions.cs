using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base class of state switch conditions.<br/>
/// It provides an abstract condition function which contains the logic that decides whether the state should switch or not.
/// </summary>
[System.Serializable]
public abstract class BehaviourSwitchConditionBase
{
    protected NPCBehaviour npc; // Reference to the NPCBehaviour of this npc
    /// <summary>
    /// Condition function which is used to switch the state or not
    /// </summary>
    /// <returns>True - State has to switch; False - State remains the same</returns>
    public abstract bool CheckCondition();
}

/// <summary>
/// Return true if the NPC has been hit (this is tested by checking if the NPC has a threat set)
/// </summary>
public class HitCondition : BehaviourSwitchConditionBase
{
    public HitCondition(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override bool CheckCondition()
    {
        return npc.threat != null;
    }
}

/// <summary>
/// Return true if the NPC has been hit (if it has threat) or if there is any player in its detection range (in which case it will set that player as a threat)
/// </summary>
public class HitOrSeenCondition : BehaviourSwitchConditionBase
{
    public HitOrSeenCondition(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override bool CheckCondition()
    {
        if (npc.threat != null)
            return true;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            PlayerController possibleThreat = player.GetComponent<PlayerController>();

            if (npc.currentHexNode.Distance(possibleThreat.currentPosition) <= npc.npcInfo.detection)
            {
                npc.threat = possibleThreat;
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Return true if the distance between the NPC and the threat is greater than the configured range
/// </summary>
public class FarFromThreatCondition : BehaviourSwitchConditionBase
{
    public FarFromThreatCondition(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override bool CheckCondition()
    {
        if (npc.threat == null || npc.threat.currentPosition.Distance(npc.currentHexNode) >= npc.npcInfo.runDistance)
        {
            npc.threat = null;
            return true;
        }

        return false;
    }
}

/// <summary>
/// Return true if the health of the NPC is lower than the configured threshold
/// </summary>
public class LowOnHealthCondition : BehaviourSwitchConditionBase
{
    public LowOnHealthCondition(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override bool CheckCondition()
    {
        return npc.currentHealth.Value <= npc.npcInfo.healthLowThreshold;
    }
}

/// <summary>
/// Return true if the health of the NPC is greater than the configured threshold
/// </summary>
public class HealthyCondition : BehaviourSwitchConditionBase
{
    public HealthyCondition(NPCBehaviour npc)
    {
        this.npc = npc;
    }

    public override bool CheckCondition()
    {
        return npc.currentHealth.Value >= npc.npcInfo.healthyThreshold;
    }
}
