using UnityEngine;

public class PlayerMainChecking : MonoBehaviour
{
    public static Transform Instance; // Static reference is extremely fast to access

    void Awake()
    {
        Instance = this.transform;
    }
}
