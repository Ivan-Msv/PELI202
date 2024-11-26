using Unity.Netcode;
using UnityEngine;

public abstract class BoardTile : MonoBehaviour
{
    public Sprite tileSprite;
    public string tileName;

    private void Start()
    {
        SetupTile();
        GetComponent<SpriteRenderer>().sprite = tileSprite;
        name = tileName;
        // ja pelaa animaatiota tässä ehkä
    }
    public abstract void SetupTile();
    public abstract void InvokeTile();
}
