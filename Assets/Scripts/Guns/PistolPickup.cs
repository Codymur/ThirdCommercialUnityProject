using UnityEngine;

public class PistolPickup : PickupItem
{
    [Header("Gun")]
    public GunController gunController;

    protected override void Start()
    {
        base.Start();   // handles rb cache + outline disable
        holdAnimationType = HoldAnimationType.Pistol;
        if (gunController != null) gunController.enabled = false;
    }

    protected override void OnPickedUpCallback()
    {
        if (gunController == null) return;
        gunController.enabled = true;
        gunController.NotifyPickedUp();
    }

    protected override void OnDroppedCallback()
    {
        if (gunController == null) return;
        gunController.LeftHanded = false;
        gunController.RightHanded = false;
        gunController.enabled = false;

    }
}