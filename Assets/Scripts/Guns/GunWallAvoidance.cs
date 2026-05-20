using UnityEngine;

public class GunWallAvoidance : MonoBehaviour
{
    [Header("Settings")]
    public float moveBackDistance = 0.5f;
    public float moveSpeed = 8f;
    public float checkDistance = 2f;
    public float sphereRadius = 0.3f;
    public LayerMask wallMask;

    [Header("Dive Pullback")]
    [Tooltip("How far the hand pulls back along the camera's local Z during a dive.")]
    public float divePullbackDistance = 0.3f;
    [Tooltip("How fast the hand moves to/from the dive position.")]
    public float divePullbackSpeed = 10f;

    private Camera cam;
    private Vector3 initialLocalPos;
    private Vector3 targetLocalPos;

    [HideInInspector] public Vector3 kickOffset;

    public PlayerMovementTutorial MovementScript;

    [Header("References")]
    public PlayerDive diveScript;

    void Start()
    {
        cam = Camera.main;
        if (!cam)
        {
            Debug.LogError("Main Camera not found!");
            enabled = false;
            return;
        }

        initialLocalPos = transform.localPosition;
        targetLocalPos = initialLocalPos;
    }

    void Update()
    {
        if (!cam) return;

        if (diveScript != null && diveScript.isDiving)
        {
            // Pull the hand backward in camera-local space during the dive.
            // Uses a separate speed so the snap-back on landing can be tuned independently.
            Vector3 diveTarget = initialLocalPos + Vector3.back * divePullbackDistance;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                diveTarget + kickOffset,
                Time.deltaTime * divePullbackSpeed
            );
            return;
        }

        bool hitWall = Physics.SphereCast(
            cam.transform.position,
            sphereRadius,
            cam.transform.forward,
            out RaycastHit hit,
            checkDistance,
            wallMask
        );

        Vector3 cameraBackward = -cam.transform.forward;
        Vector3 localBackDir = cam.transform.InverseTransformDirection(cameraBackward);

        if (hitWall)
            targetLocalPos = initialLocalPos + localBackDir * moveBackDistance;
        else
            targetLocalPos = initialLocalPos;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetLocalPos + kickOffset,
            Time.deltaTime * moveSpeed
        );
    }

    void OnDrawGizmos()
    {
        if (Camera.main)
        {
            Camera c = Camera.main;
            Gizmos.color = Color.yellow;
            Vector3 start = c.transform.position;
            Vector3 end = start + c.transform.forward * checkDistance;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, sphereRadius);
            Gizmos.DrawWireSphere(end, sphereRadius);
        }
    }
}