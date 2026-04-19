using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{

    [Header("Permanent buttons")]
    public Button inventoryButton; // Reference to the button used to open the inventory
    public Button endTurnButton; // Reference to the button used to end turn
    [Header("Other references")]
    [SerializeField] private GameObject buttonPrefab; // Prefab of HUD button
    [SerializeField] private GameObject hudObject; // Reference to the HUD game object

    private int additionalButtonsCount = 0; // Counter used to keep track of active additional buttons
    private List<Button> additionalButtons = new List<Button>(); // List of additional buttons
    private bool isOpened = false; // Specifies if the HUD is opened

    #region Unity Functions
    private void Update()
    {
        if (!isOpened && GameManager.instance.IsMyTurn() && GameLobbyManager.Instance.gameStarted)
            OpenHUD();
    }
    #endregion

    #region HUD Interaction
    /// <summary>
    /// Function used to open/display the HUD
    /// </summary>
    private void OpenHUD()
    {
        hudObject.SetActive(true);
        isOpened = true;
    }

    /// <summary>
    /// Function used to close the HUD
    /// </summary>
    public void CloseHUD()
    {
        hudObject.SetActive(false);
        isOpened = false;
    }

    /// <summary>
    /// Function used to add buttons to the HUD
    /// </summary>
    /// <param name="buttonText">Text to be displayed on button</param>
    /// <param name="buttonAction">Action that the new button should do when pressed</param>
    public void AddButton(string buttonText, UnityAction buttonAction)
    {
        additionalButtonsCount++;

        // Instantiate button
        GameObject buttonObject = Instantiate(buttonPrefab, hudObject.transform);
        buttonObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(-125f, 50f * (additionalButtonsCount + 1));

        // Configure button
        Button additionalButton = buttonObject.GetComponent<Button>();
        additionalButton.onClick.RemoveAllListeners();
        additionalButton.onClick.AddListener(buttonAction);

        buttonObject.GetComponentInChildren<TMP_Text>().text = buttonText;

        // Store button
        additionalButtons.Add(additionalButton);
    }

    /// <summary>
    /// Function used to clear all additional button from HUD
    /// </summary>
    public void ClearButtons()
    {
        foreach (Button button in additionalButtons)
        {
            button.onClick.RemoveAllListeners();
            Destroy(button.gameObject);
        }

        additionalButtons.Clear();

        additionalButtonsCount = 0;
    }
    #endregion
}
