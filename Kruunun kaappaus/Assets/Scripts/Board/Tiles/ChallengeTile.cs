using Unity.VisualScripting;
using UnityEngine;

public class ChallengeTile : BoardTile
{
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.challengeTile.tileSprite;
        tileName = GameManager.instance.challengeTile.name;
    }
    public override void InvokeTile()
    {
        Debug.Log("Challenge tile invoked");
        ReplaceWithEmptyTile();
    }

    private void ReplaceWithEmptyTile()
    {
        this.AddComponent<EmptyTile>();
        Destroy(this);
    }
}
