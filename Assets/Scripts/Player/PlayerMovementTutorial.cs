using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovementTutorial : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [HideInInspector] public float walkSpeed;
    [HideInInspector] public float sprintSpeed;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;
    public PlayerCam CameraControllerScript;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    // Add this field to PlayerMovementTutorial
    public float diveAirControlMultiplier = 1f;


    public bool GetGrounded() => grounded;


    [Header("Footsteps")]
    public AudioSource footstepAudioSource;
    public AudioClip[] footstepClips;
    public float footstepInterval = 0.4f;
    public float footstepVolumeMin = 0.8f;
    public float footstepVolumeMax = 1.0f;
    public float footstepPitchMin = 0.9f;
    public float footstepPitchMax = 1.1f;

    private float footstepTimer = 0f;
    private int lastFootstepIndex = -1;


    [Header("Jump & Land Sounds")]
    public AudioSource jumpLandAudioSource;
    public AudioClip[] jumpSoundClips;
    public AudioClip[] landSoundClips;
    public float jumpVolumeMin = 0.8f;
    public float jumpVolumeMax = 1.0f;
    public float landVolumeMin = 0.8f;
    public float landVolumeMax = 1.0f;

    private bool wasGrounded;


    public float minLandingVelocity = 7f;
    public float maxLandingVelocity = 12f; // at this speed, volume is at max
    public float minFallDistanceForLandSound = 1.5f; // tune this in Inspector

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;


        wasGrounded = true;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        MyInput();
        SpeedControl();
        HandleLandSound();

        // handle drag
        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;

        HandleFootsteps();

        wasGrounded = grounded;


    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on ground
        if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * diveAirControlMultiplier, ForceMode.Force);

        // in air
        else if(!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier * diveAirControlMultiplier, ForceMode.Force);





        //bu elave
        if (horizontalInput > 0)
        {
            CameraControllerScript.DoTilt(-7f);

        }
        else if (horizontalInput < 0)
        {

            CameraControllerScript.DoTilt(7f);
        }
        else if (horizontalInput == 0)
        {
            CameraControllerScript.DoTilt(0f);
        }


        if (verticalInput > 0)
        {

            CameraControllerScript.DoTiltX(7f);
        }
        else if (verticalInput < 0)
        {

            CameraControllerScript.DoTiltX(-7f);
        }
        else if (horizontalInput == 0)
        {

            CameraControllerScript.DoTiltX(0f);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // limit velocity if needed
        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        PlayJumpSound();
    }
    private void ResetJump()
    {
        readyToJump = true;
    }



    private void HandleFootsteps()
    {
        bool isMoving = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude > 0.5f;

        if (grounded && isMoving)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                PlayFootstep();
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            // Reset so first step plays immediately when you start moving again
            footstepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        // Pick a random clip, never repeating the same one twice
        int index;
        do
        {
            index = Random.Range(0, footstepClips.Length);
        } while (index == lastFootstepIndex && footstepClips.Length > 1);

        lastFootstepIndex = index;

        footstepAudioSource.volume = Random.Range(footstepVolumeMin, footstepVolumeMax);
        footstepAudioSource.pitch = Random.Range(footstepPitchMin, footstepPitchMax);
        footstepAudioSource.PlayOneShot(footstepClips[index]);
    }


    private void HandleLandSound()
    {
        if (!wasGrounded && grounded)
        {
            float impactVelocity = -rb.linearVelocity.y;
            if (impactVelocity >= minLandingVelocity)
            {
                float t = Mathf.InverseLerp(minLandingVelocity, maxLandingVelocity, impactVelocity);
                jumpLandAudioSource.volume = Mathf.Lerp(landVolumeMin, landVolumeMax, t);
                PlayLandSound();
            }
        }
    }

    private void PlayJumpSound()
    {
        if (jumpSoundClips == null || jumpSoundClips.Length == 0) return;

        AudioClip clip = jumpSoundClips[Random.Range(0, jumpSoundClips.Length)];
        jumpLandAudioSource.volume = Random.Range(jumpVolumeMin, jumpVolumeMax);
        jumpLandAudioSource.PlayOneShot(clip);
    }

    private void PlayLandSound()
    {
        if (landSoundClips == null || landSoundClips.Length == 0) return;

        AudioClip clip = landSoundClips[Random.Range(0, landSoundClips.Length)];
        jumpLandAudioSource.PlayOneShot(clip);
    }

}