using UnityEngine;
using System.Collections;

public class SimpleLeg : MonoBehaviour
{
    // --- Public Variables (assign in Inspector) ---

    [Header("IK Leg Control")]
    [Tooltip("The 'Shoulder Joint' or ideal resting position for the foot. Parented to the spider's body.")]
    public Transform shoulderJoint;

    [Tooltip("The layer mask that represents the ground.")]
    public LayerMask groundLayer;

    [Header("Stepping Parameters")]
    [Tooltip("The distance from the shoulder joint the foot can be before it needs to take a step.")]
    public float stepThreshold = 0.5f;

    [Tooltip("The speed at which the foot moves to its new position.")]
    public float stepSpeed = 5f;

    [Header("Animation Curves")]
    [Tooltip("The height profile of the step. Y-axis is height, X-axis is time (0 to 1).")]
    public AnimationCurve stepHeightCurve;
    [Tooltip("The maximum height the foot will reach during a step.")]
    public float stepMaxHeight = 0.3f;

    // --- Private Variables ---
    private Vector3 currentTargetPosition;
    private Vector3 lastGroundedPosition;
    private bool isStepping = false;

    // This is the IK Target's transform, which this script is attached to.
    private Transform ikTargetTransform;

    // A public property to let other scripts know if this leg is moving.
    public bool IsStepping => isStepping;
    public bool IsGrounded => !isStepping;

    void Awake()
    {
        // Cache the transform this script is attached to
        ikTargetTransform = transform;

        // Initialize positions
        // We start by assuming the leg is perfectly placed on the ground at its initial position.
        lastGroundedPosition = ikTargetTransform.position;
        currentTargetPosition = shoulderJoint.position;
    }

    void Start()
    {
        // Raycast down to find the initial ground position
        RaycastHit hit;
        if (Physics.Raycast(shoulderJoint.position, Vector3.down, out hit, 2f, groundLayer))
        {
            // Set the IK target to this position immediately
            ikTargetTransform.position = hit.point;
            // Update our tracking variables
            lastGroundedPosition = hit.point;
            currentTargetPosition = hit.point;
        }
    }

    // This function will be called by our Gait Controller.
    public void TryStep()
    {
        // Don't try to step if we are already stepping.
        if (isStepping) return;

        RaycastHit hit;
        if (Physics.Raycast(shoulderJoint.position, Vector3.down, out hit, 2f, groundLayer))
        {
            currentTargetPosition = hit.point;
        }

        float distance = Vector3.Distance(lastGroundedPosition, currentTargetPosition);

        if (distance > stepThreshold)
        {
            StartCoroutine(MoveToTarget());
        }
    }

    /// <summary>
    /// A coroutine that smoothly moves the IK target (the foot) from its old position
    /// to the new target position over several frames.
    /// </summary>
    private IEnumerator MoveToTarget()
    {
        // Mark that we are now stepping.
        isStepping = true;

        Vector3 startPosition = ikTargetTransform.position;
        float journeyTime = 0f;

        // Calculate the duration of the step based on speed and distance.
        float stepDuration = Vector3.Distance(startPosition, currentTargetPosition) / stepSpeed;

        // The journey continues until our timer reaches the calculated duration.
        while (journeyTime < stepDuration)
        {
            // Increment the timer by the time elapsed since the last frame.
            journeyTime += Time.deltaTime;

            // Calculate our progress percentage (0 to 1).
            float percent = Mathf.Clamp01(journeyTime / stepDuration);

            // Apply a smoothing function (e.g., sine curve) to the percentage for the height calculation.
            // This makes the leg lift and lower smoothly.
            float heightPercent = Mathf.Sin(percent * Mathf.PI);

            // Use Lerp to find the current position between the start and end points.
            ikTargetTransform.position = Vector3.Lerp(startPosition, currentTargetPosition, percent);

            // Add the step height, multiplied by our smoothed height percentage.
            ikTargetTransform.position += Vector3.up * stepMaxHeight * heightPercent;


            // Yield execution until the next frame.
            yield return null;
        }

        // Ensure the foot is exactly at the target position when the loop finishes.
        ikTargetTransform.position = currentTargetPosition;
        lastGroundedPosition = currentTargetPosition;

        // Mark that we have finished stepping.
        isStepping = false;
    }
}