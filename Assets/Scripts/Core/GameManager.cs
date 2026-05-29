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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;
    
    [Header("Score Settings")]
    [SerializeField] private int score = 0;
    [SerializeField] private int gold = 0;

    // Events for UI or other components to listen to
    public static event Action<GameState> OnStateChanged;
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnGoldChanged;

    public GameState CurrentState => currentState;
    public int Score => score;
    public int Gold => gold;

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
    }

    private void Start()
    {
        // Set initial state to Playing
        ChangeState(GameState.Playing);
        OnScoreChanged?.Invoke(score);
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// Changes the current state of the game and notifies listeners.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {currentState}");
        
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
                Debug.LogWarning("[GameManager] GAME OVER! No more moves possible.");
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
        Debug.Log($"[GameManager] Score: {score} (+{points})");
    }

    /// <summary>
    /// Adds gold and triggers event.
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        Debug.Log($"[GameManager] Gold: {gold} (+{amount})");
    }

    /// <summary>
    /// Restart the current game scene.
    /// </summary>
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
