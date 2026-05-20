using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ejects a shell casing from the weapon using an object pool to avoid
/// per-shot Instantiate/Destroy allocations. The shell prefab must have
/// Rigidbody and BulletShellSound baked in.
/// </summary>
public class InstantiateUselessMag : MonoBehaviour
{
    [Header("Shell Prefab")]
    public GameObject shellPrefab;
    public Transform shellOutPosition;

    [Header("Shell Sound")]
    public AudioClip[] shellSoundClips;

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float shellLifetime = 5f;

    [Header("Ejection Force")]
    [SerializeField] private float upForceMin = 2f;
    [SerializeField] private float upForceMax = 3f;
    [SerializeField] private float forwardForceMin = 3f;
    [SerializeField] private float forwardForceMax = 5f;
    [SerializeField] private float rotationAngleMin = 65f;
    [SerializeField] private float rotationAngleMax = 115f;

    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private Transform _poolParent;

    private void Awake()
    {
        _poolParent = new GameObject($"{gameObject.name}_ShellPool").transform;
        _poolParent.SetParent(transform.root);

        for (int i = 0; i < poolSize; i++)
        {
            _pool.Enqueue(CreateShell());
        }
    }

    /// <summary>Ejects a shell casing from the weapon.</summary>
    public void InstantiatingBullet()
    {
        GameObject shell = _pool.Count > 0 ? _pool.Dequeue() : CreateShell();

        Quaternion rot = transform.parent.rotation
                         * Quaternion.AngleAxis(
                               Random.Range(rotationAngleMin, rotationAngleMax),
                               Vector3.forward);

        shell.transform.SetPositionAndRotation(shellOutPosition.position, rot);
        shell.SetActive(true);

        Rigidbody rb = shell.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(transform.up * Random.Range(upForceMin, upForceMax), ForceMode.Impulse);
        rb.AddForce(transform.forward * Random.Range(forwardForceMin, forwardForceMax), ForceMode.Impulse);

        BulletShellSound shellSound = shell.GetComponent<BulletShellSound>();
        shellSound.shellSoundClips = shellSoundClips;
        shellSound.ResetShell();

        StartCoroutine(ReturnToPool(shell, shellLifetime));
    }

    private GameObject CreateShell()
    {
        GameObject shell = Instantiate(shellPrefab, _poolParent);
        shell.SetActive(false);

        // Ensure Rigidbody settings are consistent without adding at runtime.
        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = shell.AddComponent<Rigidbody>();
        }

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.angularDamping = 1f;
        rb.linearDamping = 0.7f;

        if (shell.GetComponent<BulletShellSound>() == null)
        {
            shell.AddComponent<BulletShellSound>();
        }

        return shell;
    }

    private System.Collections.IEnumerator ReturnToPool(GameObject shell, float delay)
    {
        yield return new WaitForSeconds(delay);
        shell.SetActive(false);
        _pool.Enqueue(shell);
    }
}
