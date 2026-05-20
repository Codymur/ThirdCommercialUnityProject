using UnityEngine;

/// <summary>
/// Drives the "WeaponPosition & Animations" Animator's RightHoldState and
/// LeftHoldState integer parameters based on what is currently held in each hand.
/// Attach this to the same GameObject as the Animator, or assign it via inspector.
/// </summary>
[RequireComponent(typeof(Animator))]
public class HandAnimationDriver : MonoBehaviour
{
    private static readonly int RightHoldStateHash = Animator.StringToHash("RightHoldState");
    private static readonly int LeftHoldStateHash  = Animator.StringToHash("LeftHoldState");

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>Sets the right-hand hold animation state.</summary>
    public void SetRightHand(HoldAnimationType type)
    {
        _animator.SetInteger(RightHoldStateHash, (int)type);
    }

    /// <summary>Sets the left-hand hold animation state.</summary>
    public void SetLeftHand(HoldAnimationType type)
    {
        _animator.SetInteger(LeftHoldStateHash, (int)type);
    }
}
