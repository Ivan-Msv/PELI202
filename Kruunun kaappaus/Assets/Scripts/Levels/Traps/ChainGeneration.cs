using UnityEngine;

public class ChainGeneration : MonoBehaviour
{
    [SerializeField] private GameObject startPoint;
    [SerializeField] private GameObject endPoint;
    void Start()
    {
        GenerateChain();
    }


    private void GenerateChain()
    {
        Vector2 newPosition = (startPoint.transform.position + endPoint.transform.position) / 2;
        Vector2 direction = (startPoint.transform.position - endPoint.transform.position).normalized;
        Quaternion newRotation = Quaternion.FromToRotation(Vector3.up, direction);
        transform.SetPositionAndRotation(newPosition, newRotation);
        float distance = Vector2.Distance(startPoint.transform.position, endPoint.transform.position);
        
        GetComponent<SpriteRenderer>().size += new Vector2(0, distance);
    }
}
