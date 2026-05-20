using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manages picking up and dropping any PickupItem (pistols, knives, books, etc.)
/// into the player's left and right hand slots.
///
/// Replaces DualPistolManager — all pistol-specific logic now lives in PistolPickup.
/// </summary>
public class ItemPickupManager : MonoBehaviour
{
    [Header("Hand Containers")]
    public Transform rightHandContainer;
    public Transform leftHandContainer;

    [Header("Hand Mesh Roots (toggled active/inactive)")]
    public GameObject leftHandMesh;
    public GameObject rightHandMesh;

    [Header("Hand Pickup Animation")]
    [Tooltip("Vertical offset below rest position the hand starts from when animating in")]
    public float handPickupYOffset = -0.3f;
    public float handPickupDuration = 0.3f;
    public Ease handPickupEase = Ease.OutBack;

    [Header("References")]
    public Rigidbody playerRb;
    public Camera fpsCam;
    public HandAnimationDriver handAnimationDriver;

    [Header("Pickup Settings")]
    public float pickUpRange = 6f;

    [Header("SphereCast Settings")]
    [Tooltip("Radius of the spherecast — higher = more forgiving aim")]
    public float aimSphereRadius = 0.4f;
    [Tooltip("Layer(s) your pickupable items are on")]
    public LayerMask pickupLayer;

    [Header("Drop Settings")]
    public float dropForwardForce = 5f;
    public float dropUpwardForce = 2f;

    [Header("Keys")]
    public KeyCode pickUpRightKey = KeyCode.Mouse1;
    public KeyCode pickUpLeftKey = KeyCode.Mouse0;
    public KeyCode dropRightKey = KeyCode.E;
    public KeyCode dropLeftKey = KeyCode.Q;

    // Held items
    private PickupItem rightHandItem;
    private PickupItem leftHandItem;

    // Outline tracking
    private PickupItem lastOutlined;

    // Cached rest positions and active tweens for each hand container
    // We tween ContainerRightHand / ContainerLeftHand (parents of the gun slots) because
    // the Animator on "WeaponPosition & Animations" directly drives HandRight/HandLeft
    // localPosition every frame, which would stomp any DOTween applied to those objects.
    // The container GameObjects are NOT animated by any clip, so tweening them is safe.
    private Transform rightHandContainerRoot;
    private Transform leftHandContainerRoot;
    private Vector3 rightContainerRestPos;
    private Vector3 leftContainerRestPos;
    private Tween rightHandTween;
    private Tween leftHandTween;

    private void Start()
    {
        // rightHandContainer points to RightGunForKnowingJustRotationAndPosition;
        // its parent is ContainerRightHand — the node we tween.
        if (rightHandContainer != null)
        {
            rightHandContainerRoot = rightHandContainer.parent;
            rightContainerRestPos = rightHandContainerRoot.localPosition;
        }

        if (leftHandContainer != null)
        {
            leftHandContainerRoot = leftHandContainer.parent;
            leftContainerRestPos = leftHandContainerRoot.localPosition;
        }

        if (leftHandMesh != null) leftHandMesh.SetActive(false);
        if (rightHandMesh != null) rightHandMesh.SetActive(false);
    }

    private void Update()
    {
        PickupItem aimed = GetAimedItem();
        HandleOutline(aimed);

        // Pick up
        if (Input.GetKeyDown(pickUpRightKey) && rightHandItem == null && aimed != null)
        {
            PickUp(ref rightHandItem, rightHandContainer, aimed);
            if (rightHandMesh != null) rightHandMesh.SetActive(true);
            AnimateContainerIn(rightHandContainerRoot, ref rightHandTween, rightContainerRestPos);
            handAnimationDriver?.SetRightHand(aimed.holdAnimationType);
        }

        if (Input.GetKeyDown(pickUpLeftKey) && leftHandItem == null && aimed != null)
        {
            PickUp(ref leftHandItem, leftHandContainer, aimed);
            if (leftHandMesh != null) leftHandMesh.SetActive(true);
            AnimateContainerIn(leftHandContainerRoot, ref leftHandTween, leftContainerRestPos);
            handAnimationDriver?.SetLeftHand(aimed.holdAnimationType);
        }

        // Drop
        if (Input.GetKeyDown(dropRightKey) && rightHandItem != null)
        {
            Drop(ref rightHandItem);
            rightHandTween?.Kill();
            rightHandTween = null;
            if (rightHandMesh != null) rightHandMesh.SetActive(false);
            handAnimationDriver?.SetRightHand(HoldAnimationType.None);
        }

        if (Input.GetKeyDown(dropLeftKey) && leftHandItem != null)
        {
            Drop(ref leftHandItem);
            leftHandTween?.Kill();
            leftHandTween = null;
            if (leftHandMesh != null) leftHandMesh.SetActive(false);
            handAnimationDriver?.SetLeftHand(HoldAnimationType.None);
        }

        // Hide hand meshes when slots are empty
        if (rightHandItem == null && rightHandMesh != null) rightHandMesh.SetActive(false);
        if (leftHandItem == null && leftHandMesh != null) leftHandMesh.SetActive(false);
    }

