using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Leg[] legs;

    private float maxTipWait = 0.7f;

    private bool readySwitchOrder = false;
    private bool stepOrder = true;
    private float bodyHeightBase = 1.3f;

    private Vector3 bodyPos;
    private Vector3 bodyUp;
    private Vector3 bodyForward;
    private Vector3 bodyRight;
    private Quaternion bodyRotation;

    private float PosAdjustRatio = 0.1f;
    private float RotAdjustRatio = 0.2f;

    private int legUpdateIndex = 0;

    // Define leg groups. This requires careful setup based on leg names/indices.
    // Example for a tripod gait:
    private int[] groupA = { 0, 2, 5, 7 }; // Front-left, Mid-left, Mid-right, Back-right
    private int[] groupB = { 1, 3, 4, 6 }; // And the rest

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

        // If tip is not in current order but it's too far from target position, Switch the order
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i].TipDistance > maxTipWait)
            {
                stepOrder = i % 2 == 0;
                break;
            }
        }

        // Ordering steps
        foreach (Leg leg in legs)
        {
            leg.Movable = stepOrder;
            stepOrder = !stepOrder;
        }

        int index = stepOrder ? 0 : 1;

        // If the opposite foot step completes, switch the order to make a new step
        if (readySwitchOrder && !legs[index].Animating)
        {
            stepOrder = !stepOrder;
            readySwitchOrder = false;
        }

        if (!readySwitchOrder && legs[index].Animating)
        {
            readySwitchOrder = true;
        }

        // Update only one or two legs' raycasts per frame
        legs[legUpdateIndex].UpdateRaycast();
        legUpdateIndex = (legUpdateIndex + 1) % legs.Length;

        // You might update a second one for symmetry
        int oppositeIndex = (legUpdateIndex + legs.Length / 2) % legs.Length;
        legs[oppositeIndex].UpdateRaycast();
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

            // Interpolate postition from old to new
            bodyPos = tipCenter + bodyUp * bodyHeightBase;
            bodyTransform.position = Vector3.Lerp(bodyTransform.position, bodyPos, PosAdjustRatio);

            // Calculate new body axis
            bodyRight = Vector3.Cross(bodyUp, bodyTransform.forward);
            bodyForward = Vector3.Cross(bodyRight, bodyUp);

            // Interpolate rotation from old to new
            bodyRotation = Quaternion.LookRotation(bodyForward, bodyUp);
            bodyTransform.rotation = Quaternion.Slerp(bodyTransform.rotation, bodyRotation, RotAdjustRatio);

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
