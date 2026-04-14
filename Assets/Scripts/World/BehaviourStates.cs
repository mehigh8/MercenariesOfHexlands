using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BehaviourStateBase
{
    public BehaviourSwitchConditionBase switchCondition;
    public BehaviourStateBase toState;

    public NPCBehaviour npc;

    public BehaviourStateBase UpdateState()
    {
        if (switchCondition == null)
            return this;

        return switchCondition.CheckCondition() ? toState : this;
    }

    public abstract void DoStateActions();
}

public class WanderState : BehaviourStateBase
{ 
    public WanderState(NPCBehaviour npc, BehaviourSwitchConditionBase switchCondition, BehaviourStateBase toState)
    {
        this.npc = npc;
        this.switchCondition = switchCondition;
        this.toState = toState;
    }

    public override void DoStateActions()
    {
        
    }
}
