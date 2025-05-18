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
    public float obstacleSpawnRate = 5f; // Spawn obstacles every 3 seconds
    private int coinLaneIndex = -1; // Track coin's current lane

    void Start()
    {
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

        // Start spawning obstacles
        InvokeRepeating("SpawnObstacle", 0f, obstacleSpawnRate);
    }

    void Update()
    {
        // Update coin's lane
        coinLaneIndex = GetLaneIndex(coinInstance.transform.position.x);

        // Remove off-screen obstacles
        activeObstacles.RemoveAll(obj => obj == null || obj.transform.position.y < -10f);

        // If coin goes off-screen, reposition it
        if (coinInstance.transform.position.y < -10f)
            RepositionCoin();
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

        // Check lanes already occupied by obstacles
        List<int> occupiedByObstacles = new List<int>();
        foreach (var activeObstacle in activeObstacles)
        {
            int occupiedLaneIndex = GetLaneIndex(activeObstacle.transform.position.x);
            if (occupiedLaneIndex >= 0 && !occupiedByObstacles.Contains(occupiedLaneIndex))
                occupiedByObstacles.Add(occupiedLaneIndex);
        }

        // Remove lanes occupied by obstacles
        availableLaneIndices.RemoveAll(lane => occupiedByObstacles.Contains(lane));

        if (availableLaneIndices.Count == 0)
        {
            Debug.Log("All available lanes are occupied by obstacles!");
            return;
        }

        // Pick a random available lane
        int selectedLaneIndex = availableLaneIndices[Random.Range(0, availableLaneIndices.Count)];
        float spawnLane = lanes[selectedLaneIndex];

        // Randomly choose between TrafficCar and TrafficCar1
        GameObject prefabToSpawn = Random.value < 0.5f ? trafficCarPrefab : trafficCar1Prefab;
        GameObject newObstacle = Instantiate(prefabToSpawn, new Vector3(spawnLane, 10f, 0), Quaternion.identity);
        newObstacle.name = prefabToSpawn.name; // Remove "(Clone)"
        newObstacle.tag = "Obstacle"; // Add "Obstacle" tag to obstacles
        var movement = newObstacle.AddComponent<ObstacleMovement>();
        movement.speed = 3.5f;
        newObstacle.transform.localScale = new Vector3(3f, 3f, 1f);
        activeObstacles.Add(newObstacle);

        Debug.Log("Spawned " + newObstacle.name + " in lane: " + spawnLane + " | Coin lane: " + (coinLaneIndex >= 0 ? lanes[coinLaneIndex].ToString() : "None"));
    }

    public void RepositionCoin()
    {
        if (coinInstance == null)
        {
            Debug.LogError("Coin instance is null!");
            return;
        }
        int laneIndex = Random.Range(0, lanes.Length);
        coinInstance.transform.position = new Vector3(lanes[laneIndex], 10f, 0);
        coinLaneIndex = laneIndex;
        Debug.Log("Repositioned Coin to lane: " + lanes[laneIndex]);
    }

    public void OnCoinCollected()
    {
        // Temporarily mark coin lane as free until repositioned
        coinLaneIndex = -1;
        Debug.Log("Coin collected, lane freed for obstacle spawning.");
    }
}

public class ObstacleMovement : MonoBehaviour
{
    public float speed = 3.5f;

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;
    }
}