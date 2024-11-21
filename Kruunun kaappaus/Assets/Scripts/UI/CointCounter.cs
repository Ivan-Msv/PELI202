using TMPro;
using UnityEngine;

public class CointCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        coinText.text = $"Coins: {GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInfo2D>().coinAmount.Value}";
    }

    // Update is called once per frame
    void Update()
    {
        coinText.text = $"Coins: {GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInfo2D>().coinAmount.Value}";
    }
}
