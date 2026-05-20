using UnityEngine;

public class DualPistolManager : MonoBehaviour
{
    [Header("Containers")]
    public Transform rightHandContainer;
    public Transform leftHandContainer;

    public GameObject leftHand;
    public GameObject rightHand;

    [Header("References")]
    public Transform player;
    public Rigidbody playerRb;
    public Camera fpsCam;

    [Header("Pickup Settings")]
    public float pickUpRange = 6f;

    [Header("SphereCast Settings")]
    [Tooltip("Radius of the spherecast — higher = more forgiving aim")]
    public float aimSphereRadius = 0.4f;
    [Tooltip("Layer(s) your pistol colliders are on")]
    public LayerMask pistolLayer;

    [Header("Drop Settings")]
    public float dropForwardForce = 5f;
    public float dropUpwardForce = 2f;

    [Header("Keys")]
    public KeyCode pickUpRightKey = KeyCode.Mouse1;
    public KeyCode pickUpLeftKey = KeyCode.Mouse0;
    public KeyCode dropRightKey = KeyCode.E;
    public KeyCode dropLeftKey = KeyCode.Q;

    private PistolPickup rightHandGun;
    private PistolPickup leftHandGun;

    private PistolPickup lastOutlined;   // single tracked outline target

    private void Start()
    {
        leftHand.SetActive(false);
        rightHand.SetActive(false);
    }

    // ?????????????????????????????????????????????
    private void Update()
    {
        PistolPickup aimed = GetAimedPistol();

        HandleOutline(aimed);

        // Pick up — uses the pistol the crosshair is pointing at
        if (Input.GetKeyDown(pickUpRightKey) && rightHandGun == null && aimed != null)
        {
            PickUp(ref rightHandGun, rightHandContainer, aimed);
            rightHand.SetActive(true);
        }
            

        if (Input.GetKeyDown(pickUpLeftKey) && leftHandGun == null && aimed != null)
        {
            PickUp(ref leftHandGun, leftHandContainer, aimed);
            leftHand.SetActive(true);
        }
            

        // Drop
        if (Input.GetKeyDown(dropRightKey) && rightHandGun != null)
            Drop(ref rightHandGun);

        if (Input.GetKeyDown(dropLeftKey) && leftHandGun != null)
            Drop(ref leftHandGun);

        if (leftHandGun == null)
        {
            leftHand.SetActive(false);
        }

        if (rightHandGun == null)
        {
            rightHand.SetActive(false);
        }
    }

    // ?????????????????????????????????????????????
    /// <summary>
    /// Fires a SphereCast from the camera along its forward vector.
    /// Returns the first PistolPickup hit that isn't already held.
    /// </summary>
    private PistolPickup GetAimedPistol()
    {
        Ray ray = new Ray(fpsCam.transform.position, fpsCam.transform.forward);

        // SphereCast: origin, radius, direction, out hit, max distance, layer mask
        if (Physics.SphereCast(ray, aimSphereRadius, out RaycastHit hit, pickUpRange, pistolLayer))
        {
            PistolPickup pistol = hit.collider.GetComponent<PistolPickup>();

            // Ignore if already held in either hand
            if (pistol != null && pistol != rightHandGun && pistol != leftHandGun)
                return pistol;
        }

        return null;
    }

    // ?????????????????????????????????????????????
    private void HandleOutline(PistolPickup aimed)
    {
        if (aimed == lastOutlined) return;  // no change, skip

        // Turn off previous outline
        if (lastOutlined != null)
            lastOutlined.ShowOutline(false);

        // Turn on new outline (or nothing if aimed == null)
        if (aimed != null)
            aimed.ShowOutline(true);

        lastOutlined = aimed;
    }

    // ?????????????????????????????????????????????
    private void PickUp(ref PistolPickup slot, Transform container, PistolPickup target)
    {
        target.ShowOutline(false);
        if (lastOutlined == target) lastOutlined = null;

        slot = target;
        slot.OnPickUp(container);
    }

    private void Drop(ref PistolPickup slot)
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

    private void OnDrawGizmos()
    {
        if (fpsCam == null) return;

        Ray ray = new Ray(fpsCam.transform.position, fpsCam.transform.forward);
        PistolPickup aimed = GetAimedPistol();

        // Color green if hitting a pistol, yellow otherwise
        Gizmos.color = aimed != null ? Color.green : Color.yellow;

        // Draw the SphereCast as a series of wire spheres along the ray
        int segments = 10;
        for (int i = 0; i <= segments; i++)
        {
            float t = (pickUpRange / segments) * i;
            Gizmos.DrawWireSphere(ray.origin + ray.direction * t, aimSphereRadius);
        }

        // Draw the center line
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * pickUpRange);

        // Draw a solid sphere at the hit point if something is aimed at
        if (aimed != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(aimed.transform.position, 0.15f);
        }
    }

}