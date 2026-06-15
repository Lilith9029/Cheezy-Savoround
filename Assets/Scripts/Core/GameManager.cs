using UnityEngine;
using System;

public enum GameState
{
    Menu,
    Playing,
    CheckingCombo,
    Animating,
    GameOver
}

// GameManager runs before UIManager so Instance is ready when UIManager.Start() subscribes
[DefaultExecutionOrder(-10)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Menu;
    
    [Header("Score Settings")]
    [SerializeField] private int score = 0;
    [SerializeField] private int gold = 0;

    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int levelProgressScore = 0;
    [SerializeField] private int levelTargetScore = 500;

    // Events for UI or other components to listen to
    public static event Action<GameState> OnStateChanged;
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnGoldChanged;
    public static event Action<int, float> OnLevelProgressChanged;

    public GameState CurrentState => currentState;
    public int Score => score;
    public int Gold => gold;
    public int CurrentLevel => currentLevel;

    private MergeManager _mergeManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        _mergeManager = FindFirstObjectByType<MergeManager>();

        // Load User Data
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.LoadData();
            gold = UserDataManager.Instance.Gold;
        }

        // Force broadcast initial state to all listeners (UIManager, etc.)
        // Don't use ChangeState() here - it skips if state == currentState.
        // Instead broadcast directly so UIManager sets up the correct panels.
        OnStateChanged?.Invoke(currentState);
        OnScoreChanged?.Invoke(score);
        OnGoldChanged?.Invoke(gold);
        OnLevelProgressChanged?.Invoke(currentLevel, (float)levelProgressScore / levelTargetScore);
    }

    /// <summary>
    /// Changes the current state of the game and notifies listeners.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        
        OnStateChanged?.Invoke(currentState);

        switch (currentState)
        {
            case GameState.Menu:
                // Handle Menu entrance
                break;
            case GameState.Playing:
                // Enable player interactions
                break;
            case GameState.CheckingCombo:
                // Grid analysis active
                break;
            case GameState.Animating:
                // Moving parts active
                break;
            case GameState.GameOver:
                // Display game over panel
                break;
        }
    }

    /// <summary>
    /// Adds score points and triggers event.
    /// </summary>
    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);

        // Track level progress
        levelProgressScore += points;
        while (levelProgressScore >= levelTargetScore)
        {
            levelProgressScore -= levelTargetScore;
            currentLevel++;
            levelTargetScore = currentLevel * 500;
            
            // Level-up reward: +50 Gold
            AddGold(50);
        }
        OnLevelProgressChanged?.Invoke(currentLevel, (float)levelProgressScore / levelTargetScore);
    }

    /// <summary>
    /// Adds gold and triggers event.
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.SetGold(gold);
        }
        OnGoldChanged?.Invoke(gold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.SetGold(gold);
        }
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    /// <summary>
    /// Restart the current game scene.
    /// </summary>
    public void RestartGame()
    {
        StartGame();
    }

    /// <summary>
    /// True when the player may open the replay prompt (idle playing state, no active drag/merge).
    /// </summary>
    public bool CanRequestReplay()
    {
        if (currentState != GameState.Playing) return false;
        if (DraggablePlate.IsAnyDragging()) return false;
        if (_mergeManager != null && _mergeManager.IsProcessing) return false;

        return true;
    }

    /// <summary>
    /// Starts the game and resets level metrics.
    /// </summary>
    public void StartGame()
    {
        DraggablePlate.CancelAllActiveDrags();

        if (_mergeManager != null) _mergeManager.CancelProcessing();

        if (GhostPlatePreview.Instance != null) GhostPlatePreview.Instance.Hide();

        score = 0;
        levelProgressScore = 0;
        currentLevel = 1;
        levelTargetScore = 500;

        OnScoreChanged?.Invoke(score);
        OnLevelProgressChanged?.Invoke(currentLevel, 0f);

        GridManager grid = FindFirstObjectByType<GridManager>();
        if (grid != null) grid.ClearAllPlates();

        HoldSlotsManager slots = FindFirstObjectByType<HoldSlotsManager>();
        if (slots != null)
        {
            slots.ClearAllSlots();
            slots.RefillAllSlots();
        }

        ChangeState(GameState.Playing);
    }

    /// <summary>
    /// Return to main menu state.
    /// </summary>
    public void ReturnToMenu()
    {
        ChangeState(GameState.Menu);
    }
}
