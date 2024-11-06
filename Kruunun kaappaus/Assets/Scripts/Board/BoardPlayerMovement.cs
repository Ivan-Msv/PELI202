using System.Collections;
using UnityEngine;

public class BoardPlayerMovement : MonoBehaviour
{
    // delete later
    [SerializeField] private BoardPlayerInfo player;

    private BoardPath currentPath;
    public bool alreadyMoving;
    [SerializeField] private float moveSpeed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentPath = GetComponent<BoardPath>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MovePlayer(player, 1);
        }
    }

    public void MovePlayer(BoardPlayerInfo player, int steps)
    {
        StartCoroutine(PlayerMovement(player, steps));
    }

    private IEnumerator PlayerMovement(BoardPlayerInfo player, int steps)
    {
        if (alreadyMoving)
        {
            yield break;
        }
        alreadyMoving = true;

        while (steps > 0)
        {
            var index = (player.currentPosition + 1) % currentPath.tiles.Count;
            Vector3 nextTilePosition = currentPath.tiles[index].transform.position;
            while (player.transform.position != nextTilePosition)
            {
                player.transform.position = Vector2.MoveTowards(player.transform.position, nextTilePosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
            player.currentPosition++;
            steps--;
        }

        currentPath.tiles[player.currentPosition].GetComponent<BoardTile>().InvokeTile();
        alreadyMoving = false;
    }
}
