using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerDive : MonoBehaviour
{
    [Header("Dive Forces")]
    public float diveForwardImpulse = 22f;
    public float diveUpImpulse = 4f;

    [Header("Timing")]
    public float diveLockTime = 0.2f;
    public float minAirTime = 0.1f;
    public float landCrouchTime = 0.25f;
    public float diveCooldown = 1.2f;
    public float maxStandWaitTime = 1.5f;
    public float standRiseTime = 0.15f;

    [Header("Air Control")]
    public float diveAirControlMultiplier = 0.2f;

    [Header("Colliders")]
    public CapsuleCollider standingCollider;
    public CapsuleCollider divingCollider;

    [Header("Camera Tilt")]
    public float diveTiltX = 15f;
    public float diveTiltZ = 20f;

    [Header("Feel — FOV")]
    public float diveFovBoost = 12f;
    public float diveFovKickDuration = 0.1f;
    public float diveFovRecoverDuration = 0.4f;

    [Header("Feel — Launch Pitch")]
    public float divePitchKick = -6f;
    public float divePitchKickDuration = 0.1f;
    public float divePitchRecoverDuration = 0.35f;

    [Header("Feel — Landing")]
    public float landPitchKick = 5f;
    public float landFovDip = 5f;
    public float landKickDuration = 0.08f;
    public float landRecoverDuration = 0.3f;

    [Header("Feel — Landing Slide")]
    [Tooltip("How quickly horizontal momentum bleeds off on landing. 6 = long skid, 14 = short skid.")]
    public float diveGroundFriction = 10f;

    [Header("Dive Sound")]
    public AudioSource diveAudioSource;
    public AudioClip[] diveSoundClips;
    public float diveVolumeMin = 0.85f;
    public float diveVolumeMax = 1.0f;
    public float divePitchMin = 0.95f;
    public float divePitchMax = 1.05f;

    [Header("Cooldown UI")]
    [Tooltip("Assign your filled Image here. fillAmount 0 = on cooldown, 1 = ready.")]
    public Image cooldownImage;
    public GameObject coolDownImageBackGround;

    [Header("References")]
    public PlayerMovementTutorial movementScript;
    public Transform orientation;
    public PlayerCam cameraController;

    private Rigidbody _rb;
    private float _diveStartTime;
    public bool isDiving { get; private set; }
    private bool _canDive = true;

    private float _standingHalfH;
    private float _standingRadius;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _standingHalfH = standingCollider.height * 0.5f;
        _standingRadius = standingCollider.radius;

        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = 1f;
            SetCooldownImageAlpha(0f);
            coolDownImageBackGround.SetActive(false);
        }
    }

    private void Update()
    {
        if (!_canDive || isDiving) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool hasInput = new Vector2(h, v).magnitude > 0.1f;

        if (Input.GetKeyDown(KeyCode.LeftShift) && movementScript.GetGrounded() && hasInput)
            StartCoroutine(DiveRoutine());
    }

    private IEnumerator DiveRoutine()
    {
        _canDive = false;
        isDiving = true;
        _diveStartTime = Time.time;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = (orientation.forward * v + orientation.right * h).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = orientation.forward;

        SetDiveCollider(true);

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(dir * diveForwardImpulse, ForceMode.Impulse);
        _rb.AddForce(Vector3.up * diveUpImpulse, ForceMode.Impulse);

        PlayDiveSound();

        movementScript.diveAirControlMultiplier = diveAirControlMultiplier;

        float forwardSign = Vector3.Dot(dir, orientation.forward);
        float sideSign = Vector3.Dot(dir, orientation.right);

        if (cameraController != null)
        {
            cameraController.DoDiveTiltX(diveTiltX * forwardSign, 0.12f);
            cameraController.DoDiveTiltZ(-sideSign * diveTiltZ, 0.12f);
            cameraController.TriggerFovKick(cameraController.baseFov + diveFovBoost, diveFovKickDuration, diveFovRecoverDuration);
            cameraController.TriggerPitchKick(divePitchKick, divePitchKickDuration, divePitchRecoverDuration);
        }

        float lockEnd = Time.time + diveLockTime;
        while (Time.time < lockEnd)
            yield return null;

        while (!(movementScript.GetGrounded() && (Time.time - _diveStartTime) > minAirTime))
            yield return null;

        if (cameraController != null)
        {
            cameraController.TriggerFovKick(cameraController.baseFov - landFovDip, landKickDuration, landRecoverDuration);
            cameraController.TriggerPitchKick(landPitchKick, landKickDuration, landRecoverDuration);
            cameraController.DoDiveTiltX(8f, 0.08f);
            cameraController.DoDiveTiltZ(0f, 0.2f);
        }

        movementScript.diveAirControlMultiplier = 1f;

        float landTime = Time.time;
        float standWait = 0f;

        while (true)
        {
            Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            flatVel = Vector3.Lerp(flatVel, Vector3.zero, diveGroundFriction * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector3(flatVel.x, _rb.linearVelocity.y, flatVel.z);

            bool crouchDone = (Time.time - landTime) >= landCrouchTime;
            float currentTargetY = GetStandingTargetY();
            bool hasRoom = HasHeadroom(currentTargetY, _standingHalfH, _standingRadius);
            if (crouchDone && hasRoom) break;
            if (standWait >= maxStandWaitTime) break;
            standWait += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        float targetY = GetStandingTargetY();
        float startY = transform.position.y;
        float riseTime = 0f;

        while (riseTime < standRiseTime)
        {
            riseTime += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(riseTime / standRiseTime);
            float smoothT = t * t * (3f - 2f * t);
            float newY = Mathf.Lerp(startY, targetY, smoothT);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return new WaitForFixedUpdate();
        }

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        yield return new WaitForFixedUpdate();

        SetDiveCollider(false);
        yield return new WaitForFixedUpdate();

        if (cameraController != null)
        {
            cameraController.DoDiveTiltX(0f, 0.25f);
            cameraController.DoDiveTiltZ(0f, 0.25f);
        }

        isDiving = false;

        if (cooldownImage != null)
        {
            coolDownImageBackGround.SetActive(true);
            cooldownImage.fillAmount = 0f;
            SetCooldownImageAlpha(1f);
        }

        float elapsed = 0f;
        while (elapsed < diveCooldown)
        {
            elapsed += Time.deltaTime;
            if (cooldownImage != null)
                cooldownImage.fillAmount = Mathf.Clamp01(elapsed / diveCooldown);
            yield return null;
        }

        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = 1f;
            SetCooldownImageAlpha(0f);
        }

        _canDive = true;
        coolDownImageBackGround.SetActive(false);
    }

    private float GetStandingTargetY()
    {
        float groundY = transform.position.y
                        + divingCollider.center.y
                        - divingCollider.height * 0.5f;

        return groundY + _standingHalfH + standingCollider.center.y;
    }

    private bool HasHeadroom(float targetY, float halfHeight, float radius)
    {
        int mask = ~LayerMask.GetMask("Player");
        Vector3 topSphere = new Vector3(
            transform.position.x,
            targetY + halfHeight - radius,
            transform.position.z);
        return !Physics.CheckSphere(topSphere, radius - 0.05f, mask, QueryTriggerInteraction.Ignore);
    }

    private void SetDiveCollider(bool diving)
    {
        if (standingCollider) standingCollider.enabled = !diving;
        if (divingCollider) divingCollider.enabled = diving;
        Physics.SyncTransforms();
    }

    private void OnDisable()
    {
        SetDiveCollider(false);
    }

    private void PlayDiveSound()
    {
        if (diveAudioSource == null || diveSoundClips == null || diveSoundClips.Length == 0) return;

        AudioClip clip = diveSoundClips[Random.Range(0, diveSoundClips.Length)];
        diveAudioSource.volume = Random.Range(diveVolumeMin, diveVolumeMax);
        diveAudioSource.pitch = Random.Range(divePitchMin, divePitchMax);
        diveAudioSource.PlayOneShot(clip);
    }

    private void SetCooldownImageAlpha(float alpha)
    {
        Color c = cooldownImage.color;
        c.a = alpha;
        cooldownImage.color = c;
    }
}