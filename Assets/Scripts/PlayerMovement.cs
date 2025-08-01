using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    private int lane = 1;           // 0=left, 1=middle, 2=right
    private float[] lanes = { -2f, 0f, 2f };
    public float verticalSpeed = 5f; // Kept for potential future use, but not used
    private float verticalPosition = -4f; // Fixed vertical position
    private Vector2 touchStartPos; // Track touch start position
    private const float swipeThreshold = 50f; // Minimum distance for a swipe (in pixels)
    private int currentSessionScore = 0; // Score for current game session
    public TextMeshProUGUI scoreText; // Reference to TextMeshPro UI; use Text if legacy
    public float cursorSensitivity = 1.0f; // Sensitivity for cursor movement (adjustable in Inspector)

    [Header("UI References")]
    public GameObject gameOverPanel; // Assign in inspector
    public Button restartButton; // Assign in inspector

    [Header("Start Panel References")]
    public GameObject startPanel; // Assign in inspector
    public Button playButton; // Assign in inspector
    public Button quitButton; // Assign in inspector
    public Button cancelButton; // Button that toggles start panel during gameplay

    [Header("Gameplay UI")]
    public Button pauseButton; // Button that pauses/unpauses gameplay

    private bool isGameOver = false;
    private bool isGamePaused = false;
    private bool gameStarted = false;
    private bool buttonsSetup = false; // Prevent multiple setups
    private bool hasUpdatedPersistentScore = false;

    // Add these constants for PlayerPrefs keys
    private const string PERSISTENT_SCORE_KEY = "DeliveryRush_PersistentScore";
    private const string TOTAL_COINS_KEY = "DeliveryRush_TotalCoins";
    private const string GAMES_PLAYED_KEY = "DeliveryRush_GamesPlayed";

    void Start()
    {
        transform.position = new Vector3(0, -4, 0);
        if (GetComponent<BoxCollider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
            Debug.Log("Added BoxCollider2D to Player.");
        }
        if (GetComponent<Rigidbody2D>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            Debug.Log("Added Rigidbody2D to Player.");
        }
        GameObject canvas = GameObject.Find("ScoreCanvas");
        if (canvas != null)
        {
            Debug.Log("Canvas active: " + canvas.activeInHierarchy + ", ScoreText active: " + scoreText.gameObject.activeInHierarchy);
            canvas.SetActive(true); // Force Canvas active
            scoreText.gameObject.SetActive(true); // Force ScoreText active
        }
        else
        {
            Debug.LogWarning("ScoreCanvas not found!");
        }

        // Ensure your dedicated HUD canvas is always active.
        GameObject hudCanvas = GameObject.Find("HUDCanvas"); // Name your HUD canvas accordingly
        if (hudCanvas != null)
        {
            hudCanvas.SetActive(true);
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("ScoreText reference is not assigned!");
            }
        }
        else
        {
            Debug.LogWarning("HUDCanvas not found!");
        }

        // Initialize session score and persistent score
        currentSessionScore = 0;
        UpdateScoreText();
        Debug.Log($"Game started - Persistent Score: {GetPersistentScore()}, Session Score: {currentSessionScore}");

        // Setup UI only once
        if (!buttonsSetup)
        {
            SetupAllButtons();
            buttonsSetup = true;
        }

        SetupStartPanel();
        UpdateScoreText(); // Initialize score display

        // Log current persistent score for debugging
        Debug.Log($"Game started - Persistent Score: {GetPersistentScore()}, Session Score: {currentSessionScore}");
    }

    void SetupAllButtons()
    {
        // Setup Game Over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => RestartGame());
            SetupButton(restartButton);
        }

        // Setup Start Panel Buttons
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => StartGame());
            SetupButton(playButton);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() => QuitGame());
            SetupButton(quitButton);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => ToggleStartPanel());
            SetupButton(cancelButton);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(() => TogglePause());
            SetupButton(pauseButton);
        }
    }

    void SetupButton(Button button)
    {
        if (button == null) return;

        button.interactable = true;

        // Use ColorTint for hover effects but with subtle colors
        button.transition = Selectable.Transition.ColorTint;

        // Setup subtle hover effects
        ColorBlock colorBlock = button.colors;
        colorBlock.normalColor = Color.white;
        colorBlock.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f); // Subtle gray on hover
        colorBlock.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Darker gray on press
        colorBlock.selectedColor = Color.white;
        colorBlock.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colorBlock.colorMultiplier = 1f;
        colorBlock.fadeDuration = 0.1f;

        button.colors = colorBlock;

        // Ensure proper navigation
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;

        // Ensure target graphic exists
        if (button.targetGraphic == null)
        {
            Image img = button.GetComponent<Image>();
            if (img != null)
            {
                button.targetGraphic = img;
            }
        }

        // Add hover effect component
        AddHoverEffect(button);
    }

    void AddHoverEffect(Button button)
    {
        // Remove existing hover effect if present
        HoverEffect existingEffect = button.GetComponent<HoverEffect>();
        if (existingEffect != null)
        {
            DestroyImmediate(existingEffect);
        }

        // Add new hover effect
        button.gameObject.AddComponent<HoverEffect>();
    }

    void SetupStartPanel()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(true);
            Time.timeScale = 0;
            gameStarted = false;
            isGamePaused = false;
        }

        // Hide gameplay buttons initially
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    void StartGame()
    {
        Debug.Log("Starting game...");

        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Show and enable gameplay buttons
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(true);
            cancelButton.interactable = true;
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
            pauseButton.interactable = true;
        }

        Time.timeScale = 1;
        gameStarted = true;
        isGamePaused = false;
        isGameOver = false;
        
        // Reset player position
        transform.position = new Vector3(lanes[1], verticalPosition, 0); // Reset to middle lane
        lane = 1; // Reset lane to middle
        
        // Ensure score display is updated
        UpdateScoreText();
    }

    void QuitGame()
    {
        Debug.Log("Quitting game...");

        // Only update if we haven't already done so AND there's actually a session score to save
        if (!hasUpdatedPersistentScore && currentSessionScore > 0 && (gameStarted && !isGameOver))
        {
            int newScore = GetPersistentScore() + currentSessionScore;
            SavePersistentScore(newScore);
            hasUpdatedPersistentScore = true;
            Debug.Log($"QuitGame: Updated Persistent Score: {newScore}, Session Score: {currentSessionScore}");
        }
        else
        {
            Debug.Log($"QuitGame: No score update needed. hasUpdated: {hasUpdatedPersistentScore}, sessionScore: {currentSessionScore}, gameStarted: {gameStarted}, isGameOver: {isGameOver}");
        }

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void ToggleStartPanel()
    {
        if (isGameOver) return; // Don't allow panel toggle during game over

        bool isStartPanelActive = startPanel != null && startPanel.activeInHierarchy;

        if (isStartPanelActive)
        {
            // Close start panel and resume game
            if (startPanel != null)
            {
                startPanel.SetActive(false);
            }

            if (gameStarted)
            {
                Time.timeScale = 1;
                isGamePaused = false;
            }
        }
        else
        {
            // Open start panel and pause game
            if (startPanel != null)
            {
                startPanel.SetActive(true);
            }

            Time.timeScale = 0;
            isGamePaused = true;
        }

        Debug.Log($"Start panel {(isStartPanelActive ? "closed" : "opened")}");
    }

    void TogglePause()
    {
        if (!gameStarted || isGameOver) return;

        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0 : 1;

        Debug.Log($"Game {(isGamePaused ? "paused" : "resumed")}");
    }

    void Update()
    {
        // Prevent movement when game hasn't started, is paused, or is over
        if (!gameStarted || isGamePaused || isGameOver) return;

        // Handle cursor input for PC testing
        float mouseX = Input.GetAxis("Mouse X") * cursorSensitivity * 10f; // Amplify mouse movement
        int previousLane = lane;

        // Detect horizontal cursor movement for lane change
        if (mouseX > 0.1f && lane < 2) // Right movement
        {
            lane++;
        }
        else if (mouseX < -0.1f && lane > 0) // Left movement
        {
            lane--;
        }

        // Handle touch input for mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); // Get the first touch
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position; // Record start position
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Ended:
                    Vector2 touchEndPos = touch.position;
                    float swipeDistance = touchEndPos.x - touchStartPos.x;

                    // Detect horizontal swipe for lane change
                    if (swipeDistance > swipeThreshold && lane < 2) // Right swipe
                    {
                        lane++;
                    }
                    else if (swipeDistance < -swipeThreshold && lane > 0) // Left swipe
                    {
                        lane--;
                    }

                    touchStartPos = touchEndPos; // Update start for continuous movement
                    break;
            }
        }

        // Trigger lane change log (no animation)
        if (lane != previousLane)
        {
            Debug.Log("Lane changed to " + lane + " via cursor or touch.");
        }

        transform.position = new Vector3(lanes[lane], verticalPosition, 0); // Fixed vertical position
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Obstacle") && !isGameOver && gameStarted)
        {
            Debug.Log("Game Over! Hit an obstacle.");
            GameOver();
        }
    }

    void GameOver()
    {
        isGameOver = true;
        gameStarted = false;

        // Only update persistent score if we haven't already and there's a session score
        if (!hasUpdatedPersistentScore && currentSessionScore > 0)
        {
            int newScore = GetPersistentScore() + currentSessionScore;
            SavePersistentScore(newScore);
            hasUpdatedPersistentScore = true;
            Debug.Log($"GameOver: Updated Persistent Score: {newScore}, Session Score: {currentSessionScore}");
        }
        else
        {
            Debug.Log($"GameOver: No score update needed. hasUpdated: {hasUpdatedPersistentScore}, sessionScore: {currentSessionScore}");
        }

        // Disable gameplay buttons during game over
        if (cancelButton != null)
        {
            cancelButton.interactable = false;
        }

        if (pauseButton != null)
        {
            pauseButton.interactable = false;
        }

        Time.timeScale = 0; // Pause the game
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        UpdateScoreText();
    }

    void RestartGame()
    {
        Debug.Log("Restarting game...");

        Time.timeScale = 1; // Resume time

        // Reset game state variables
        isGameOver = false;
        isGamePaused = false;
        gameStarted = true;
        currentSessionScore = 0; // New game: reset session score
        hasUpdatedPersistentScore = false; // Allow persistent score update for the new game

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Clear all obstacles when restarting
        ObstacleSpawner spawner = FindFirstObjectByType<ObstacleSpawner>();
        if (spawner != null)
        {
            spawner.ResetSpawner();
        }

        // Act like the Play button
        StartGame();
        UpdateScoreText();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Prevent coin collection when game hasn't started, is paused, or is over
        if (!gameStarted || isGamePaused || isGameOver) return;

        Debug.Log("Trigger detected with: " + other.name + " | Tag: " + other.tag);
        if (other.CompareTag("Coin"))
        {
            Debug.Log("Coin collected! +1 point");
            currentSessionScore += 1;
            UpdateScoreText();
            
            // Notify spawner to reposition the coin
            ObstacleSpawner spawner = FindFirstObjectByType<ObstacleSpawner>();
            if (spawner != null)
            {
                spawner.OnCoinCollected();
                Debug.Log("Coin collection notified to spawner");
            }
        }
    }

    void UpdateScoreText()
    {
        int totalScore = GetPersistentScore() + currentSessionScore;
        if (scoreText != null)
        {
            scoreText.text = "Score: " + totalScore.ToString();
            RectTransform rectTransform = scoreText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log("Score updated to: " + totalScore + ", Text active: " + scoreText.gameObject.activeInHierarchy + ", Anchored Position: " + rectTransform.anchoredPosition);
            }
            else
            {
                Debug.Log("Score updated to: " + totalScore + ", Text active: " + scoreText.gameObject.activeInHierarchy + ", No RectTransform found");
            }
            scoreText.gameObject.SetActive(true); // Ensure text stays active
        }
        else
        {
            Debug.LogWarning("ScoreText reference is not assigned!");
        }
    }

    // PlayerPrefs methods for persistent storage
    private int GetPersistentScore()
    {
        return PlayerPrefs.GetInt(PERSISTENT_SCORE_KEY, 0);
    }

    private void SavePersistentScore(int score)
    {
        PlayerPrefs.SetInt(PERSISTENT_SCORE_KEY, score);
        PlayerPrefs.Save(); // Force save to disk
        Debug.Log($"Persistent score saved: {score}");
    }

    private int GetTotalCoins()
    {
        return PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
    }

    private void IncrementTotalCoins()
    {
        int totalCoins = GetTotalCoins() + 1;
        PlayerPrefs.SetInt(TOTAL_COINS_KEY, totalCoins);
        PlayerPrefs.Save();
    }

    private int GetGamesPlayed()
    {
        return PlayerPrefs.GetInt(GAMES_PLAYED_KEY, 0);
    }

    private void IncrementGamesPlayed()
    {
        int gamesPlayed = GetGamesPlayed() + 1;
        PlayerPrefs.SetInt(GAMES_PLAYED_KEY, gamesPlayed);
        PlayerPrefs.Save();
        Debug.Log($"Games played: {gamesPlayed}");
    }

    // Enhanced reset method for testing
    [ContextMenu("Reset All Persistent Data")]
    public void ResetAllPersistentData()
    {
        PlayerPrefs.DeleteKey(PERSISTENT_SCORE_KEY);
        PlayerPrefs.DeleteKey(TOTAL_COINS_KEY);
        PlayerPrefs.DeleteKey(GAMES_PLAYED_KEY);
        PlayerPrefs.Save();
        currentSessionScore = 0;
        UpdateScoreText();
        Debug.Log("All persistent data reset!");
    }

    [ContextMenu("Show Persistent Stats")]
    public void ShowPersistentStats()
    {
        Debug.Log($"=== PERSISTENT STATS ===");
        Debug.Log($"Total Score: {GetPersistentScore()}");
        Debug.Log($"Total Coins: {GetTotalCoins()}");
        Debug.Log($"Games Played: {GetGamesPlayed()}");
        Debug.Log($"Current Session Score: {currentSessionScore}");
        Debug.Log($"========================");
    }

    // Public getters updated to use PlayerPrefs
    public int GetTotalScore() => GetPersistentScore() + currentSessionScore;
    public int GetSessionScore() => currentSessionScore;
    public int GetStoredPersistentScore() => GetPersistentScore();

    void OnApplicationQuit()
    {
        // Only update if we haven't already done so AND the game is still active (not game over)
        if (!hasUpdatedPersistentScore && currentSessionScore > 0 && gameStarted && !isGameOver)
        {
            int newScore = GetPersistentScore() + currentSessionScore;
            SavePersistentScore(newScore);
            hasUpdatedPersistentScore = true;
            Debug.Log($"OnApplicationQuit: Updated Persistent Score: {newScore}, Session Score: {currentSessionScore}");
        }
        else
        {
            Debug.Log($"OnApplicationQuit: No score update needed. hasUpdated: {hasUpdatedPersistentScore}, sessionScore: {currentSessionScore}, gameStarted: {gameStarted}, isGameOver: {isGameOver}");
        }
    }
}

// Add this new class for hover effects
public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private bool isHovering = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHovering)
        {
            isHovering = true;
            StartCoroutine(ScaleEffect(originalScale * 1.05f)); // Slightly larger on hover
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHovering)
        {
            isHovering = false;
            StartCoroutine(ScaleEffect(originalScale)); // Back to original size
        }
    }

    private IEnumerator ScaleEffect(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for UI
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    void OnDestroy()
    {
        // Reset scale when component is destroyed
        if (transform != null)
        {
            transform.localScale = originalScale;
        }
    }
}