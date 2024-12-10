using Unity.Cinemachine;
using UnityEngine;

public class BoardCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;

    public void SetCameraPosition()
    {
        virtualCamera.transform.position = GameManager.instance.currentPlayer.transform.position;
    }

    public void SetCustomPosition(Vector2 newPosition)
    {
        virtualCamera.transform.position = newPosition;
    }

    private void Update()
    {
        if (GameManager.instance.currentPlayer == null)
        {
            return;
        }
        virtualCamera.Follow = GameManager.instance.currentPlayer.transform;
    }
}
