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

        if (GameManager.instance != null)
        {
            vfxPrefab = GameManager.instance.emptyTile.GetComponent<EmptyTile>().vfxPrefab;
        }
    }
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.emptyTile.tileSprite;
        tileName = GameManager.instance.emptyTile.name;

        // On scene switch it might have crowntile from the start even though it should be empty
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("EmptyTile_Switch"))
        {
            anim.Play("EmptyTile_Idle");
        }
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
