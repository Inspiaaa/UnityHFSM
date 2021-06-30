using UnityEngine;

namespace FSM.Samples
{
    public class PlayerController : MonoBehaviour
    {
        private static PlayerController instance;

        public static PlayerController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PlayerController>();
                }

                return instance;
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
}