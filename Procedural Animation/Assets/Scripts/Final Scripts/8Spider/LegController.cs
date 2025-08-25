using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Leg[] legs;

    [Header("Gait Properties")]
    [Tooltip("The distance a leg can be from its ideal position before forcing a step.")]
    [SerializeField] private float forceStepDistance = 0.7f;
    [Tooltip("How much to shorten the step distance during turns. 0 = no change, 1 = steps become instant.")]
    [SerializeField][Range(0, 1)] private float turnStepShortenFactor = 0.6f;

    [Header("Body Positioning")]
    [SerializeField] private float bodyHeight = 1.3f;
    [SerializeField][Range(0, 1)] private float bodyPositionSmoothTime = 0.1f;
    [SerializeField][Range(0, 1)] private float bodyRotationSmoothTime = 0.2f;

    [Header("Body Lean")]
    [Tooltip("How much the body rolls side-to-side when strafing.")]
    [SerializeField] private float leanFactor = 4.0f;
    [Tooltip("How much the body rolls when turning.")]
    [SerializeField] private float turnLeanFactor = 10.0f;

    private Vector3 bodyPos;
    private Vector3 bodyUp;
    private Vector3 bodyForward;
    private Vector3 bodyRight;
    private Quaternion bodyRotation;

    private int legUpdateIndex = 0;

    // Leg groups for tripod gait (indices depend on leg setup in the array)
    // NOTE: One may need to adjust these indices to match your leg order!
    // For an 8-legged spider:
    private readonly int[] groupA = { 0, 2, 5, 7 };
    private readonly int[] groupB = { 1, 3, 4, 6 };
    private bool useGroupA = true; // Which group's turn is it

    private void Start()
    {
        StartCoroutine(MoveSpider());

        // Start coroutine to adjust body transform
        StartCoroutine(AdjustBodyTransform());
    }

    private void Update()
    {
        if (legs.Length < 2) return;

        // Update leg raycasts, staggered over frames for performance
        legs[legUpdateIndex].UpdateRaycast();
        legUpdateIndex = (legUpdateIndex + 1) % legs.Length;
        int oppositeIndex = (legUpdateIndex + legs.Length / 2) % legs.Length;
        legs[oppositeIndex].UpdateRaycast();

        // --- DYNAMIC STEP DISTANCE ---
        // Check if the player is trying to rotate.
        // Mathf.Abs gets the absolute value, so it works for turning left (-1) or right (1).
        float rotationInput = Mathf.Abs(playerController.RotationDirection);

        // Calculate the effective step distance for this frame.
        // When turning (rotationInput is 1), the distance is shortened. When not turning (input is 0), it's the full distance.
        float currentStepDistance = forceStepDistance * (1.0f - (rotationInput * turnStepShortenFactor));

        // --- GAIT LOGIC ---

        // Determine which group is active and which is waiting
        int[] activeGroup = useGroupA ? groupA : groupB;
        int[] waitingGroup = useGroupA ? groupB : groupA;

        // Check if the waiting group has finished its move. If not, we wait.
        if (IsAnyLegMovingInGroup(waitingGroup))
        {
            return;
        }

        // Check if any leg in the active group is stretched too far and needs to step.
        bool activeGroupNeedsToStep = false;
        foreach (int index in activeGroup)
        {
            if (legs[index].TipDistance > currentStepDistance)
            {
                activeGroupNeedsToStep = true;
                break;
            }
        }

        // If the active group needs to step, allow all its legs to become movable.
        if (activeGroupNeedsToStep)
        {
            foreach (int index in activeGroup)
            {
                legs[index].Movable = true;
            }
            // It's now the other group's turn to be active
            useGroupA = !useGroupA;
        }
        else // If no step is needed, ensure all legs in the active group are not movable.
        {
            foreach (int index in activeGroup)
            {
                legs[index].Movable = false;
            }
        }
    }

    private IEnumerator MoveSpider()
    {
        while (true)
        {
            // Apply movement based on player controller's intent
            Vector3 worldMoveDirection = bodyTransform.TransformDirection(playerController.MoveDirection);
            bodyTransform.position += worldMoveDirection * playerController.moveSpeed * Time.deltaTime;

            // Apply rotation
            bodyTransform.Rotate(0, playerController.RotationDirection * playerController.rotSpeed * Time.deltaTime, 0);

            yield return null; // Run every frame
        }
    }

    // Helper function to check if any leg in a given group is currently animating.
    private bool IsAnyLegMovingInGroup(int[] group)
    {
        foreach (int index in group)
        {
            if (legs[index].Animating)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator AdjustBodyTransform()
    {
        while (true)
        {
            Vector3 tipCenter = Vector3.zero;
            bodyUp = Vector3.zero;

            // Collect leg information to calculate body transform
            foreach (Leg leg in legs)
            {
                tipCenter += leg.TipPos;
                bodyUp += leg.TipUpDir + leg.RaycastTipNormal;
            }

            RaycastHit hit;
            if (Physics.Raycast(bodyTransform.position, bodyTransform.up * -1, out hit, 10.0f))
            {
                bodyUp += hit.normal;
            }

            tipCenter /= legs.Length;
            bodyUp.Normalize();

            // Interpolate position from old to new (using our new inspector variable)
            bodyPos = tipCenter + bodyUp * bodyHeight;
            bodyTransform.position = Vector3.Lerp(bodyTransform.position, bodyPos, bodyPositionSmoothTime);

            // --- NEW ROTATION AND LEAN LOGIC ---

            // Calculate the base rotation to align with the ground
            bodyRight = Vector3.Cross(bodyUp, bodyTransform.forward);
            bodyForward = Vector3.Cross(bodyRight, bodyUp);
            Quaternion groundAlignmentRotation = Quaternion.LookRotation(bodyForward, bodyUp);

            // Calculate lean based on movement
            // Convert world movement direction to local space to know if we are strafing or moving forward
            Vector3 localMoveDirection = bodyTransform.InverseTransformDirection(playerController.MoveDirection);
            float rollAngle = -localMoveDirection.x * leanFactor; // Roll side-to-side when strafing

            // Calculate lean based on turning
            float turnRollAngle = -playerController.RotationDirection * turnLeanFactor;

            // Combine lean angles into a rotation
            Quaternion leanRotation = Quaternion.Euler(0, 0, rollAngle + turnRollAngle);

            // Combine ground alignment with the lean
            bodyRotation = groundAlignmentRotation * leanRotation;

            // Interpolate rotation from old to new
            bodyTransform.rotation = Quaternion.Slerp(bodyTransform.rotation, bodyRotation, bodyRotationSmoothTime);

            yield return new WaitForFixedUpdate();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(bodyPos, bodyPos + bodyRight);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(bodyPos, bodyPos + bodyUp);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(bodyPos, bodyPos + bodyForward);
    }
}
