using UnityEngine;

public class Sway : MonoBehaviour
{
    [Header("Sway Position")]
    public float amount = 0.02f;
    public float maxAmount = 0.06f;
    public float smoothAmount = 6f;

    [Header("Sway Rotation")]
    public float rotationAmount = 4f;
    public float maxRotationAmount = 5f;
    public float smoothRotation = 12f;
    public bool rotationX = true;
    public bool rotationY = true;
    public bool rotationZ = true;

    [Header("Idle Breathing")]
    public float idleSpeed = 0.5f;
    public float idlePosY = 0.012f;
    public float idlePosX = 0.006f;
    public float idleRotZ = 1.2f;
    public float idleRotX = 0.8f;

    [Header("Walk Breathing")]
    public float walkSpeed = 1.8f;
    public float walkPosY = 0.06f;
    public float walkPosX = 0.03f;
    public float walkRotZ = 8f;
    public float walkRotX = 5f;

    [Header("Breathing Blend")]
    public float blendSpeed = 3f;
    public float walkThreshold = 0.5f;
    public Rigidbody playerRb;

    [Header("References")]
    public PlayerDive diveScript;
    public float diveSuppressionSpeed = 8f; // How fast sway fades out/in during a dive

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float InputX;
    private float InputY;

    private float _timer;
    private float _blendT;
    private float _seedX, _seedY, _seedRX, _seedRZ;
    private float _swayMult = 1f; // smoothly suppressed to 0 while diving

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        _seedX = Random.Range(0f, 100f);
        _seedY = Random.Range(0f, 100f);
        _seedRX = Random.Range(0f, 100f);
        _seedRZ = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Fade sway out while diving so it doesn't fight the dive camera effects.
        bool isDiving = diveScript != null && diveScript.isDiving;
        float swayTarget = isDiving ? 0f : 1f;
        _swayMult = Mathf.Lerp(_swayMult, swayTarget, Time.deltaTime * diveSuppressionSpeed);

        CalculateSway();

        // --- Breathing blend ---
        float flatSpeed = 0f;
        if (playerRb != null)
        {
            Vector3 fv = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
            flatSpeed = fv.magnitude;
        }
        _blendT = Mathf.Lerp(_blendT, flatSpeed > walkThreshold ? 1f : 0f, Time.deltaTime * blendSpeed);

        float speed = Mathf.Lerp(idleSpeed, walkSpeed, _blendT);
        float posY = Mathf.Lerp(idlePosY, walkPosY, _blendT);
        float posX = Mathf.Lerp(idlePosX, walkPosX, _blendT);
        float rotZ = Mathf.Lerp(idleRotZ, walkRotZ, _blendT);
        float rotX = Mathf.Lerp(idleRotX, walkRotX, _blendT);

        _timer += Time.deltaTime * speed;

        float noiseY = (Mathf.PerlinNoise(_seedY, _timer) - 0.5f) * 2f;
        float noiseX = (Mathf.PerlinNoise(_seedX, _timer * 0.7f) - 0.5f) * 2f;
        float noiseRZ = (Mathf.PerlinNoise(_seedRZ, _timer * 0.9f) - 0.5f) * 2f;
        float noiseRX = (Mathf.PerlinNoise(_seedRX, _timer * 0.6f) - 0.5f) * 2f;

        // Breathing offsets
        Vector3 breathPos = new Vector3(noiseX * posX, noiseY * posY, 0f);
        Quaternion breathRot = Quaternion.Euler(noiseRX * rotX, 0f, noiseRZ * rotZ);

        ApplySway(breathPos, breathRot);
    }

    private void CalculateSway()
    {
        InputX = -Input.GetAxis("Mouse X");
        InputY = -Input.GetAxis("Mouse Y");
    }

    private void ApplySway(Vector3 breathPos, Quaternion breathRot)
    {
        // Position: sway target + breathing offset on top
        float moveX = Mathf.Clamp(InputX * amount, -maxAmount, maxAmount);
        float moveY = Mathf.Clamp(InputY * amount, -maxAmount, maxAmount);
        Vector3 swayPos = new Vector3(moveX, moveY, 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            initialPosition + (swayPos + breathPos) * _swayMult,
            Time.deltaTime * smoothAmount
        );

        // Rotation: sway target * breathing offset on top
        float tiltY = Mathf.Clamp(InputX * rotationAmount, -maxRotationAmount, maxRotationAmount);
        float tiltX = Mathf.Clamp(InputY * rotationAmount, -maxRotationAmount, maxRotationAmount);

        // Scale sway rotation by _swayMult — lerp toward identity when diving
        Quaternion swayRot = Quaternion.Slerp(
            Quaternion.identity,
            Quaternion.Euler(
                rotationX ? -tiltX : 0f,
                rotationY ? tiltY : 0f,
                rotationZ ? tiltY : 0f
            ),
            _swayMult
        );
        Quaternion breathRotScaled = Quaternion.Slerp(Quaternion.identity, breathRot, _swayMult);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            swayRot * initialRotation * breathRotScaled,
            Time.deltaTime * smoothRotation
        );
    }
}