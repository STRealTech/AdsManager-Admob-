using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public bool ShouldPersistOnLoad = false;


    private bool _isEnabled;
    public bool isEnabled
    {
        get { return _isEnabled; }
        set
        {
            _isEnabled = value;
            this.gameObject.SetActive(value);
        }
    }

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>(true);
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    instance = singletonObject.AddComponent<T>();
                }
            }

            if (instance is Singleton<T> singleton)
            {
                if (singleton.ShouldPersistOnLoad)
                {
                    DontDestroyOnLoad(singleton.gameObject);
                }
            }
            return instance;
        }
    }

    public virtual void Awake()
    {
        if (ShouldPersistOnLoad)
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public void EnableDisableScreen(bool state)
    {
        this.gameObject.SetActive(state);
    }
}