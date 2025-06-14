using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [HideInInspector] public AbilitiesUIManager abilitiesUIManager;
    [HideInInspector] public TooltipHandler tooltipHandler;

    void Start()
    {
        abilitiesUIManager = GetComponent<AbilitiesUIManager>();
        tooltipHandler = GetComponent<TooltipHandler>();
    }

    public static UIManager instance;
    void Awake()
    {
        instance = this;
    }

}
