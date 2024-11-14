using UnityEngine;

public class BoardPlayerInfo : MonoBehaviour
{
    public int currentPosition;
    public bool movingForward;

    private void Start()
    {
        transform.position = GameManager.instance.currentPath.tiles[currentPosition].transform.position;
    }
}
