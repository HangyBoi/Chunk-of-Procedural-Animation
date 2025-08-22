using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is the "brain" of the spider. It coordinates all the LegSolver scripts
// and adjusts the body's position and orientation to match the terrain.
public class SpiderController : MonoBehaviour
{
    [Header("Body Components")]
    [SerializeField] private Transform body; // The spider's main body transform
    [SerializeField] private LegSolver[] legs; // An array of all the leg solvers

    [Header("Gait Settings")]
    [Tooltip("How many legs can be moving at the same time.")]
    [SerializeField] private int maxMovingLegs = 2;

    [Header("Body Orientation")]
    [SerializeField] private float bodyHeight = 1.5f; // How high the body should be off the ground
    [SerializeField] private float bodyAdjustSpeed = 5f; // How quickly the body adjusts its position and rotation

    private void Start()
    {
        // Start the coroutine that continuously manages the leg movement sequence
        StartCoroutine(ManageGait());
    }

    private void Update()
    {
        AdjustBodyTransform();
    }

    private IEnumerator ManageGait()
    {
        // This is a continuous loop that runs in the background
        while (true)
        {
            int movingLegsCount = 0;
            foreach (var leg in legs)
            {
                if (leg.IsMoving)
                {
                    movingLegsCount++;
                }
            }

            // If we have room to move another leg, find one that needs to step
            if (movingLegsCount < maxMovingLegs)
            {
                // Simple gait: just tell all legs they are allowed to move if they need to.
                // The LegSolver script itself checks the distance and decides if a step is necessary.
                foreach (var leg in legs)
                {
                    leg.Movable = true;
                }
            }
            else // If too many legs are moving, tell all non-moving legs to wait
            {
                foreach (var leg in legs)
                {
                    if (!leg.IsMoving)
                    {
                        leg.Movable = false;
                    }
                }
            }

            yield return null; // Wait for the next frame
        }
    }

    private void AdjustBodyTransform()
    {
        // --- 1. Calculate Average Position ---
        Vector3 averagePosition = Vector3.zero;
        foreach (var leg in legs)
        {
            averagePosition += leg.CurrentPosition;
        }
        averagePosition /= legs.Length;

        // --- 2. Calculate Average Normal (for tilting the body) ---
        Vector3 averageNormal = Vector3.zero;
        foreach (var leg in legs)
        {
            averageNormal += leg.CurrentNormal;
        }
        averageNormal.Normalize();

        // --- 3. Calculate Target Body Position and Rotation ---
        // The target position is above the center of the feet, oriented by the average normal
        Vector3 targetPosition = averagePosition + averageNormal * bodyHeight;

        // The target rotation looks forward, but its "up" direction is the average ground normal
        Quaternion targetRotation = Quaternion.LookRotation(body.forward, averageNormal);

        // --- 4. Smoothly Interpolate (Lerp) the Body to the Target ---
        body.position = Vector3.Lerp(body.position, targetPosition, Time.deltaTime * bodyAdjustSpeed);
        body.rotation = Quaternion.Slerp(body.rotation, targetRotation, Time.deltaTime * bodyAdjustSpeed);
    }
}