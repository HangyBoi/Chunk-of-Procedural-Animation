using UnityEngine;

public class PlayerVelocityInput : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float turnSpeed = 100f;

    // We don't even need this variable for this simple version
    // public Vector3 DesiredVelocity { get; private set; } 

    void Update()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        // 1. Apply rotational input directly
        transform.Rotate(0, horizontalInput * turnSpeed * Time.deltaTime, 0);

        // 2. Apply linear movement directly
        transform.position += transform.forward * verticalInput * moveSpeed * Time.deltaTime;

    }
}