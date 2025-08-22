using System.Collections;
using UnityEngine;

// This script manages a single leg of the spider.
public class LegSolver : MonoBehaviour
{
    [Header("IK Leg Components")]
    [SerializeField] private Transform body;                    // The main body of the spider
    [SerializeField] private Transform ikTarget;                // The target for the IK constraint
    [SerializeField] private LayerMask terrainLayer;            // The layer the ground is on

    [Header("Stepping Parameters")]
    [SerializeField] private float stepDistance = 0.8f;         // The distance the leg can be from its ideal spot before it must step
    [SerializeField] private float stepHeight = 0.4f;           // How high the leg lifts during a step
    [SerializeField] private float stepSpeed = 2f;              // How fast the leg moves during a step
    [SerializeField] private AnimationCurve stepHeightCurve;    // Curve to control the arc of the step
    [SerializeField] private AnimationCurve stepSpeedCurve;     // Curve to control the speed of the step over time

    // Public properties that the main SpiderController can access
    public bool IsMoving { get; private set; }
    public bool Movable { get; set; } = true; // The controller can set this to false to prevent this leg from moving

    public Vector3 CurrentPosition => transform.position;
    public Vector3 CurrentNormal { get; private set; }

    private Vector3 idealPosition;
    private float footSpacing;

    private void Start()
    {
        // Store the initial distance from the body center to this leg's target
        footSpacing = Vector3.Distance(transform.position, body.position);

        // Initialize the leg on the ground
        transform.position = FindGround();
        CurrentNormal = Vector3.up;
    }

    void Update()
    {
        // The "ideal" position is where the foot would be if it were directly under its starting point relative to the body
        idealPosition = body.position + (transform.position - body.position).normalized * footSpacing;

        float distance = Vector3.Distance(transform.position, idealPosition);

        // Check if the leg needs to take a step
        if (distance > stepDistance && !IsMoving && Movable)
        {
            StartCoroutine(PerformStep());
        }
    }

    private Vector3 FindGround()
    {
        // Raycast down from the ideal position to find the ground
        Ray ray = new Ray(idealPosition + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f, terrainLayer))
        {
            CurrentNormal = hit.normal; // Store the normal of the ground surface
            return hit.point;
        }
        // If no ground is found, return the current position
        return transform.position;
    }

    private IEnumerator PerformStep()
    {
        IsMoving = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = FindGround();

        float timeElapsed = 0f;

        while (timeElapsed < 1f / stepSpeed)
        {
            timeElapsed += Time.deltaTime;
            float progress = timeElapsed * stepSpeed;

            // Use animation curves for smooth, customizable movement
            float speedProgress = stepSpeedCurve.Evaluate(progress);
            float heightProgress = stepHeightCurve.Evaluate(progress);

            // Interpolate position
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, speedProgress);
            newPos.y += heightProgress * stepHeight; // Add height to create an arc

            ikTarget.position = newPos;

            yield return null;
        }

        // Ensure the target is exactly at the final position
        ikTarget.position = targetPos;
        IsMoving = false;
    }

    // Draw gizmos in the editor to visualize what the script is doing
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(idealPosition, 0.1f); // Ideal position
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.1f); // Current position
    }
}