    /// <summary>
    /// Snaps ContainerRightHand / ContainerLeftHand below their rest Y and tweens them
    /// back up. These containers are not driven by the Animator, so DOTween runs freely.
    /// </summary>
    private void AnimateContainerIn(Transform containerRoot, ref Tween tween, Vector3 restPos)
    {
        if (containerRoot == null) return;

        tween?.Kill();
        containerRoot.localPosition = restPos + new Vector3(0f, handPickupYOffset, 0f);

        tween = containerRoot
            .DOLocalMove(restPos, handPickupDuration)
            .SetEase(handPickupEase);
    }

    // ?? SphereCast ??????????????????????????????????????????????????????????
    /// <summary>
    /// Returns the first PickupItem the camera is aimed at that isn't already held.
    /// </summary>
    private PickupItem GetAimedItem()
    {
        Ray ray = new Ray(fpsCam.transform.position, fpsCam.transform.forward);

        // SphereCastAll so a held weapon in front never blocks the one behind it.
        // QueryTriggerInteraction.Ignore skips held items (their colliders become triggers on pickup).
        RaycastHit[] hits = Physics.SphereCastAll(
            ray, aimSphereRadius, pickUpRange, pickupLayer,
            QueryTriggerInteraction.Ignore);

        // Find the closest valid (not already held) item
        PickupItem closest = null;
        float closestDist = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            PickupItem item = hit.collider.GetComponent<PickupItem>();

            if (item == null) continue;   // not a pickup item
            if (item == rightHandItem) continue;   // already in right hand
            if (item == leftHandItem) continue;   // already in left hand

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                closest = item;
            }
        }

        return closest;
    }

    // ?? Outline ?????????????????????????????????????????????????????????????
    private void HandleOutline(PickupItem aimed)
    {
        if (aimed == lastOutlined) return;

        if (lastOutlined != null) lastOutlined.ShowOutline(false);
        if (aimed != null) aimed.ShowOutline(true);

        lastOutlined = aimed;
    }

    // ?? Pick / Drop helpers ?????????????????????????????????????????????????
    private void PickUp(ref PickupItem slot, Transform container, PickupItem target)
    {
        target.ShowOutline(false);
        if (lastOutlined == target) lastOutlined = null;

        slot = target;
        bool isLeft = container == leftHandContainer;
        slot.OnPickUp(container, isLeftHand: isLeft);
    }

    private void Drop(ref PickupItem slot)
    {
        slot.OnDrop(
            playerRb.linearVelocity,
            fpsCam.transform.forward,
            fpsCam.transform.up,
            dropForwardForce,
            dropUpwardForce
        );
        slot = null;
    }

    // ?? Gizmos ??????????????????????????????????????????????????????????????
    private void OnDrawGizmos()
    {
        if (fpsCam == null) return;

        Ray ray = new Ray(fpsCam.transform.position, fpsCam.transform.forward);
        PickupItem aimed = GetAimedItem();

        Gizmos.color = aimed != null ? Color.green : Color.yellow;

        int segments = 10;
        for (int i = 0; i <= segments; i++)
        {
            float t = (pickUpRange / segments) * i;
            Gizmos.DrawWireSphere(ray.origin + ray.direction * t, aimSphereRadius);
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * pickUpRange);

        if (aimed != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(aimed.transform.position, 0.15f);
        }
    }
}