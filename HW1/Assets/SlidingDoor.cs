using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public Transform leftDoor;
    public Transform rightDoor;

    public float slideDistance = 2f;
    public float speed = 3f;

    Vector3 leftClosed;
    Vector3 rightClosed;

    Vector3 leftOpen;
    Vector3 rightOpen;

    bool open = false;

    void Start()
    {
        // store starting positions
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;

        // REVERSED directions (fix)
        leftOpen = leftClosed + Vector3.right * slideDistance;
        rightOpen = rightClosed + Vector3.left * slideDistance;
    }

    void Update()
    {
        if (open)
        {
            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftOpen, Time.deltaTime * speed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightOpen, Time.deltaTime * speed);
        }
        else
        {
            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftClosed, Time.deltaTime * speed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightClosed, Time.deltaTime * speed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            open = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            open = false;
        }
    }
}