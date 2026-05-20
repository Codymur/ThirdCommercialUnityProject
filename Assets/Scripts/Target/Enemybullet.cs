using UnityEngine;

/// <summary>
/// Attach to the ShooterEnemy bullet prefab.
/// Prefab setup: Sphere mesh + Rigidbody (gravity off) + SphereCollider + this script.
/// ShooterEnemy already sets velocity on spawn — this handles damage and cleanup.
/// </summary>
public class Enemybullet : MonoBehaviour
{
    [Header("Stats")]
    public float damage = 10f;
    public float lifetime = 4f;        // Auto-destroy if nothing is hit

    [Header("Impact")]
    public GameObject impactEffectPrefab; // Optional — assign a small particle prefab

    private void Start()
    {
        // Ignore gravity so the bullet travels in a straight line
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.useGravity = false;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Spawn impact effect if assigned
        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, collision.contacts[0].point, Quaternion.identity);

        // Try to damage the player (or anything with a Target component)
        Target target = collision.collider.GetComponentInParent<Target>();
        if (target != null)
        {
            Vector3 hitDirection = transform.forward;
            target.TakeDamage(damage, hitDirection);
        }

        Destroy(gameObject);
    }
}