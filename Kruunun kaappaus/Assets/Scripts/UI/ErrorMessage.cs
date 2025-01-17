using TMPro;
using UnityEngine;

public class ErrorMessage : MonoBehaviour
{
    public int lifeTimeSeconds;
    public string errorMessage;

    [SerializeField] private TextMeshProUGUI textObject;

    void Start()
    {
        Destroy(gameObject, lifeTimeSeconds);
        textObject.text = errorMessage;
    }
}
