using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SimpleGaitController : MonoBehaviour
{
    [Header("Leg Groups")]
    public SimpleLeg[] legGroupA;
    public SimpleLeg[] legGroupB;

    [Header("All Legs")]
    public SimpleLeg[] allLegs;

    [Header("Grounding")]
    [Tooltip("The minimum number of legs that must be on the ground for the spider to move.")]
    public int minGroundedLegs = 4;

    [Header("Gait Timing")]
    public float stepCooldown = 0.2f;

    [Header("Body Control")]
    public float bodyAdaptSpeed = 10f;
    public float bodyHeightOffset = 0.8f;

    [Tooltip("The minimum height the body will keep from the ground beneath it.")]
    public float bodyGroundClearance = 0.2f;

    // --- Private Variables ---
    private bool isGroupATurn = true;
    private float lastStepTime;
    private SimplePlayerMover playerMover;

    // Variables to store our calculated positions for debugging
    private Vector3 averageLegPosition;
    private Vector3 desiredBodyPosition;
    private Vector3 finalBodyPosition;

    void Awake()
    {
        playerMover = GetComponent<SimplePlayerMover>();
    }

    void Update()
    {
        // Gait Logic (unchanged)
        if (Time.time < lastStepTime + stepCooldown) return;
        if (isGroupATurn)
        {
            if (IsAnyLegStepping(legGroupB)) return;
            foreach (var leg in legGroupA) { leg.TryStep(); }
            isGroupATurn = false;
            lastStepTime = Time.time;
        }
        else
        {
            if (IsAnyLegStepping(legGroupA)) return;
            foreach (var leg in legGroupB) { leg.TryStep(); }
            isGroupATurn = true;
            lastStepTime = Time.time;
        }
    }

    void LateUpdate()
    {
        // --- Calculate the Center and Up Direction from the Legs ---
        Vector3 averageLegPosition = Vector3.zero;
        Vector3 averageNormal = Vector3.zero;
        int groundedLegCount = 0;

        foreach (var leg in allLegs)
        {
            if (leg.IsGrounded)
            {
                averageLegPosition += leg.transform.position;

                // Raycast down from the foot to get the normal of the ground it's standing on
                RaycastHit hit;
                if (Physics.Raycast(leg.transform.position + Vector3.up, Vector3.down, out hit, 2f, leg.groundLayer))
                {
                    averageNormal += hit.normal;
                }
                groundedLegCount++;
            }
        }

        if (groundedLegCount > 0)
        {
            averageLegPosition /= groundedLegCount;
            averageNormal /= groundedLegCount;
        }

        // --- 1. BODY POSITIONING ---
        // Calculate the ideal body position
        Vector3 desiredBodyPosition = averageLegPosition + averageNormal * bodyHeightOffset;

        // Add player movement ONLY if the spider is stable
        if (groundedLegCount >= minGroundedLegs)
        {
            // Now we use the corrected velocity from our player mover
            desiredBodyPosition += playerMover.DesiredVelocity * Time.deltaTime;
        }

        // Smoothly move the body to the target position
        transform.position = Vector3.Lerp(transform.position, desiredBodyPosition, bodyAdaptSpeed * Time.deltaTime);

        // --- 2. BODY ROTATION ---
        // Calculate the target rotation
        Vector3 bodyForward = transform.forward; // Use current forward direction
        Vector3 bodyRight = Vector3.Cross(averageNormal, bodyForward).normalized;
        bodyForward = Vector3.Cross(bodyRight, averageNormal).normalized; // Recalculate forward to be perpendicular to the new up and right

        // Create the target rotation
        Quaternion targetRotation = Quaternion.LookRotation(bodyForward, averageNormal);

        // Smoothly rotate the body
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bodyAdaptSpeed * Time.deltaTime);
    }

    // --- NEW: Helper function to check the spider's stability ---
    private bool IsSpiderGrounded()
    {
        int groundedLegCount = 0;
        foreach (var leg in allLegs)
        {
            if (leg.IsGrounded)
            {
                groundedLegCount++;
            }
        }
        return groundedLegCount >= minGroundedLegs;
    }

    // --- Helper Functions (unchanged) ---
    private Vector3 GetAveragePosition(SimpleLeg[] legs)
    {
        if (legs.Length == 0) return Vector3.zero;
        Vector3 avg = Vector3.zero;
        foreach (var leg in legs) { avg += leg.transform.position; }
        return avg / legs.Length;
    }

    private bool IsAnyLegStepping(SimpleLeg[] legGroup)
    {
        foreach (var leg in legGroup) { if (leg.IsStepping) return true; }
        return false;
    }

    // --- NEW: Gizmo drawing function ---
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return; // Only draw when the game is running

        // Draw the calculated average leg position (red sphere)
        Gizmos.color = Color.rosyBrown;
        Gizmos.DrawSphere(desiredBodyPosition, 0.1f);
#if UNITY_EDITOR
        Handles.Label(desiredBodyPosition, "Ideal Body Position");
#endif

        // Draw the FINAL body position after the safety check (Magenta Sphere)
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(finalBodyPosition, 0.12f); // Slightly bigger to see it
#if UNITY_EDITOR
        Handles.Label(finalBodyPosition, "Final Body Position");
#endif

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, finalBodyPosition);
    }
}