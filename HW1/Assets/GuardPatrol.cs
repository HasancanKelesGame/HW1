using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GuardPatrol : MonoBehaviour
{
    [Header("Patrol Path (Coroutine)")]
    public Transform[] waypoints;
    public float moveSpeed = 2.2f;
    public float turnSpeed = 10f;
    public float reachDistance = 0.1f;
    public float waitAtPoint = 0.25f;
    public bool pingPong = true;

    [Header("Detection")]
    public Transform playerRoot;
    public string playerTag = "Player";
    public float detectDistance = 2.7f;
    [Range(1f, 179f)] public float detectAngle = 70f;
    public float eyeHeight = 1.6f;
    public bool requireLineOfSight = true;
    public LayerMask obstacleMask = ~0;

    [Header("Fail")]
    public float failDelay = 0.4f;
    public bool askForAnotherRoundOnFail = true;
    public DeathRoundPromptUI deathRoundPromptUI;
    public bool reloadSceneOnFail = true;

    [Header("Debug")]
    public bool drawDebug = true;
    public bool logDetection = true;

    private Coroutine _patrolRoutine;
    private bool _isFailing;
    private int _index;
    private int _direction = 1;

    private void OnEnable()
    {
        FindPlayerIfNeeded();
        if (_patrolRoutine == null)
        {
            _patrolRoutine = StartCoroutine(PatrolRoutine());
        }
    }

    private void OnDisable()
    {
        if (_patrolRoutine != null)
        {
            StopCoroutine(_patrolRoutine);
            _patrolRoutine = null;
        }
    }

    private void Update()
    {
        if (_isFailing)
        {
            return;
        }

        FindPlayerIfNeeded();
        if (playerRoot == null)
        {
            return;
        }

        if (IsPlayerInDangerZone())
        {
            if (logDetection)
            {
                Debug.Log("[GuardPatrol] Player detected by " + name + ".", this);
            }

            StartCoroutine(FailPlayer());
        }
    }

    private IEnumerator PatrolRoutine()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            yield break;
        }

        if (waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
        }

        _index = waypoints.Length > 1 ? 1 : 0;

        while (true)
        {
            Transform target = waypoints[_index];
            if (target == null)
            {
                AdvanceIndex();
                yield return null;
                continue;
            }

            while (Vector3.Distance(transform.position, target.position) > reachDistance)
            {
                Vector3 toTarget = target.position - transform.position;
                Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);

                if (flat.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
                yield return null;
            }

            if (waitAtPoint > 0f)
            {
                yield return new WaitForSeconds(waitAtPoint);
            }

            AdvanceIndex();
        }
    }

    private void AdvanceIndex()
    {
        if (waypoints == null || waypoints.Length <= 1)
        {
            _index = 0;
            return;
        }

        if (!pingPong)
        {
            _index = (_index + 1) % waypoints.Length;
            return;
        }

        _index += _direction;
        if (_index >= waypoints.Length)
        {
            _direction = -1;
            _index = waypoints.Length - 2;
        }
        else if (_index < 0)
        {
            _direction = 1;
            _index = 1;
        }
    }

    private bool IsPlayerInDangerZone()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = playerRoot.position - eye;

        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
        float distance = flatToPlayer.magnitude;
        if (distance > detectDistance)
        {
            return false;
        }

        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        if (flatToPlayer.sqrMagnitude > 0.0001f)
        {
            float angle = Vector3.Angle(flatForward, flatToPlayer.normalized);
            if (angle > detectAngle * 0.5f)
            {
                return false;
            }
        }

        if (!requireLineOfSight)
        {
            return true;
        }

        return HasLineOfSightToPlayer(eye, toPlayer);
    }

    private IEnumerator FailPlayer()
    {
        _isFailing = true;

        if (playerRoot != null)
        {
            PlayerMovement movement = playerRoot.GetComponentInChildren<PlayerMovement>();
            if (movement == null)
            {
                movement = FindObjectOfType<PlayerMovement>();
            }
            if (movement != null)
            {
                movement.enabled = false;
            }

            CharacterController controller = playerRoot.GetComponentInChildren<CharacterController>();
            if (controller == null)
            {
                controller = FindObjectOfType<CharacterController>();
            }
            if (controller != null)
            {
                controller.enabled = false;
            }
        }

        if (failDelay > 0f)
        {
            yield return new WaitForSeconds(failDelay);
        }

        if (askForAnotherRoundOnFail)
        {
            if (deathRoundPromptUI == null)
            {
                deathRoundPromptUI = FindObjectOfType<DeathRoundPromptUI>(true);
            }

            if (deathRoundPromptUI != null)
            {
                deathRoundPromptUI.Show();
                yield break;
            }

            Debug.LogWarning("[GuardPatrol] Death prompt UI was not found, falling back to scene reload.", this);
        }

        if (reloadSceneOnFail || askForAnotherRoundOnFail)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield break;
        }

        _isFailing = false;
    }

    private void FindPlayerIfNeeded()
    {
        if (playerRoot != null && playerRoot.gameObject.activeInHierarchy)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject tagged = GameObject.FindWithTag(playerTag);
            if (tagged != null)
            {
                playerRoot = tagged.transform;
                return;
            }
        }

        PlayerMovement movement = FindObjectOfType<PlayerMovement>();
        if (movement != null)
        {
            playerRoot = movement.transform;
        }
    }

    private bool HasLineOfSightToPlayer(Vector3 eye, Vector3 toPlayer)
    {
        Vector3 direction = toPlayer.normalized;
        float rayLength = Mathf.Max(0.01f, toPlayer.magnitude);

        RaycastHit[] hits = Physics.RaycastAll(eye, direction, rayLength, obstacleMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        int bestIndex = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null)
            {
                continue;
            }

            // Ignore the guard's own colliders.
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            return false;
        }

        return IsPlayerTransform(hits[bestIndex].transform);
    }

    private bool IsPlayerTransform(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (playerRoot != null)
        {
            if (target == playerRoot || target.IsChildOf(playerRoot) || playerRoot.IsChildOf(target))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            Transform current = target;
            while (current != null)
            {
                if (current.CompareTag(playerTag))
                {
                    return true;
                }

                current = current.parent;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(waypoints[i].position, 0.12f);
                if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
            }
        }

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 left = Quaternion.Euler(0f, -detectAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, detectAngle * 0.5f, 0f) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(eye, left.normalized * detectDistance);
        Gizmos.DrawRay(eye, right.normalized * detectDistance);
        Gizmos.DrawWireSphere(eye, detectDistance);
    }
}
