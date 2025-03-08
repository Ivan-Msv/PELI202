using Unity.Netcode;
using UnityEngine;

public abstract class BoardTile : MonoBehaviour
{
    public Sprite tileSprite;
    public string tileName;
    public Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();

        SetupTile();
        GetComponent<SpriteRenderer>().sprite = tileSprite;
        name = tileName;
    }
    public abstract void SetupTile();
    public abstract void InvokeTile();
}
