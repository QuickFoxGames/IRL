using UnityEngine;
namespace MGUtilities
{
    public abstract class Singleton_template<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        static T s_instance;

        public static T Instance()
        {
            if (s_instance == null)
            {
                s_instance = FindFirstObjectByType<T>();

                if (s_instance == null)
                {
                    new GameObject("Singleton", typeof(T));
                }
            }

            return s_instance;
        }

        protected virtual void Awake()
        {
            if (s_instance != null && FindObjectsByType<T>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}