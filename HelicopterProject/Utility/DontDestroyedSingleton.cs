using UnityEngine;

public class DontDestroyedSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    static T m_instance;

    public static T Instance
    {
        get
        {
            if (m_instance == null)
            {
                var instance = FindFirstObjectByType<T>();

                if (instance == null)
                {
                    var singleton = new GameObject(typeof(T).Name);
                    instance = singleton.AddComponent<T>();
                }

                return instance;
            }

            return m_instance;
        }
    }

    public virtual void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this as T;

            transform.parent = null;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

