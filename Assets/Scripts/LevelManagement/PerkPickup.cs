using UnityEngine;

/// <summary>
/// Trigger placed on PerkObject inside a perk room prefab.
/// When the player enters it, opens the perk selection UI.
/// After a card is chosen, unlocks the exit door and loads the next batch of 6 rooms.
/// </summary>
public class PerkPickup : MonoBehaviour
{
    private bool taken = false;

    private void OnTriggerEnter(Collider other)
    {
        if (taken) return;
        if (!other.CompareTag("Player")) return;

        taken = true;

        Room room = GetComponentInParent<Room>();

        if (PerkSelectionUI.Instance != null)
        {
            PerkSelectionUI.Instance.Show(() => OnPerkChosen(room));
        }
        else
        {
            Debug.LogWarning("[PerkPickup] PerkSelectionUI not found — advancing without perk choice.");
            OnPerkChosen(room);
        }

        Destroy(gameObject);
    }

    private void OnPerkChosen(Room room)
    {
        room?.OnPerkTaken();
        RoomManager.Instance.LoadNextBatch();
    }
}
