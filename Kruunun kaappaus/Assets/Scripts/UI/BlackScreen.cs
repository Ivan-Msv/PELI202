using UnityEngine;

public class BlackScreen : MonoBehaviour
{
    public static BlackScreen instance;
    public Fade screenFade;
    private void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(transform.parent);
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
