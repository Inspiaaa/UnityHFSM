using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    PlayerController()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Awake()
    {
        if (this != Instance)
        {
            Destroy(gameObject);
        }
    }
}