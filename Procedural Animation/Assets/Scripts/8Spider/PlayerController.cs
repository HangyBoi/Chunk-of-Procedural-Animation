using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Vector3 MoveDirection { get; private set; }
    public float RotationDirection { get; private set; }

    [SerializeField] public float moveSpeed = 3.8f;
    [SerializeField] public float rotSpeed = 80.0f;

    void Update()
    {
        // Get movement input
        float ws = Input.GetAxis("Vertical");
        float ad = Input.GetAxis("Horizontal");
        MoveDirection = new Vector3(ad, 0, ws).normalized;

        // Get rotation input
        RotationDirection = 0f;
        if (Input.GetKey(KeyCode.Q)) RotationDirection = -1f;
        if (Input.GetKey(KeyCode.E)) RotationDirection = 1f;
    }
}