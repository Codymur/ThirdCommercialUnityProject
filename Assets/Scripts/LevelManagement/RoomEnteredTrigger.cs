using UnityEngine;

/// <summary>
/// Placed at the entrance of each room. Destroys the previous room once the
/// player has physically stepped past the threshold, so the old room is never
/// deleted while the player is still standing in it.
/// </summary>
public class RoomEnteredTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RoomManager.Instance.DestroyPreviousRoom();
        GetComponent<Collider>().enabled = false;
    }
}
