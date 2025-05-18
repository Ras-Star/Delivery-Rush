using UnityEngine;

public class RoadScroll : MonoBehaviour
{
    public float speed = 3f;        // Scrolling speed
    private float roadHeight = 10f; // Height of the road sprite (adjust if different)

    void Start()
    {
        // Ensure road starts centered in view
        transform.position = new Vector3(0, 0, 0);
    }

    void Update()
    {
        // Move road downward
        transform.position += Vector3.down * speed * Time.deltaTime;

        // If road moves completely off-screen (bottom below -roadHeight)
        if (transform.position.y <= -roadHeight)
        {
            // Reset to top (just above camera view)
            transform.position = new Vector3(0, roadHeight, 0);
        }
    }
}