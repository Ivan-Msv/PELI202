using UnityEngine;

public class CircleRotation : MonoBehaviour
{
    [SerializeField] private GameObject centerPoint;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private bool clockWise;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 axis = clockWise ? Vector3.back : Vector3.forward;
        transform.RotateAround(centerPoint.transform.position, axis, rotationSpeed * Time.deltaTime);
    }
}
