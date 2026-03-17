using UnityEngine;
using StarterAssets;

public class SlidingDoor : MonoBehaviour
{
    [Header("Door Parts")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Motion")]
    public float slideDistance = 2f;
    public float speed = 3f;
    public float openReachedThreshold = 0.03f;

    [Header("Win Trigger")]
    public string boxTag = "Box";
    public DeathRoundPromptUI roundPromptUI;
    public bool freezePlayerOnWin = true;

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private Vector3 leftOpen;
    private Vector3 rightOpen;

    private bool open;
    private bool winHandled;

    void Start()
    {
        // Store starting positions.
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;

        // Open directions.
        leftOpen = leftClosed + Vector3.right * slideDistance;
        rightOpen = rightClosed + Vector3.left * slideDistance;
    }

    void Update()
    {
        if (!open)
        {
            return;
        }

        leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftOpen, Time.deltaTime * speed);
        rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightOpen, Time.deltaTime * speed);

        if (!winHandled && IsDoorFullyOpen())
        {
            HandleWin();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryOpenByBox(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        TryOpenByBox(collision.collider);
    }

    private void TryOpenByBox(Collider other)
    {
        if (open || other == null)
        {
            return;
        }

        if (!IsBoxCollider(other))
        {
            return;
        }

        open = true;
    }

    private bool IsBoxCollider(Collider other)
    {
        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag(boxTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsDoorFullyOpen()
    {
        float leftDistance = Vector3.Distance(leftDoor.localPosition, leftOpen);
        float rightDistance = Vector3.Distance(rightDoor.localPosition, rightOpen);
        return leftDistance <= openReachedThreshold && rightDistance <= openReachedThreshold;
    }

    private void HandleWin()
    {
        winHandled = true;

        if (freezePlayerOnWin)
        {
            FreezePlayerControls();
        }

        if (roundPromptUI == null)
        {
            roundPromptUI = FindObjectOfType<DeathRoundPromptUI>(true);
        }

        if (roundPromptUI != null)
        {
            roundPromptUI.Show();
        }
        else
        {
            Debug.LogWarning("[SlidingDoor] Win prompt UI is missing.", this);
        }
    }

    private static void FreezePlayerControls()
    {
        PlayerMovement movement = Object.FindObjectOfType<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        CharacterController controller = Object.FindObjectOfType<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
    }
}
