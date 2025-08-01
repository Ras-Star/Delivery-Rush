using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject trafficCarPrefab;
    public GameObject trafficCar1Prefab;
    public GameObject coinPrefab;
    private float[] lanes = { -2f, 0f, 2f };
    private GameObject coinInstance;
    private List<GameObject> activeObstacles = new List<GameObject>();
    
    [Header("Difficulty Settings")]
    public float initialSpawnRate = 5f; // Starting spawn interval (5 seconds)
    public float minimumSpawnRate = 1.5f; // Fastest spawn rate (1.5 seconds)
    public float difficultyIncreaseRate = 0.1f; // How much to decrease interval every X seconds
    public float difficultyInterval = 10f; // Increase difficulty every 10 seconds

    [Header("Obstacle Behavior")]
    public bool enableSpeedVariation = true;
    public bool enableSizeVariation = true;
    public float maxSpeedVariation = 0.5f;
    public float maxSizeVariation = 0.2f;
    
    private float currentSpawnRate;
    private float gameStartTime;
    private float lastDifficultyIncrease;
    private int difficultyLevel = 0;
    
    private int coinLaneIndex = -1; // Track coin's current lane

    // Add this to the top of your ObstacleSpawner class
    private PlayerMovement playerMovement;

    // Add this as a class variable
    private int lastSpawnedPrefabIndex = -1;

    void Start()
    {
        // Validate prefab assignments
        ValidatePrefabAssignments();
        
        // Get reference to PlayerMovement to check game state
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        
        // Initialize difficulty settings
        currentSpawnRate = initialSpawnRate;
        gameStartTime = Time.time;
        lastDifficultyIncrease = gameStartTime;
        
        // Ensure random seed for unpredictable lane selection
        Random.InitState((int)System.DateTime.Now.Ticks);
        
        // Instantiate coin
        coinInstance = Instantiate(coinPrefab, new Vector3(0, 10f, 0), Quaternion.identity);
        coinInstance.name = "Coin";
        coinInstance.tag = "Coin";
        var coinMovement = coinInstance.AddComponent<ObstacleMovement>();
        coinMovement.speed = 5f;
        coinInstance.transform.localScale = new Vector3(1f, 1f, 1f);
        RepositionCoin();

        // Don't start spawning immediately - wait for game to start
        StartCoroutine(WaitForGameStart());
    }

    private void ValidatePrefabAssignments()
    {
        Debug.Log("=== Prefab Validation ===");
        Debug.Log($"TrafficCarPrefab: {(trafficCarPrefab != null ? trafficCarPrefab.name : "NOT ASSIGNED")}");
        Debug.Log($"TrafficCar1Prefab: {(trafficCar1Prefab != null ? trafficCar1Prefab.name : "NOT ASSIGNED")}");
        Debug.Log($"CoinPrefab: {(coinPrefab != null ? coinPrefab.name : "NOT ASSIGNED")}");
        
        if (trafficCarPrefab == null)
            Debug.LogError("TrafficCarPrefab is not assigned! Please assign it in the inspector.");
        if (trafficCar1Prefab == null)
            Debug.LogError("TrafficCar1Prefab is not assigned! Please assign it in the inspector.");
        if (coinPrefab == null)
            Debug.LogError("CoinPrefab is not assigned! Please assign it in the inspector.");
    }

    private System.Collections.IEnumerator WaitForGameStart()
    {
        // More efficient waiting
        while (Time.timeScale == 0)
        {
            yield return new WaitForSecondsRealtime(0.1f); // Check every 0.1 seconds instead of every frame
        }
        
        Debug.Log("Game started - beginning obstacle spawning");
        StartCoroutine(SpawnObstacleCoroutine());
    }

    void Update()
    {
        // Update difficulty over time
        UpdateDifficulty();
        
        // Update coin's lane (less frequent checks)
        if (Time.frameCount % 10 == 0 && coinInstance != null) // Check every 10 frames
        {
            coinLaneIndex = GetLaneIndex(coinInstance.transform.position.x);
        }

        // Clean up off-screen obstacles less frequently
        if (Time.frameCount % 30 == 0) // Every 30 frames (~twice per second)
        {
            CleanupOffscreenObstacles();
        }

        // Handle coin respawn
        HandleCoinRespawn();
    }

    private void CleanupOffscreenObstacles()
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] == null || activeObstacles[i].transform.position.y < -12f)
            {
                if (activeObstacles[i] != null)
                    Destroy(activeObstacles[i]);
                activeObstacles.RemoveAt(i);
            }
        }
    }

    private void HandleCoinRespawn()
    {
        if (coinInstance != null && coinInstance.transform.position.y < -10f)
        {
            RepositionCoin();
        }
        else if (coinInstance == null)
        {
            CreateNewCoin();
        }
    }

    void UpdateDifficulty()
    {
        float currentTime = Time.time;
        
        // Check if it's time to increase difficulty
        if (currentTime - lastDifficultyIncrease >= difficultyInterval)
        {
            if (currentSpawnRate > minimumSpawnRate)
            {
                // Progressive difficulty increase (starts slow, gets faster)
                float difficultyMultiplier = 1f + (difficultyLevel * 0.1f);
                float decreaseAmount = difficultyIncreaseRate * difficultyMultiplier;
                currentSpawnRate = Mathf.Max(minimumSpawnRate, currentSpawnRate - decreaseAmount);
                difficultyLevel++;
                lastDifficultyIncrease = currentTime;
                
                float gameTime = currentTime - gameStartTime;
                Debug.Log($"Difficulty increased! Level: {difficultyLevel}, Spawn Rate: {currentSpawnRate:F1}s, Game Time: {gameTime:F0}s");
            }
        }
    }
    
    private System.Collections.IEnumerator SpawnObstacleCoroutine()
    {
        while (true)
        {
            if (Time.timeScale > 0)
            {
                SpawnObstacle();
            }
            
            // Use WaitForSecondsRealtime to work even when paused
            yield return new WaitForSecondsRealtime(currentSpawnRate);
        }
    }

    int GetLaneIndex(float x)
    {
        for (int i = 0; i < lanes.Length; i++)
            if (Mathf.Abs(x - lanes[i]) < 0.1f)
                return i;
        return -1;
    }

    void SpawnObstacle()
    {
        // Clean up null references first
        activeObstacles.RemoveAll(obj => obj == null);
        
        // Determine available lanes (exclude coin's lane)
        List<int> availableLaneIndices = new List<int>();
        for (int i = 0; i < lanes.Length; i++)
        {
            if (i != coinLaneIndex)
                availableLaneIndices.Add(i);
        }

        if (availableLaneIndices.Count == 0)
        {
            Debug.LogWarning("No available lanes for obstacle spawning!");
            return;
        }

        // Check for obstacles too close to spawn point (improved spacing)
        List<int> occupiedByObstacles = new List<int>();
        foreach (var activeObstacle in activeObstacles)
        {
            if (activeObstacle != null && activeObstacle.transform.position.y > 3f) // Only check obstacles near spawn area
            {
                int occupiedLaneIndex = GetLaneIndex(activeObstacle.transform.position.x);
                if (occupiedLaneIndex >= 0 && !occupiedByObstacles.Contains(occupiedLaneIndex))
                    occupiedByObstacles.Add(occupiedLaneIndex);
            }
        }

        // Remove occupied lanes
        availableLaneIndices.RemoveAll(lane => occupiedByObstacles.Contains(lane));

        if (availableLaneIndices.Count == 0)
        {
            Debug.Log("All available lanes are occupied by obstacles!");
            return;
        }

        // Pick a random available lane
        int selectedLaneIndex = availableLaneIndices[Random.Range(0, availableLaneIndices.Count)];
        float spawnLane = lanes[selectedLaneIndex];

        // DEBUG: Check prefab assignments
        Debug.Log($"TrafficCarPrefab assigned: {trafficCarPrefab != null}");
        Debug.Log($"TrafficCar1Prefab assigned: {trafficCar1Prefab != null}");

        // Improved prefab selection with validation
        GameObject prefabToSpawn = null;
        
        // Check if both prefabs are assigned
        if (trafficCarPrefab != null && trafficCar1Prefab != null)
        {
            // Alternating selection to ensure variety
            int prefabIndex = Random.Range(0, 2);
            
            // Avoid spawning the same prefab twice in a row (optional)
            if (prefabIndex == lastSpawnedPrefabIndex)
            {
                prefabIndex = 1 - prefabIndex; // Switch to the other prefab
            }
            
            prefabToSpawn = (prefabIndex == 0) ? trafficCarPrefab : trafficCar1Prefab;
            lastSpawnedPrefabIndex = prefabIndex;
            
            Debug.Log($"Selected prefab index: {prefabIndex}, spawning: {prefabToSpawn.name}");
        }
        else if (trafficCarPrefab != null)
        {
            // Only trafficCarPrefab is assigned
            prefabToSpawn = trafficCarPrefab;
            Debug.LogWarning("Only TrafficCarPrefab is assigned! Please assign TrafficCar1Prefab in the inspector.");
        }
        else if (trafficCar1Prefab != null)
        {
            // Only trafficCar1Prefab is assigned
            prefabToSpawn = trafficCar1Prefab;
            Debug.LogWarning("Only TrafficCar1Prefab is assigned! Please assign TrafficCarPrefab in the inspector.");
        }
        else
        {
            // Neither prefab is assigned
            Debug.LogError("Neither TrafficCarPrefab nor TrafficCar1Prefab is assigned! Please assign both prefabs in the inspector.");
            return;
        }
        
        // Spawn with slight position variation for more natural feel
        float spawnY = 6f + Random.Range(-0.2f, 0.2f);
        CreateObstacle(prefabToSpawn, new Vector3(spawnLane, spawnY, 0));
        
        Debug.Log($"Spawned: {prefabToSpawn.name} in lane: {spawnLane}");
    }

    void CreateObstacle(GameObject prefab, Vector3 position)
    {
        GameObject newObstacle = Instantiate(prefab, position, Quaternion.identity);
        newObstacle.name = prefab.name;
        newObstacle.tag = "Obstacle";
        
        // Add movement with intelligent speed
        var movement = newObstacle.AddComponent<ObstacleMovement>();
        float baseSpeed = 2.5f + (difficultyLevel * 0.15f);
        
        if (enableSpeedVariation)
        {
            float variation = Random.Range(-maxSpeedVariation, maxSpeedVariation);
            movement.speed = Mathf.Clamp(baseSpeed + variation, 1.5f, 7f);
        }
        else
        {
            movement.speed = baseSpeed;
        }
        
        // Visual variety
        if (enableSizeVariation)
        {
            float sizeVariation = Random.Range(1f - maxSizeVariation, 1f + maxSizeVariation);
            newObstacle.transform.localScale = new Vector3(3f * sizeVariation, 3f * sizeVariation, 1f);
        }
        else
        {
            newObstacle.transform.localScale = new Vector3(3f, 3f, 1f);
        }
        
        activeObstacles.Add(newObstacle);
    }

    public void RepositionCoin()
    {
        if (coinInstance == null)
        {
            Debug.LogError("Coin instance is null!");
            CreateNewCoin();
            return;
        }
        
        int laneIndex = Random.Range(0, lanes.Length);
        coinInstance.transform.position = new Vector3(lanes[laneIndex], 10f, 0);
        coinInstance.SetActive(true); // Ensure coin is active
        coinLaneIndex = laneIndex;
        Debug.Log("Repositioned Coin to lane: " + lanes[laneIndex]);
    }

    private void CreateNewCoin()
    {
        // Create a new coin if the original is lost
        coinInstance = Instantiate(coinPrefab, new Vector3(0, 10f, 0), Quaternion.identity);
        coinInstance.name = "Coin";
        coinInstance.tag = "Coin";
        var coinMovement = coinInstance.AddComponent<ObstacleMovement>();
        coinMovement.speed = 5f;
        coinInstance.transform.localScale = new Vector3(1f, 1f, 1f);
        RepositionCoin();
        Debug.Log("Created new coin instance");
    }

    public void OnCoinCollected()
    {
        if (coinInstance != null)
        {
            // Immediately reposition the coin instead of disabling it
            RepositionCoin();
            Debug.Log("Coin repositioned after collection");
        }
        else
        {
            Debug.LogError("Coin instance is null when trying to reposition!");
            // Recreate coin if it's missing
            CreateNewCoin();
        }
    }
    
    // Public getter for current difficulty info (useful for UI display)
    public int GetDifficultyLevel() => difficultyLevel;
    public float GetCurrentSpawnRate() => currentSpawnRate;
    public float GetGameTime() => Time.time - gameStartTime;

    // Add this new method to clear all obstacles
    public void ClearAllObstacles()
    {
        // Destroy all active obstacles
        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        
        // Clear the list
        activeObstacles.Clear();
        
        // Also destroy any remaining obstacles with the "Obstacle" tag
        GameObject[] remainingObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obstacle in remainingObstacles)
        {
            Destroy(obstacle);
        }
        
        Debug.Log("Cleared all obstacles from the scene");
    }

    // Add this method to reset the spawner when game restarts
    public void ResetSpawner()
    {
        // Clear all obstacles
        ClearAllObstacles();
        
        // Reset difficulty settings
        currentSpawnRate = initialSpawnRate;
        gameStartTime = Time.time;
        lastDifficultyIncrease = gameStartTime;
        difficultyLevel = 0;
        
        // Reposition the coin to a safe location
        if (coinInstance != null)
        {
            RepositionCoin();
        }
        else
        {
            CreateNewCoin();
        }
        
        Debug.Log("ObstacleSpawner reset for new game");
    }

    // Add this method for mobile debugging
    public void LogActiveObstacles()
    {
        Debug.Log($"Active obstacles count: {activeObstacles.Count}");
        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>();
                Debug.Log($"Obstacle {obstacle.name} at {obstacle.transform.position} - Renderers: {renderers.Length} - Active: {obstacle.activeInHierarchy}");
            }
        }
    }

    // Add this method to remove an obstacle from the active list
    public void RemoveObstacle(GameObject obstacle)
    {
        activeObstacles.Remove(obstacle);
        Debug.Log($"Removed obstacle: {obstacle.name} from active list");
    }
}

public class ObstacleMovement : MonoBehaviour
{
    public float speed = 3.5f;
    private ObstacleSpawner spawner;

    void Start()
    {
        // Get reference to spawner for cleanup
        spawner = FindFirstObjectByType<ObstacleSpawner>();
    }

    void Update()
    {
        // Move obstacle downward
        transform.position += Vector3.down * speed * Time.deltaTime;
        
        // Auto-destroy when off-screen (better performance)
        if (transform.position.y < -12f)
        {
            // Remove from active obstacles list
            if (spawner != null)
            {
                spawner.RemoveObstacle(gameObject);
            }
            Destroy(gameObject);
        }
    }
}
