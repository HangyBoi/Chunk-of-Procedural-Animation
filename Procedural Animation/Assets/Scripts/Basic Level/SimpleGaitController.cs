using UnityEngine;

public class SimpleGaitController : MonoBehaviour
{
    [Header("Leg Groups")]
    [Tooltip("The first group of legs that will move together.")]
    public SimpleLeg[] legGroupA;

    [Tooltip("The second group of legs that will move together.")]
    public SimpleLeg[] legGroupB;

    [Header("Gait Timing")]
    [Tooltip("How long to wait after one group moves before the next can move.")]
    public float stepCooldown = 0.2f;

    // True: Group A's turn, False: Group B's turn
    private bool isGroupATurn = true;
    private float lastStepTime;

    void Update()
    {
        // Check if enough time has passed since the last group stepped
        if (Time.time < lastStepTime + stepCooldown)
        {
            return; // It's too soon, wait for the cooldown
        }

        if (isGroupATurn)
        {
            // Group A's turn.
            // First, check if any leg in the OTHER group (B) is still moving.
            if (IsAnyLegStepping(legGroupB))
            {
                return; // Wait for Group B to finish its steps.
            }

            // Tell every leg in Group A to try and take a step.
            foreach (var leg in legGroupA)
            {
                leg.TryStep();
            }

            // Switch turns and update the timestamp.
            isGroupATurn = false;
            lastStepTime = Time.time;
        }
        else
        {
            // Group B's turn.
            // Check if any leg in Group A is still moving.
            if (IsAnyLegStepping(legGroupA))
            {
                return; // Wait for Group A to finish.
            }

            // Tell every leg in Group B to try and take a step.
            foreach (var leg in legGroupB)
            {
                leg.TryStep();
            }

            // Switch turns and update the timestamp.
            isGroupATurn = true;
            lastStepTime = Time.time;
        }
    }

    /// <summary>
    /// Checks if any leg in the provided group is currently in its stepping coroutine.
    /// </summary>
    private bool IsAnyLegStepping(SimpleLeg[] legGroup)
    {
        foreach (var leg in legGroup)
        {
            if (leg.IsStepping)
            {
                return true; // Found a leg that is still moving
            }
        }
        return false; // All legs in this group are stationary
    }
}