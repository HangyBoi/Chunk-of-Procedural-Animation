using UnityEngine;

// This script handles the player's input to move and rotate the spider's main body.
// It acts as the "driver" and knows nothing about the legs or the ground.
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float rotateSpeed = 100.0f;

    void Update()
    {
        // Forward/Backward movement based on Vertical axis (W/S keys or controller stick)
        float verticalInput = Input.GetAxis("Vertical");
        transform.Translate(transform.forward * verticalInput * moveSpeed * Time.deltaTime, Space.World);

        // Left/Right rotation based on Horizontal axis (A/D keys or controller stick)
        float horizontalInput = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up, horizontalInput * rotateSpeed * Time.deltaTime);

        // Use Q and E for strafing or turning
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }
    }
}