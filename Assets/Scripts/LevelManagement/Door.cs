using UnityEngine;

[RequireComponent(typeof(HingeJoint))]
[RequireComponent(typeof(Rigidbody))]
public class Door : MonoBehaviour
{
    [Header("Trigger")]
    public PassageTrigger passageTrigger;

    private Rigidbody rb;
    private bool isLocked = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // frozen until unlocked

        if (passageTrigger != null)
            passageTrigger.SetActiving(false);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    public void Open()
    {
        if (this == null || rb == null) return;
        isLocked = false;
        rb.isKinematic = false;
        Invoke(nameof(EnablePassage), 0.5f);
    }

    void EnablePassage()
    {
        if (passageTrigger != null)
            passageTrigger.SetActiving(true);
    }
}