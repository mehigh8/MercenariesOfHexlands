using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [HideInInspector] public AbilitiesUIManager abilitiesUIManager;

    void Start()
    {
        abilitiesUIManager = GetComponent<AbilitiesUIManager>();
    }

    public static UIManager instance;
    void Awake()
    {
        instance = this;
    }

}
