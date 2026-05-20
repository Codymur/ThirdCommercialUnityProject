using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunBreath : MonoBehaviour
{

    [Header("— Assign In Inspector —")]
    public Rigidbody playerRb;

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

    [Header("Blend")]
    public float blendSpeed = 3f;
    public float walkThreshold = 0.5f;

    // Different noise seeds per axis so they never mirror each other
    private float _seedX;
    private float _seedY;
    private float _seedZ;
    private float _seedRX;
    private float _seedRZ;

    private Vector3 _originPos;
    private Quaternion _originRot;
    private float _timer;
    private float _blendT;

    void Start()
    {
        _originPos = transform.localPosition;
        _originRot = transform.localRotation;

        // Random offsets so two guns held together never sync perfectly
        _seedX = Random.Range(0f, 100f);
        _seedY = Random.Range(0f, 100f);
        _seedZ = Random.Range(0f, 100f);
        _seedRX = Random.Range(0f, 100f);
        _seedRZ = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Velocity check
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

        // Perlin noise returns 0..1, remap to -1..1
        float noiseY = (Mathf.PerlinNoise(_seedY, _timer) - 0.5f) * 2f;
        float noiseX = (Mathf.PerlinNoise(_seedX, _timer * 0.7f) - 0.5f) * 2f;
        float noiseRZ = (Mathf.PerlinNoise(_seedRZ, _timer * 0.9f) - 0.5f) * 2f;
        float noiseRX = (Mathf.PerlinNoise(_seedRX, _timer * 0.6f) - 0.5f) * 2f;

        transform.localPosition = _originPos + new Vector3(noiseX * posX, noiseY * posY, 0f);
        transform.localRotation = _originRot * Quaternion.Euler(noiseRX * rotX, 0f, noiseRZ * rotZ);
    }

}
