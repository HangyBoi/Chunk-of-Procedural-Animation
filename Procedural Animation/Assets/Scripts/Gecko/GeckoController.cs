using System.Collections;
using UnityEngine;

public class GeckoController : MonoBehaviour
{
    [Header("Tracking Settings")]
    [Tooltip("The target the gecko will look at")]
    [SerializeField] Transform target;

    [Header("--- System Toggles ---")]
    [SerializeField] bool rootMotionEnabled = true;
    [SerializeField] bool headTrackingEnabled = true;
    [SerializeField] bool eyeTrackingEnabled = true;
    [SerializeField] bool tailSwayEnabled = true;
    [SerializeField] bool legSteppingEnabled = true;
    [SerializeField] bool bodyAdjustmentEnabled = true;

    [Header("--- Control Mode ---")]
    [Tooltip("Enable this to control the gecko with WASDQE keys instead of following the target.")]
    [SerializeField] bool useManualControl = false;

    void Awake()
    {
        StartCoroutine(LegUpdateCoroutine());
        TailInitialize();
        RootBoneInitialize();
    }

    void Update()
    {
        if (useManualControl)
        {
            ManualMovementUpdate();
        }
        else
        {
            RootMotionUpdate();
        }
    }

    void LateUpdate()
    {
        // Update order is important! 
        // We update things in order of dependency, so we update the body first via IdleBobbingUpdate,
        // since the head is moved by the body, then we update the head, since the eyes are moved by the head,
        // and finally the eyes.

        BodyAdjustmentUpdate();
        HeadTrackingUpdate();
        EyeTrackingUpdate();
        TailUpdate();
    }

    #region Root Motion

    [Header("Body Movement Settings")]
    [Tooltip("How fast the gecko can turn (degrees per second)")]
    [SerializeField, Range(1f, 360f)] float turnSpeed = 150.0f;
    [Tooltip("How fast the gecko can move (units per second)")]
    [SerializeField, Range(0.1f, 10f)] float moveSpeed = 3.0f;
    [Tooltip("How fast the gecko accelerates when turning (degrees per second squared)")]
    [SerializeField, Range(0.1f, 10f)] float turnAcceleration = 4.0f;
    [Tooltip("How fast the gecko accelerates when moving (units per second squared)")]
    [SerializeField, Range(0.1f, 10f)] float moveAcceleration = 1.0f;

    // Try to stay in this min/max from the target
    [SerializeField] float minDistToTarget;
    [SerializeField] float maxDistToTarget;
    // If we are above this angle from the target, start turning
    [SerializeField] float maxAngToTarget;

    // World space velocity
    private Vector3 currentVelocity;
    // We are only doing a rotation around the up axis, so we only use a float here
    private float currentAngularVelocity;

    /// <summary>
    /// Updates the gecko's position and rotation to follow the target using smooth acceleration.
    /// </summary>
    void RootMotionUpdate()
    {
        if (!rootMotionEnabled) return;

        // Get the direction toward our target
        Vector3 towardTarget = target.position - transform.position;
        // Vector toward target on the local XZ plane
        Vector3 towardTargetProjected = Vector3.ProjectOnPlane(towardTarget, transform.up);
        // Get the angle from the gecko's forward direction to the direction toward our target
        // Here we get the signed angle around the up vector so we know which direction to turn in
        float angToTarget = Vector3.SignedAngle(transform.forward, towardTargetProjected, transform.up);

        float targetAngularVelocity = 0;

        // If we are within the max angle (i.e. approximately facing the target)
        // leave the target angular velocity at zero
        if (Mathf.Abs(angToTarget) > maxAngToTarget)
        {
            // Angles in Unity are clockwise, so a positive angle here means to our right
            if (angToTarget > 0)
            {
                targetAngularVelocity = turnSpeed;
            }
            // Invert angular speed if target is to our left
            else
            {
                targetAngularVelocity = -turnSpeed;
            }
        }

        // Use our smoothing function to gradually change the velocity
        currentAngularVelocity = Mathf.Lerp(
          currentAngularVelocity,
          targetAngularVelocity,
          1 - Mathf.Exp(-turnAcceleration * Time.deltaTime)
        );

        // Rotate the transform around the Y axis in world space, 
        // making sure to multiply by delta time to get a consistent angular velocity
        transform.Rotate(0, Time.deltaTime * currentAngularVelocity, 0, Space.World);

        Vector3 targetVelocity = Vector3.zero;

        // Don't move if we are facing away from the target, just rotate in place
        if (Mathf.Abs(angToTarget) < 90)
        {
            float distToTarget = Vector3.Distance(transform.position, target.position);

            // If we are too far away from the target, move closer
            if (distToTarget > maxDistToTarget)
            {
                targetVelocity = moveSpeed * towardTargetProjected.normalized;
            }
            // If we are too close to the target, reverse the direction and move away
            else if (distToTarget < minDistToTarget)
            {
                targetVelocity = -1 * moveSpeed * towardTargetProjected.normalized;
            }
        }

        currentVelocity = Vector3.Lerp(
          currentVelocity,
          targetVelocity,
          1 - Mathf.Exp(-moveAcceleration * Time.deltaTime)
        );

        // Apply the velocity
        transform.position += currentVelocity * Time.deltaTime;
    }

