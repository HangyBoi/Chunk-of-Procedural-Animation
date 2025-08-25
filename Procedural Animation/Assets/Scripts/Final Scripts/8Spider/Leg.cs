using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leg : MonoBehaviour
{
    // Self-explanatory variable names
    private LegController legController;

    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform rayOrigin;
    public GameObject ikTarget;

    [SerializeField] private AnimationCurve speedCurve;
    [SerializeField] private AnimationCurve heightCurve;

    [Header("Stepping Properties")]
    [SerializeField] private float stepDistance = 0.55f;
    [SerializeField] private float stepOvershoot = 0.2f;
    [SerializeField] private float stepHeight = 0.2f;
    [SerializeField] private float stepDuration = 0.15f;
    [SerializeField] private float raycastMaxDistance = 7.0f;
    [SerializeField] private float ikTargetVerticalOffset = 1.0f;

    private const float STEP_DURATION_FRAME_TIME = 1.0f / 60.0f;

    public Vector3 TipPos { get; private set; }
    public Vector3 TipUpDir { get; private set; }
    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }

    public bool Animating { get; private set; } = false;
    public bool Movable { get; set; } = false;
    public float TipDistance { get; private set; }

    private void Awake()
    {
        legController = GetComponentInParent<LegController>();

        transform.parent = bodyTransform;
        rayOrigin.parent = bodyTransform;
        TipPos = ikTarget.transform.position;
    }

    private void Start()
    {
        UpdateIKTargetTransform();
    }

    // Call this from LegController
    public void UpdateRaycast()
    {
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin.position, bodyTransform.up.normalized * -1, out hit, raycastMaxDistance))
        {
            RaycastTipPos = hit.point;
            RaycastTipNormal = hit.normal;
        }
    }

    private void Update()
    {
        TipDistance = (RaycastTipPos - TipPos).magnitude;

        // If the distance gets too far, animate and move the tip to new position
        if (!Animating && (TipDistance > stepDistance && Movable))
        {
            StartCoroutine(AnimateLeg());
        }
    }

    private IEnumerator AnimateLeg()
    {
        Animating = true;

        float timer = 0.0f;
        float animTime;

        Vector3 startingTipPos = TipPos;
        Vector3 tipDirVec = RaycastTipPos - TipPos;
        tipDirVec += tipDirVec.normalized * stepOvershoot;

        Vector3 right = Vector3.Cross(bodyTransform.up, tipDirVec.normalized).normalized;
        TipUpDir = Vector3.Cross(tipDirVec.normalized, right);

        while (timer < stepDuration + STEP_DURATION_FRAME_TIME)
        {
            animTime = speedCurve.Evaluate(timer / stepDuration);

            // If the target is keep moving, apply acceleration to correct the end point
            float tipAcceleration = Mathf.Max((RaycastTipPos - startingTipPos).magnitude / tipDirVec.magnitude, 1.0f);

            TipPos = startingTipPos + tipDirVec * tipAcceleration * animTime; // Forward direction of tip vector
            TipPos += TipUpDir * heightCurve.Evaluate(animTime) * stepHeight; // Upward direction of tip vector

            UpdateIKTargetTransform();

            timer += STEP_DURATION_FRAME_TIME;

            yield return new WaitForSeconds(STEP_DURATION_FRAME_TIME);
        }

        Animating = false;
    }

    private void UpdateIKTargetTransform()
    {
        // Update leg ik target transform depend on tip information
        ikTarget.transform.position = TipPos + bodyTransform.up.normalized * ikTargetVerticalOffset;
        ikTarget.transform.rotation = Quaternion.LookRotation(TipPos - ikTarget.transform.position) * Quaternion.Euler(90, 0, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(RaycastTipPos, 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(TipPos, 0.1f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(TipPos, RaycastTipPos);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(ikTarget.transform.position, 0.1f);
    }
}
