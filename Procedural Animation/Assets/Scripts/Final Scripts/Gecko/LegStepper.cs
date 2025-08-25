using System.Collections;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class LegStepper : MonoBehaviour
{
    // The position and rotation we want to stay in range of
    [SerializeField] Transform homeTransform;
    // If we exceed this distance from home, next move try will succeed
    [SerializeField] float wantStepAtDistance;
    // How long a step takes to complete
    [SerializeField] float moveDuration;
    // How far past the home position the foot will move as a fraction of wantStepAtDistance
    [SerializeField] float stepOvershootFraction;
    // How far above the ground should we be when at rest
    [SerializeField] float heightOffset;
    // If we exceed this angle from home, next move try will succeed
    [SerializeField] float wantStepAtAngle = 135f;
    // What layers are considered ground
    [SerializeField] LayerMask groundRaycastMask = ~0;

    public bool Moving { get; private set; }

    void Awake()
    {
        // Exit hierarchy to avoid influence from root
        transform.SetParent(null);

        // Move to a valid position right away
        // We call this in a coroutine to give the environment time to set up
        StartCoroutine(SnapToGroundOnStart());
    }

    // Helper for startup
    IEnumerator SnapToGroundOnStart()
    {
        yield return null; // Wait one frame
        TryMove();
    }

    public void TryMove()
    {
        if (Moving) return;

        float distFromHome = Vector3.Distance(transform.position, homeTransform.position);
        float angleFromHome = Quaternion.Angle(transform.rotation, homeTransform.rotation);

        // If we are too far off in position or rotation
        if (distFromHome > wantStepAtDistance ||
                angleFromHome > wantStepAtAngle)
        {
            // If we can't find a good target position, don't step
            if (GetGroundedEndPosition(out Vector3 endPos, out Vector3 endNormal))
            {
                // Get rotation facing in the home forward direction but aligned with the normal plane
                Quaternion endRot = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(homeTransform.forward, endNormal),
                    endNormal
                );

                // Start a MoveToPointCoroutine and store it
                StartCoroutine(
                    Move(
                        endPos,
                        endRot,
                        moveDuration
                    )
                );
            }
        }
    }

    // Find a grounded point using home position and overshoot fraction
    // Returns true if a point was found
    bool GetGroundedEndPosition(out Vector3 position, out Vector3 normal)
    {
        Vector3 towardHome = (homeTransform.position - transform.position).normalized;

        // Limit overshoot to a fraction of the step distance.
        float overshootDistance = wantStepAtDistance * stepOvershootFraction;
        Vector3 overshootVector = towardHome * overshootDistance;

        // Raycast from above the point to avoid starting inside the ground
        Vector3 raycastOrigin = homeTransform.position + overshootVector + homeTransform.up * 2f;

        if (Physics.Raycast(
            raycastOrigin,
            -homeTransform.up,
            out RaycastHit hit,
            5f, // Added a max distance for safety
            groundRaycastMask
        ))
        {
            position = hit.point;
            normal = hit.normal;
            return true;
        }

        // If we didn't hit anything, default to the home position
        position = homeTransform.position;
        normal = homeTransform.up;
        return false;
    }

    IEnumerator Move(Vector3 endPoint, Quaternion endRot, float moveTime)
    {
        Moving = true;

        Vector3 startPoint = transform.position;
        Quaternion startRot = transform.rotation;

        // Apply the height offset
        endPoint += homeTransform.up * heightOffset;

        // We want to pass through the center point
        Vector3 centerPoint = (startPoint + endPoint) / 2;
        // But also lift off, so we move it up by half the step distance
        centerPoint += homeTransform.up * Vector3.Distance(startPoint, endPoint) / 2f;

        float timeElapsed = 0;
        do
        {
            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / moveTime;
            normalizedTime = Easing.EaseInOutCubic(normalizedTime);

            // Quadratic bezier curve
            
            transform.SetPositionAndRotation(
                    Vector3.Lerp(
                    Vector3.Lerp(startPoint, centerPoint, normalizedTime),
                    Vector3.Lerp(centerPoint, endPoint, normalizedTime),
                    normalizedTime
            ), Quaternion.Slerp(startRot, endRot, normalizedTime));
            yield return null;
        }
        while (timeElapsed < moveTime);

        Moving = false;
    }

    void OnDrawGizmosSelected()
    {
        if (homeTransform == null) return;

        // Draw the step-trigger radius
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(homeTransform.position, wantStepAtDistance);

        // Draw a visual for the foot itself
        Gizmos.color = Moving ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
        Gizmos.DrawLine(transform.position, homeTransform.position);
        Gizmos.DrawWireCube(homeTransform.position, Vector3.one * 0.1f);
    }
}