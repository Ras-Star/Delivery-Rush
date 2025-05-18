using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Use if TextMeshPro; remove if using legacy Text

public class PlayerMovement : MonoBehaviour
{
    private int lane = 1;           // 0=left, 1=middle, 2=right
    private float[] lanes = { -2f, 0f, 2f };
    public float verticalSpeed = 5f; // Kept for potential future use, but not used
    private float verticalPosition = -4f; // Fixed vertical position
    private Vector2 touchStartPos; // Track touch start position
    private const float swipeThreshold = 50f; // Minimum distance for a swipe (in pixels)
    private int score = 0;
    public TextMeshProUGUI scoreText; // Reference to TextMeshPro UI; use Text if legacy
    public float cursorSensitivity = 1.0f; // Sensitivity for cursor movement (adjustable in Inspector)

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
        UpdateScoreText(); // Initialize score display
    }

    void Update()
    {
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
        if (other.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("Game Over! Hit an obstacle.");
            Time.timeScale = 0;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger detected with: " + other.name + " | Tag: " + other.tag);
        if (other.CompareTag("Coin"))
        {
            Debug.Log("Coin Collected! Increasing score by 10...");
            score += 10; // Increase score by 10
            UpdateScoreText(); // Update the UI
            StartCoroutine(CollectCoin(other.gameObject));
        }
    }

    private System.Collections.IEnumerator CollectCoin(GameObject coin)
    {
        FindFirstObjectByType<ObstacleSpawner>().OnCoinCollected();
        Debug.Log("Disabling coin...");
        coin.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("Repositioning coin...");
        FindFirstObjectByType<ObstacleSpawner>().RepositionCoin();
        Debug.Log("Re-enabling coin...");
        coin.SetActive(true);
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
            RectTransform rectTransform = scoreText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log("Score updated to: " + score + ", Text active: " + scoreText.gameObject.activeInHierarchy + ", Anchored Position: " + rectTransform.anchoredPosition);
            }
            else
            {
                Debug.Log("Score updated to: " + score + ", Text active: " + scoreText.gameObject.activeInHierarchy + ", No RectTransform found");
            }
            scoreText.gameObject.SetActive(true); // Ensure text stays active
        }
        else
        {
            Debug.LogWarning("ScoreText reference is not assigned!");
        }
    }

    void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        score = 0; // Reset score on restart
        UpdateScoreText(); // Update display
    }
}