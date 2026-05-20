/// <summary>
/// Defines which hand-pose animation an item requires when held.
/// The integer values must match the RightHoldState / LeftHoldState
/// animator parameter values set up in "WeaponPosition & Animations.controller".
/// 0 = Idle (nothing held), 1 = Pistol, 2 = Cup
/// </summary>
public enum HoldAnimationType
{
    None   = 0,
    Pistol = 1,
    Cup    = 2,
}
