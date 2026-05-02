using System;
using System.Collections;
using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : NetworkBehaviour
{
    [Header("Player Stats")]
    [AllowMutableSyncType] public SyncVar<string> playerName; // Player's name
    [AllowMutableSyncType] public SyncVar<int> maxHealth = new SyncVar<int>(); // Player's maximum health
    [AllowMutableSyncType] public SyncVar<int> currentHealth = new SyncVar<int>(); // Player's current health
    public int damage; // Player's damage stat
    [Range(0f, 1f)]
    public float critChance; // Player's critical chance stat
    public int movementPerTurn; // Player's movement stat
    public int defence; // Player's defence stat
    public int viewingRange; // Player's viewing range
    [Header("References")]
    [SerializeField] private TMP_Text nameText; // Reference to the text object used for the name above the player
    [SerializeField] private Slider healthBar; // Reference to the health bar above the player
    [SerializeField] private TMP_Text healthText; // Reference to the health text above the player

    [HideInInspector] public int canMoveThisTurn; // Variable used to keep track of how many tiles can the player move this turn

    #region Unity + FishNet Functions
    private void Awake()
    {
        // Set callback to update health bar
        currentHealth.OnChange += OnCurrentHealthChange;

        // Initialize current health and movement this turn
        currentHealth.Value = maxHealth.Value;
        canMoveThisTurn = movementPerTurn;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner && GameManager.instance.IsMyTurn())
        {
            Debug.Log("Opening HUD for client " + LocalConnection.ClientId);
            UIManager.instance.hudManager.OpenHUD();
        }
        Set player's name from Steam
        StartCoroutine(SetPlayerName());
    }
    #endregion

    #region Health Related Functions
    /// <summary>
    /// Function used when the player has died
    /// </summary>
    public void Die()
    {
        print("Am murit");
        // Update GameManager and Hex
        GameManager.instance.PlayerDied(OwnerId);
        HexGridLayout.instance.UpdateHex(GetComponent<PlayerController>().currentlyOn.Value, null);

        // If it was this player's turn, end it
        if (GameManager.instance.currentPlayerTurn.Value == OwnerId)
            GameManager.instance.NextTurn();

        GetComponent<NetworkObject>().Despawn();
    }

    /// <summary>
    /// Function used when the player has taken damage
    /// </summary>
    /// <param name="amount">Amount of damage taken</param>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamage(int amount)
    {
        // Update current health and call Die if it reached 0
        currentHealth.Value = Math.Max(0, currentHealth.Value - amount);
        if (currentHealth.Value == 0)
            Die();
    }

    /// <summary>
    /// Function used when the player receives a heal
    /// </summary>
    /// <param name="amount">Amount of health healed</param>
    [ServerRpc(RequireOwnership = false)]
    public void Heal(int amount)
    {
        currentHealth.Value = Math.Min(maxHealth.Value, currentHealth.Value + amount);
    }

    /// <summary>
    /// Callback used to update the health bar when the player's current health has chaged
    /// </summary>
    /// <param name="oldVal">Previous health amount</param>
    /// <param name="newVal">New health amount</param>
    /// <param name="asServer">Bool used to specify if this callback is called on the Server</param>
    public void OnCurrentHealthChange(int oldVal, int newVal, bool asServer)
    {
        healthBar.value = (float)newVal / maxHealth.Value;
        healthText.text = newVal + " / " + maxHealth.Value;
    }
    #endregion

    #region Item Related Functions
    /// <summary>
    /// Function used for an item that was equipped in order to update the player's stats
    /// </summary>
    /// <param name="item">Item that was equipped</param>
    public void EquipItem(ItemInfo item)
    {
        if (item == null)
            return;

        // Update player stats based on the item's stats
        foreach (ItemInfo.ModifyStat stat in item.modifiedStats)
        {
            switch (stat.stat)
            {
                case ItemInfo.AffectedStat.Movement:
                    movementPerTurn += stat.value;
                    canMoveThisTurn += stat.value;
                    break;
                case ItemInfo.AffectedStat.Damage:
                    damage += stat.value;
                    break;
                case ItemInfo.AffectedStat.Defence:
                    defence += stat.value;
                    break;
                case ItemInfo.AffectedStat.Crit:
                    critChance += stat.value / 100f;
                    break;
                case ItemInfo.AffectedStat.Health:
                    float healthPercentage = (float)currentHealth.Value / maxHealth.Value;
                    maxHealth.Value += stat.value;
                    currentHealth.Value = (int)Mathf.Ceil(healthPercentage * maxHealth.Value);
                    break;
            }
        }

        // Update the player's abilities based on the item's abilities
        foreach (AbilityInfo ability in item.abilities)
        {
            if (!UIManager.instance.abilitiesUIManager.HasAbility(ability))
            {
                UIManager.instance.abilitiesUIManager.AddAbility(ability);
                UIManager.instance.abilitiesUIManager.GenerateAbilityUI();
            }
        }
    }

    /// <summary>
    /// Function used for an item that was unequipped to update the player's stats
    /// </summary>
    /// <param name="item">Item that was unequipped</param>
    public void UnequipItem(ItemInfo item)
    {
        if (item == null)
            return;

        // Update player's stats based on the item's stats
        foreach (ItemInfo.ModifyStat stat in item.modifiedStats)
        {
            switch (stat.stat)
            {
                case ItemInfo.AffectedStat.Movement:
                    movementPerTurn -= stat.value;
                    canMoveThisTurn -= stat.value;
                    break;
                case ItemInfo.AffectedStat.Damage:
                    damage -= stat.value;
                    break;
                case ItemInfo.AffectedStat.Defence:
                    defence -= stat.value;
                    break;
                case ItemInfo.AffectedStat.Crit:
                    critChance -= stat.value / 100f;
                    break;
                case ItemInfo.AffectedStat.Health:
                    float healthPercentage = (float)currentHealth.Value / maxHealth.Value;
                    maxHealth.Value -= stat.value;
                    currentHealth.Value = (int)Mathf.Ceil(healthPercentage * maxHealth.Value);
                    break;
            }
        }

        // Update the player's abilities based on the item's abilities
        foreach (AbilityInfo ability in item.abilities)
        {
            if (UIManager.instance.abilitiesUIManager.HasAbility(ability))
            {
                UIManager.instance.abilitiesUIManager.RemoveAbility(ability);
                UIManager.instance.abilitiesUIManager.GenerateAbilityUI();
            }
        }
    }
    #endregion

    #region Player Name Functions
    /// <summary>
    /// Server RPC used to tell the server to update the player's name
    /// </summary>
    /// <param name="name">New player name</param>
    /// <param name="id">Player ID</param>
    [ServerRpc]
    public void UpdateName(string name, int id)
    {
        if (name == "")
            name = "Player" + id;
        playerName.Value = name;
    }

    /// <summary>
    /// IEnumerator used to update the player's name
    /// </summary>
    /// <returns>-</returns>
    private IEnumerator SetPlayerName()
    {
        // Get name from steam and update SyncVar through RPC
        if (IsOwner)
        {
            UpdateName(SteamFriends.GetPersonaName(), LocalConnection.ClientId);
        }

        // Wait for the SyncVar to be updated
        while (playerName.Value == "")
            yield return null;

        nameText.text = playerName.Value;
    }
    #endregion
}
