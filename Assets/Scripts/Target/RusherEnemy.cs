using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Rusher Enemy
/// — Charges directly at the player on detection
/// — No shooting — pure melee threat
/// — Tankier than ShooterEnemy
/// — Kick, throw, dive impact all kill it via TakeDamage()
/// </summary>
public class RusherEnemy : EnemyBase
{
    [Header("Rusher Settings")]
    public float chargeSpeed = 6f;
    public float meleeRange = 1.8f;
    public float meleeDamage = 20f;
    public float meleeCooldown = 1.2f;
    public float chargeWindup = 0.6f;  // Brief pause before full charge — readable tell

    // ?? Internal ???????????????????????????????????????????????????
    float meleeTimer = 0f;
    float windupTimer = 0f;
    bool isWindingUp = false;

    // ??????????????????????????????????????????????????????????????
    protected override void Start()
    {
        base.Start();

        // Rushers are tankier — override default Target health
        health = 30f;
        agent.speed = chargeSpeed;
        agent.angularSpeed = 360f;           // Turns fast — hard to juke
        agent.stoppingDistance = meleeRange * 0.8f;
    }

    // ??????????????????????????????????????????????????????????????
    protected override void Update()
    {
        base.Update(); // handles Idle ? Alert ? Attack transitions
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case EnemyState.Idle:
                agent.ResetPath();
                break;

            case EnemyState.Alert:
                // Brief windup before first charge — visual tell for the player
                if (!isWindingUp)
                {
                    isWindingUp = true;
                    windupTimer = chargeWindup;
                    agent.ResetPath(); // Stand still during windup
                }

                windupTimer -= Time.deltaTime;
                if (windupTimer <= 0f)
                {
                    isWindingUp = false;
                    state = EnemyState.Attack;
                }
                break;

            case EnemyState.Attack:
                HandleCharge(dist);
                HandleMelee(dist);
                break;
        }
    }

    // ??????????????????????????????????????????????????????????????
    void HandleCharge(float dist)
    {
        // Always charge directly at player — no strafing, no backing up
        // This makes it readable and gives the player a fair chance to dive/kick
        agent.SetDestination(player.position);

        // Face player
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * 12f
            );
        }
    }

    // ??????????????????????????????????????????????????????????????
    void HandleMelee(float dist)
    {
        meleeTimer -= Time.deltaTime;
        if (meleeTimer > 0f) return;
        if (dist > meleeRange) return;

        // Hit player
        Target playerTarget = player.GetComponent<Target>();
        if (playerTarget != null)
            playerTarget.TakeDamage(meleeDamage);

        // Slight knockback on the player's Rigidbody
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 knockDir = (player.position - transform.position).normalized + Vector3.up * 0.3f;
            playerRb.AddForce(knockDir * 8f, ForceMode.Impulse);
        }

        meleeTimer = meleeCooldown;
    }
}