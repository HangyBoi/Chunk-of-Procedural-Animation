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
        playerMover = GetComponentInParent<SimplePlayerMover>();
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
        // --- NEW: Grounding Check ---
        // We will only calculate player movement if the spider is stable.
        Vector3 playerMovement = Vector3.zero;
        if (IsSpiderGrounded())
        {
            // The spider is stable, so we can apply the player's desired velocity.
            playerMovement = playerMover.DesiredVelocity * Time.deltaTime;
        }


        // --- Step 9: Calculate Ideal Body Position ---
        averageLegPosition = GetAveragePosition(allLegs);
        // We now use our new 'playerMovement' variable, which will be zero if the spider is unstable.
        desiredBodyPosition = averageLegPosition + playerMovement;
        desiredBodyPosition.y += bodyHeightOffset;


        // --- Body Collision Safety Check ---
        RaycastHit hit;
        if (Physics.Raycast(desiredBodyPosition + Vector3.up, Vector3.down, out hit, 2f, allLegs[0].groundLayer))
        {
            float minimumHeight = hit.point.y + bodyGroundClearance;
            if (desiredBodyPosition.y < minimumHeight)
            {
                desiredBodyPosition.y = minimumHeight;
            }
        }

        finalBodyPosition = desiredBodyPosition;

        // --- Move and Rotate the Body ---
        transform.position = Vector3.Lerp(transform.position, finalBodyPosition, bodyAdaptSpeed * Time.deltaTime);

        // --- Step 10: Body Rotation ---
        // (Rotation logic is complex, let's focus on position first. You can comment this out temporarily if needed)
        Vector3 leftLegsAverage = GetAveragePosition(legGroupA.Where((leg, i) => i % 2 == 0).Concat(legGroupB.Where((leg, i) => i % 2 != 0)).ToArray());
        Vector3 rightLegsAverage = GetAveragePosition(legGroupA.Where((leg, i) => i % 2 != 0).Concat(legGroupB.Where((leg, i) => i % 2 == 0)).ToArray());

        Vector3 forwardDirection = (rightLegsAverage - leftLegsAverage);
        Vector3 upDirection = Vector3.Cross(forwardDirection, transform.forward).normalized;

        if (upDirection != Vector3.zero) // Avoid issues when upDirection is zero
        {
            Quaternion targetRotation = Quaternion.LookRotation(transform.forward, upDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bodyAdaptSpeed * Time.deltaTime);
        }

        if (playerMover == null)
        {
            Debug.LogError("GaitController: playerMover reference is NULL!");
            return;
        }
        Debug.Log("GaitController: Reading velocity of " + playerMover.DesiredVelocity);
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