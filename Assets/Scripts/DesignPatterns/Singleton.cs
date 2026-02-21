using UnityEngine;

[DefaultExecutionOrder(-1000)]
public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    protected static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance) return _instance;
            try
            {
                _instance = FindFirstObjectByType<T>();

                if (!_instance) throw new UnassignedReferenceException("Input Manager is Unassiged");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                GameObject obj = new GameObject(typeof(T).Name);
                _instance = obj.AddComponent<T>();
                DontDestroyOnLoad(_instance);
            }

            return _instance;
        }
    }
    private void Awake()
    {
        if (!_instance)
        {
            _instance = this as T;
            DontDestroyOnLoad(_instance);
            return;
        }

        Destroy(gameObject);
    }
}
