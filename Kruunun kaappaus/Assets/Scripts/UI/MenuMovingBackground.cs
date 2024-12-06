using UnityEngine;
using UnityEngine.UI;

public class MenuMovingBackground : MonoBehaviour
{
    [SerializeField] private RawImage image;
    [SerializeField] private float xDirectionSpeed, yDirectionSpeed;

    private void Update()
    {
        image.uvRect = new Rect(image.uvRect.position + new Vector2(xDirectionSpeed, yDirectionSpeed) * Time.deltaTime, image.uvRect.size);
    }
}
