using UnityEngine;

public class BulletShellSound : MonoBehaviour
{
    public AudioClip[] shellSoundClips;
    public float volumeMin = 0.4f;
    public float volumeMax = 0.7f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.2f;

    private bool hasPlayed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPlayed) return;
        if (shellSoundClips == null || shellSoundClips.Length == 0) return;

        hasPlayed = true;

        AudioClip clip = shellSoundClips[Random.Range(0, shellSoundClips.Length)];
        AudioSource.PlayClipAtPoint(
            clip,
            transform.position,
            Random.Range(volumeMin, volumeMax)
        );
    }

    /// <summary>Resets state so a pooled shell can be reused.</summary>
    public void ResetShell()
    {
        hasPlayed = false;
    }
}
