using UnityEngine;

public class FirstLevelDestroyer : MonoBehaviour
{
    public GameObject FirstLevel;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(FirstLevel);
            Destroy(gameObject);
        }
    }
}