    /// <summary>
    /// Updates the gecko's position and rotation based on keyboard input (WASDQE).
    /// </summary>
    void ManualMovementUpdate()
    {
        if (!rootMotionEnabled) return;

        // --- Get Player Input ---
        float verticalInput = Input.GetAxis("Vertical"); // W/S or Up/Down arrows
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows

        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.E))
        {
            rotationInput = 1f;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            rotationInput = -1f;
        }

        // --- Calculate Target Velocity ---
        // Combine forward/backward and strafe movement, relative to the gecko's orientation
        Vector3 moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        Vector3 targetVelocity = moveDirection * moveSpeed;

        // --- Calculate Target Angular Velocity ---
        float targetAngularVelocity = rotationInput * turnSpeed;

        // --- Apply Smoothing (using the same logic as the AI) ---
        currentVelocity = Vector3.Lerp(
          currentVelocity,
          targetVelocity,
          1 - Mathf.Exp(-moveAcceleration * Time.deltaTime)
        );

        currentAngularVelocity = Mathf.Lerp(
          currentAngularVelocity,
          targetAngularVelocity,
          1 - Mathf.Exp(-turnAcceleration * Time.deltaTime)
        );

        // --- Apply the final movement ---
        transform.position += currentVelocity * Time.deltaTime;
        transform.Rotate(0, Time.deltaTime * currentAngularVelocity, 0, Space.World);
    }

    #endregion

    #region Body Adjustment

    [Header("Body Adjustment")]
    [Tooltip("The root bone of the skeleton, usually the hips or pelvis.")]
    [SerializeField] Transform rootBone;
    [Tooltip("The desired height of the root bone from the center of the feet.")]
    [SerializeField] float bodyHeight = 0.5f;
    [Tooltip("How quickly the body adjusts its position to the terrain.")]
    [SerializeField] float bodyPositionSpeed = 5.0f;
    [Tooltip("How quickly the body adjusts its rotation to match the terrain slope.")]
    [SerializeField] float bodyRotationSpeed = 4.0f;

    // Variables for smoothing the body's movement
    private SmoothDamp.Vector3 smoothedBodyPosition;
    private Quaternion smoothedBodyRotation;
    private Vector3 initialRootBonePosition;

    void RootBoneInitialize()
    {
        // Store the initial local position and rotation of the root bone
        if (rootBone != null)
        {
            initialRootBonePosition = rootBone.localPosition;
            smoothedBodyPosition.currentValue = initialRootBonePosition;
            smoothedBodyRotation = rootBone.localRotation;
        }
    }

    /// <summary>
    /// Adjusts the body's height and tilt to conform to the terrain the feet are on.
    /// </summary>
    void BodyAdjustmentUpdate()
    {
        if (!bodyAdjustmentEnabled || rootBone == null) return;

        // --- Calculate Average Feet Position and Ground Normal ---
        Vector3 feetCenterPoint = (
            frontLeftLegStepper.transform.position +
            frontRightLegStepper.transform.position +
            backLeftLegStepper.transform.position +
            backRightLegStepper.transform.position
        ) / 4;

        // Create two vectors along the plane defined by the feet
        Vector3 frontVector = frontRightLegStepper.transform.position - frontLeftLegStepper.transform.position;
        Vector3 sideVector = backLeftLegStepper.transform.position - frontLeftLegStepper.transform.position;
        Vector3 planeNormal = Vector3.Cross(frontVector, sideVector).normalized;

        // Ensure normal is pointing generally upwards
        if (Vector3.Dot(planeNormal, transform.up) < 0)
        {
            planeNormal *= -1;
        }

        // --- Calculate Target Rotation ---
        // 1. Calculate the target rotation in World Space
        Quaternion targetWorldRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, planeNormal), planeNormal);
        // 2. Convert the World Space rotation to the parent's Local Space
        Quaternion targetLocalRotation = Quaternion.Inverse(transform.rotation) * targetWorldRotation;


        // --- Calculate Target Position ---
        // 1. Calculate the desired WORLD position of the root bone.
        Vector3 targetWorldPosition = feetCenterPoint + (planeNormal * bodyHeight);

        // 2. Convert this world position to the LOCAL space of the GeckoBrain (the parent transform).
        Vector3 targetLocalPosition = transform.InverseTransformPoint(targetWorldPosition);

        // 3. Create the final target position using the root bone's initial X and Z
        //    and ONLY the calculated Y from our target. This prevents the feedback loop.
        Vector3 finalTargetPosition = new Vector3(
            initialRootBonePosition.x,
            targetLocalPosition.y,
            initialRootBonePosition.z
        );


        // --- Apply Smoothing ---
        smoothedBodyPosition.Step(finalTargetPosition, bodyPositionSpeed);
        smoothedBodyRotation = Quaternion.Slerp(
            smoothedBodyRotation,
            targetLocalRotation,
            1 - Mathf.Exp(-bodyRotationSpeed * Time.deltaTime)
        );

        // Apply the final LOCAL position and rotation to the root bone.
        rootBone.SetLocalPositionAndRotation(smoothedBodyPosition.currentValue, smoothedBodyRotation);
    }

    #endregion

    #region Head Tracking

    [Header("Head Tracking")]
    [Tooltip("The head bone of the gecko")]
    [SerializeField] Transform headBone;
    [Space]
    [Header("Head Tracking Settings")]
    [Tooltip("The maximum angle the head can turn to look at the target")]
    [SerializeField] float headMaxTurnAngle = 60.0f;
    [Tooltip("How fast the head will turn to look at the target")]
    [SerializeField, Range(0.1f, 40f)] float headTrackingSpeed = 20.0f;

    /// <summary>
    /// Rotates the head bone to look at the target with angle limits and smoothing.
    /// </summary>
    private void HeadTrackingUpdate()
    {
        if (!headTrackingEnabled)
        {
            headBone.localRotation = Quaternion.identity;
            return;
        }

        // Store the current head rotation since we will be resetting it
        Quaternion currentLocalRotation = headBone.localRotation;
        // Reset the head rotation so our world to local space transformation will use the head's zero rotation. 
        // Note: Quaternion.Identity is the quaternion equivalent of "zero"
        headBone.localRotation = Quaternion.identity;

        Vector3 targetWorldLookDir = target.position - headBone.position;
        Vector3 targetLocalLookDir = headBone.InverseTransformDirection(targetWorldLookDir);

        // Apply angle limit
        targetLocalLookDir = Vector3.RotateTowards(
            Vector3.forward,
            targetLocalLookDir,
            headMaxTurnAngle * Mathf.Deg2Rad,   // We multiply by Mathf.Deg2Rad here to convert degrees to radians
            0                                   // We don't care about the length here, so we leave it at zero
        );

        // Get the local rotation by using LookRotation on a local directional vector
        Quaternion targetLocalRotation = Quaternion.LookRotation(targetLocalLookDir, Vector3.up);

        // Apply smoothing
        headBone.localRotation = Quaternion.Slerp(
          currentLocalRotation,
          targetLocalRotation,
          1 - Mathf.Exp(-headTrackingSpeed * Time.deltaTime)
        );
    }

    #endregion

    #region Eye Tracking

    [Header("Eye Tracking Settings")]
    // References to the eye bones
    [SerializeField] Transform leftEyeBone;
    [SerializeField] Transform rightEyeBone;
    [Space]
    [Tooltip("How fast the eyes will turn to look at the target")]
    [SerializeField, Range(0.1f, 40f)] float eyeTrackingSpeed = 30.0f;
    // The maximum and minimum rotation angles for the eyes
    [SerializeField] float leftEyeMaxYRotation = 10.0f;
    [SerializeField] float leftEyeMinYRotation = -180.0f;
    [SerializeField] float rightEyeMaxYRotation = 180.0f;
    [SerializeField] float rightEyeMinYRotation = -10.0f;

    /// <summary>
    /// Rotates the eye bones to look at the target with angle limits and smoothing.
    /// </summary>
    private void EyeTrackingUpdate()
    {
        if (!eyeTrackingEnabled)
        {
            leftEyeBone.localRotation = Quaternion.identity;
            rightEyeBone.localRotation = Quaternion.identity;
            return;
        }

        // Note: We use head position here just because the gecko doesn't
        // look so great when cross eyed. To make it relative to the eye 
        // itself, subtract the eye's position instead of the head's.
        Quaternion targetEyeRotation = Quaternion.LookRotation(
          target.position - headBone.position, // toward target
          transform.up
        );

        leftEyeBone.rotation = Quaternion.Slerp(
          leftEyeBone.rotation,
          targetEyeRotation,
          1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
        );

        rightEyeBone.rotation = Quaternion.Slerp(
          rightEyeBone.rotation,
          targetEyeRotation,
          1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
        );

        float leftEyeCurrentYRotation = leftEyeBone.localEulerAngles.y;
        float rightEyeCurrentYRotation = rightEyeBone.localEulerAngles.y;

        // Move the rotation to a -180 ~ 180 range
        if (leftEyeCurrentYRotation > 180)
        {
            leftEyeCurrentYRotation -= 360;
        }
        if (rightEyeCurrentYRotation > 180)
        {
            rightEyeCurrentYRotation -= 360;
        }

        // Clamp the Y axis rotation
        float leftEyeClampedYRotation = Mathf.Clamp(
            leftEyeCurrentYRotation,
            leftEyeMinYRotation,
            leftEyeMaxYRotation
        );
        float rightEyeClampedYRotation = Mathf.Clamp(
            rightEyeCurrentYRotation,
            rightEyeMinYRotation,
            rightEyeMaxYRotation
        );

        // Apply the clamped Y rotation without changing the X and Z rotations
        leftEyeBone.localEulerAngles = new Vector3(
            leftEyeBone.localEulerAngles.x,
            leftEyeClampedYRotation,
            leftEyeBone.localEulerAngles.z
        );
        rightEyeBone.localEulerAngles = new Vector3(
            rightEyeBone.localEulerAngles.x,
            rightEyeClampedYRotation,
            rightEyeBone.localEulerAngles.z
        );
    }

    #endregion

    #region Tail

    [Header("Tail")]
    // All the tail bone transforms in order from the base to the tip.
    [SerializeField] Transform[] tailBones;
    // How intensely the tail curls in response to turning.
    [SerializeField] float tailTurnMultiplier = 15.0f;
    // How quickly the tail responds to changes in direction.
    [SerializeField, Range(1f, 20f)] float tailTurnSpeed = 8.0f;

    // Stores the default, resting rotation of each tail bone.
    Quaternion[] tailHomeLocalRotation;
    // The current, smoothed sway angle of the tail.
    SmoothDamp.Float tailRotation;

    /// <summary>
    /// Caches the tail's default pose at the start of the game.
    /// </summary>
    void TailInitialize()
    {
        tailHomeLocalRotation = new Quaternion[tailBones.Length];
        for (int i = 0; i < tailHomeLocalRotation.Length; i++)
        {
            tailHomeLocalRotation[i] = tailBones[i].localRotation;
        }
    }

    /// <summary>
    /// Applies procedural sway to the tail based on the body's rotation.
    /// </summary>
    void TailUpdate()
    {
        if (tailSwayEnabled)
        {
            // Calculate a target sway angle that opposes the body's current turn direction.
            tailRotation.Step(-currentAngularVelocity / turnSpeed * tailTurnMultiplier, tailTurnSpeed);

            // Apply the calculated sway to each tail bone.
            for (int i = 0; i < tailBones.Length; i++)
            {
                // Create a rotation from our single sway value.
                Quaternion rotation = Quaternion.Euler(0, tailRotation, 0);
                // Multiply the sway by the bone's default rotation to add the effect.
                tailBones[i].localRotation = rotation * tailHomeLocalRotation[i];
            }
        }
        else
        {
            // If disabled, reset the tail to its resting pose.
            for (int i = 0; i < tailBones.Length; i++)
            {
                tailBones[i].localRotation = tailHomeLocalRotation[i];
            }
        }
    }

    #endregion

    #region Legs

    [Header("Legs")]
    // References to the controller script for each of the four legs.
    [SerializeField] LegStepper frontLeftLegStepper;
    [SerializeField] LegStepper frontRightLegStepper;
    [SerializeField] LegStepper backLeftLegStepper;
    [SerializeField] LegStepper backRightLegStepper;

    /// <summary>
    /// A coroutine that manages the leg stepping gait pattern.
    /// </summary>
    IEnumerator LegUpdateCoroutine()
    {
        // This coroutine runs forever.
        while (true)
        {
            // Pause the gait cycle if leg stepping is disabled.
            while (!legSteppingEnabled) yield return null;

            // --- First Diagonal Pair ---
            do
            {
                // Tell the legs to check if they should move.
                frontLeftLegStepper.TryMove();
                backRightLegStepper.TryMove();
                // Wait one frame before the next check.
                yield return null;
                // Stay in this loop until both legs in the pair have finished stepping.
            } while (backRightLegStepper.Moving || frontLeftLegStepper.Moving);

            // --- Second Diagonal Pair ---
            do
            {
                frontRightLegStepper.TryMove();
                backLeftLegStepper.TryMove();
                yield return null;
            } while (backLeftLegStepper.Moving || frontRightLegStepper.Moving);
        }
    }

    #endregion
}