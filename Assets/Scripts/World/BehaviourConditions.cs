using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public abstract class BehaviourSwitchConditionBase
{
    protected NPCBehaviour npc;
    public abstract bool CheckCondition();
}

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
