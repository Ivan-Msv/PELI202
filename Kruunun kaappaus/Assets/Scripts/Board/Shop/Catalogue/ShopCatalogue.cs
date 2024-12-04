using TMPro;
using UnityEngine;

public class ShopCatalogue : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI catalogueName;

    private void Start()
    {
        catalogueName.text = this.name;
    }
}
