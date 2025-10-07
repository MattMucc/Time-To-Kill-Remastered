using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 45f;
    public bool useWorldSpace = false;

    [Header("Bob Settings")]
    public bool enableBobbing = false;
    public float bobHeight = 0.25f;
    public float bobSpeed = 2f;
    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        float rotationAmount = rotationSpeed * Time.deltaTime;
        if (useWorldSpace)
            transform.Rotate(Vector3.up, rotationAmount, Space.World);
        else
            transform.Rotate(Vector3.up, rotationAmount, Space.Self);

        if (enableBobbing)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}