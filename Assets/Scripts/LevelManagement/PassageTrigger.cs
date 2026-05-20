using UnityEngine;

public class PassageTrigger : MonoBehaviour
{
    private Room room;
    private bool triggered = false;

    public GameObject PreviousLevelColliderBlocker;

    void Awake()
    {
        room = GetComponentInParent<Room>();
        PreviousLevelColliderBlocker.SetActive(false);
    }

    public void SetActiving(bool active)
    {
        GetComponent<Collider>().enabled = active;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        PreviousLevelColliderBlocker.SetActive(true);

        triggered = true;
        room.PlayerExited();
    }
}