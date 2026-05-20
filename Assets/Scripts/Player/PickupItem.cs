using UnityEngine;
using DG.Tweening;

public class PickupItem : MonoBehaviour
{
    [Header("Collider")]
    public Collider[] itemColliders;

    [Header("Outline")]
    public Outline outlineScript;

    [Header("Equipped Transform (Right Hand)")]
    public Vector3 equippedLocalPosition = Vector3.zero;
    public Vector3 equippedLocalRotation = Vector3.zero;
    public Vector3 equippedLocalScale = Vector3.one;

    [Header("Equipped Transform (Left Hand Override)")]
    [Tooltip("If unchecked, left hand uses the right-hand values above.")]
    public bool useLeftHandOverride = false;
    public Vector3 leftEquippedLocalPosition = Vector3.zero;
    public Vector3 leftEquippedLocalRotation = Vector3.zero;
    public Vector3 leftEquippedLocalScale = Vector3.one;

    [Header("Hold Animation")]
    [Tooltip("Which hand-pose animation to play while this item is held.")]
    public HoldAnimationType holdAnimationType = HoldAnimationType.None;

    [Header("Pickup Animation")]
    public float pickupAnimationDuration = 0.3f;
    public Ease pickupEaseType = Ease.OutBack;

    [Header("Drop Physics")]
    public float mass = 1f;
    public float drag = 0f;

    public GameObject CameraRecoilObject;
    public CameraRecoil CameraRecoilScript;
    public float MinRotation = 1.5f;
    public float MaxRotation = 2.5f;

    protected Rigidbody rb;
    private Tween currentTween;


    // ?? Throw Damage ?????????????????????????????????????????????????????????
    [Header("Throw Damage")]
    [Tooltip("Damage dealt to enemies on impact when thrown.")]
    public float throwDamage = 30f;

    [Tooltip("Minimum velocity magnitude required to deal throw damage. " +
             "Prevents damage from slow rolls or accidental drops.")]
    public float minThrowVelocity = 3f;

    // Tracks whether this item is mid-flight after being thrown
    // so it only deals damage once per throw, not on every bounce.
    private bool isThrown = false;
    private bool hasDealtThrowDamage = false;


    public bool IsHeld { get; private set; }
    public Transform CurrentContainer { get; private set; }

    protected virtual void Start()
    {

        CameraRecoilObject = GameObject.Find("CameraRecoil");
        CameraRecoilScript = CameraRecoilObject.GetComponent<CameraRecoil>();

        rb = GetComponent<Rigidbody>();
        if (outlineScript != null) outlineScript.enabled = false;
    }

    public void ShowOutline(bool show)
    {
        if (outlineScript != null) outlineScript.enabled = show;
    }

    // ?? resolve which set of values to use ??????????????????????????????????
    private Vector3 GetTargetPosition(bool isLeft) =>
        (isLeft && useLeftHandOverride) ? leftEquippedLocalPosition : equippedLocalPosition;

    private Quaternion GetTargetRotation(bool isLeft) =>
        Quaternion.Euler((isLeft && useLeftHandOverride)
            ? leftEquippedLocalRotation
            : equippedLocalRotation);

    private Vector3 GetTargetScale(bool isLeft) =>
        (isLeft && useLeftHandOverride) ? leftEquippedLocalScale : equippedLocalScale;
    // ????????????????????????????????????????????????????????????????????????

    public void OnPickUp(Transform container, bool isLeftHand = false)
    {
        IsHeld = true;
        CurrentContainer = container;


        isThrown = false;
        hasDealtThrowDamage = false;


        if (rb != null) { Destroy(rb); rb = null; }
        foreach (Collider col in itemColliders)
            if (col != null) col.isTrigger = true;

        ShowOutline(false);
        transform.SetParent(container);

        Vector3 targetPos = GetTargetPosition(isLeftHand);
        Quaternion targetRot = GetTargetRotation(isLeftHand);
        Vector3 targetScale = GetTargetScale(isLeftHand);

        transform.localScale = targetScale;
        transform.localPosition = targetPos + new Vector3(0f, -0.3f, 0f); // start below
        transform.localRotation = targetRot;

        currentTween?.Kill();
        currentTween = transform
            .DOLocalMove(targetPos, pickupAnimationDuration)
            .SetEase(pickupEaseType);

        OnPickedUpCallback();
    }

    public void OnDrop(Vector3 playerVelocity,
                       Vector3 camForward,
                       Vector3 camUp,
                       float forwardForce,
                       float upwardForce)
    {
        currentTween?.Kill();
        IsHeld = false;
        CurrentContainer = null;

        ShowOutline(false);
        transform.SetParent(null);

        foreach (Collider col in itemColliders)
            if (col != null) col.isTrigger = false;

        rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.isKinematic = false;

        CameraRecoilScript.DropingRecoilEffect();

        rb.linearVelocity = playerVelocity;
        rb.AddForce(camForward * forwardForce, ForceMode.Impulse);
        rb.AddForce(camUp * upwardForce, ForceMode.Impulse);

        float rand = Random.Range(MinRotation, MaxRotation);
        rb.AddRelativeTorque(new Vector3(0f, 0f, rand) * 10f);

        isThrown = true;
        hasDealtThrowDamage = false;

        OnDroppedCallback();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only deal damage if actively thrown, not just dropped or placed
        if (!isThrown || hasDealtThrowDamage) return;

        // Ignore slow-moving collisions (rolling to a stop, accidental drops)
        if (rb != null && rb.linearVelocity.magnitude < minThrowVelocity) return;

        Target target = collision.collider.GetComponentInParent<Target>();
        if (target != null)
        {
            // Hit direction = velocity of the thrown object at moment of impact
            Vector3 hitDirection = rb != null
                ? rb.linearVelocity.normalized
                : transform.forward;

            target.TakeDamage(throwDamage, hitDirection);

            // Only damage once per throw — subsequent bounces deal no damage
            hasDealtThrowDamage = true;
        }

        // Stop tracking as thrown after first solid hit regardless
        isThrown = false;
    }

    protected virtual void OnPickedUpCallback() { }
    protected virtual void OnDroppedCallback() { }
}