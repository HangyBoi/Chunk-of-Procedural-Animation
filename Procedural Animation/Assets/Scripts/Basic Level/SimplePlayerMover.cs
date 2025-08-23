using UnityEngine;

public class SimplePlayerMover : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float turnSpeed = 100f;

    // <-- NEW: Make this public so other scripts can read it
    public Vector3 DesiredVelocity { get; private set; }

    void Update()
    {
        // Calculate the desired forward/backward movement
        Vector3 forwardMovement = transform.forward * Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        // Calculate the desired turning
        float yaw = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;
        transform.Rotate(0, yaw, 0); // We still apply rotation directly here for simplicity

        // Set the public velocity vector
        DesiredVelocity = forwardMovement;

        Debug.Log("PlayerMover: DesiredVelocity is " + DesiredVelocity);
    }
}