using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilitiesUIManager : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private Transform abilitySlotsRoot;
    [SerializeField] private GameObject abilitySlotPrefab;

    [SerializeField] private List<Button> abilitySlots;
    [SerializeField] private List<AbilityInfo> abilities;

    [Header("Variables")]
    [SerializeField] private float abilitySize;
    [SerializeField] private float abilitySpacing;


    private void OnAbilityClick(int index)
    {
        Debug.Log($"Using Ability {index}");
    }

    private void GenerateAbilityUI()
    {
        abilitySlots.Clear();
        foreach (Button child in abilitySlotsRoot.GetComponentsInChildren<Button>())
            Destroy(child.gameObject);
        if (abilities.Count == 0)
        {
            abilitySlotsRoot.gameObject.SetActive(false);
            return;
        }
        abilitySlotsRoot.gameObject.SetActive(true);
        RectTransform rootRect = abilitySlotsRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2((abilitySize + abilitySpacing) * abilities.Count + abilitySpacing, abilitySize + abilitySpacing * 2);
        Vector3 cursor = rootRect.position - Vector3.right * (rootRect.sizeDelta.x / 2 - abilitySpacing - abilitySize / 2) + Vector3.up * rootRect.sizeDelta.y / 2;
        for (int i = 0; i < abilities.Count; i++)
        {
            Button createdButton = Instantiate(abilitySlotPrefab, cursor, Quaternion.identity, abilitySlotsRoot).GetComponent<Button>();
            int localI = i;
            createdButton.onClick.AddListener(delegate { OnAbilityClick(localI); });
            HoverHandler hoverLogic = createdButton.GetComponent<HoverHandler>();
            hoverLogic.abilityInfo = abilities[i];
            hoverLogic.position = cursor + Vector3.up * abilitySize;
            cursor += Vector3.right * (abilitySize + abilitySpacing);
            createdButton.GetComponent<RectTransform>().sizeDelta = new Vector2(abilitySize, abilitySize);
            createdButton.GetComponent<Image>().sprite = abilities[i].displayImage;
            abilitySlots.Add(createdButton);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        GenerateAbilityUI();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
