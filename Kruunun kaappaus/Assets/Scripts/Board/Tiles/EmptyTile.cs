using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EmptyTile : BoardTile
{
    [SerializeField] private GameObject vfxPrefab;
    private Animator anim;
    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.StopPlayback();

        anim.runtimeAnimatorController = GameManager.instance.emptyTile.GetComponent<Animator>().runtimeAnimatorController;
        vfxPrefab = GameManager.instance.emptyTile.GetComponent<EmptyTile>().vfxPrefab;
    }
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.emptyTile.tileSprite;
        tileName = GameManager.instance.emptyTile.name;
    }
    public override void InvokeTile()
    {
        Debug.Log("Empty Tile Invoked");
    }

    public void AnimationVFXTrigger()
    {
        var vfxObject = Instantiate(vfxPrefab, this.transform);
        var component = vfxObject.GetComponent<Animator>();
        component.Play("EmptyTile_Switch_VFX");
    }
}
