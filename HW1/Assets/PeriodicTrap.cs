using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PeriodicTrap : MonoBehaviour
{
    [Header("Cycle")]
    public bool startActive = false;
    public float startDelay = 0f;
    public float activeTime = 1.5f;
    public float inactiveTime = 1.5f;
    public float moveDuration = 0.35f;

    [Header("Movement")]
    public Transform movingPart;
    public Vector3 activeOffset = new Vector3(0f, 1f, 0f);

    [Header("Animation (Optional)")]
    public bool useAnimatorMotion = true;
    public Animator trapAnimator;
    public string activeTrigger = "close";
    public string inactiveTrigger = "open";
    public bool forceAnimatePhysics = true;

    [Header("Activation Targets")]
    public Collider damageTrigger;
    public Collider[] collidersToToggle;
    public GameObject[] visualsToToggle;
    public bool autoPopulateCollidersToToggle = true;
    public bool autoCreateBodyColliders = true;
    public string spearNameKeyword = "Spear";
    public PhysicMaterial spearPhysicMaterial;

    [Header("Hit Detection")]
    public bool useDamageZoneCheck = false;
    public bool usePhysicsPenetrationCheck = true;
    public Transform playerRoot;
    public string playerTag = "Player";
    [Range(0f, 0.5f)] public float activeBoundsPadding = 0.1f;

    [Header("Death")]
    public float deathDelay = 0.8f;
    public bool askForAnotherRoundOnDeath = true;
    public DeathRoundPromptUI deathRoundPromptUI;
    public bool reloadSceneOnDeath = true;
    public Transform respawnPoint;

    [Header("Debug")]
    public bool debugCollision = false;
    [Range(1, 120)] public int debugLogEveryNFrames = 20;
    public bool drawDebugBounds = true;

    private bool _isActive;
    private bool _playerCaught;
    private Coroutine _cycleRoutine;
    private Vector3 _inactiveLocalPosition;
    private Vector3 _activeLocalPosition;
    private GameObject _playerObject;
    private CharacterController _playerController;
    private Collider[] _playerColliders;
    private int _debugFrameCounter;

    private void Reset()
    {
        movingPart = transform;
        damageTrigger = GetComponent<Collider>();
    }

    private void Awake()
    {
        if (movingPart == null)
        {
            movingPart = transform;
        }

        if (trapAnimator == null)
        {
            trapAnimator = GetComponent<Animator>();
        }

        if (trapAnimator != null && useAnimatorMotion && forceAnimatePhysics)
        {
            trapAnimator.updateMode = AnimatorUpdateMode.AnimatePhysics;
        }

        EnsureSpearBodyColliders();
        AutoPopulateCollidersIfNeeded();

        // Spears should always be physical colliders while enabled.
        if (collidersToToggle != null)
        {
            for (int i = 0; i < collidersToToggle.Length; i++)
            {
                if (collidersToToggle[i] != null)
                {
                    collidersToToggle[i].isTrigger = false;
                }
            }
        }

        ValidateSpearCollisionSetup();

        DebugLog("Awake complete. CollidersToToggle count: " +
                 (collidersToToggle == null ? 0 : collidersToToggle.Length));

        CachePlayerReferences();

        _inactiveLocalPosition = movingPart.localPosition;
        _activeLocalPosition = _inactiveLocalPosition + activeOffset;

        if (!UsesAnimatorMotion())
        {
            movingPart.localPosition = startActive ? _activeLocalPosition : _inactiveLocalPosition;
        }

        SetActiveStateImmediate(startActive);
        if (UsesAnimatorMotion())
        {
            TriggerAnimatorState(startActive);
        }
    }

    private void OnEnable()
    {
        _cycleRoutine = StartCoroutine(TrapCycle());
    }

    private void OnDisable()
    {
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
    }

    private void LateUpdate()
    {
        EnsurePlayerReferences();
        Physics.SyncTransforms();

        if (_playerCaught)
        {
            return;
        }

        bool hit = false;
        string reason = string.Empty;

        if (usePhysicsPenetrationCheck && IsPlayerTouchingTrapColliders(out string trapColliderName))
        {
            hit = true;
            reason = "ComputePenetration:" + trapColliderName;
        }
        else if (useDamageZoneCheck && damageTrigger != null && damageTrigger.enabled)
        {
            hit = IsPlayerInsideBounds(damageTrigger.bounds);
            reason = "DamageTriggerBounds";
        }
        else if (TryGetActiveTrapBounds(out Bounds trapBounds))
        {
            trapBounds.Expand(activeBoundsPadding * 2f);
            hit = IsPlayerInsideBounds(trapBounds);
            reason = "TrapBounds";
        }

        if (hit)
        {
            TryKillCachedPlayer(reason);
            return;
        }

        if (debugCollision)
        {
            _debugFrameCounter++;
            if (_debugFrameCounter % Mathf.Max(1, debugLogEveryNFrames) == 0)
            {
                DebugLog("Collision check: no hit this frame.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryKillPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryKillPlayer(other);
    }

    private IEnumerator TrapCycle()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        while (true)
        {
            float wait = _isActive ? activeTime : inactiveTime;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            yield return MoveAndSetState(!_isActive);
        }
    }

    private IEnumerator MoveAndSetState(bool makeActive)
    {
        if (UsesAnimatorMotion())
        {
            if (makeActive)
            {
                SetActiveStateImmediate(true);
            }

            TriggerAnimatorState(makeActive);

            if (moveDuration > 0f)
            {
                yield return new WaitForSeconds(moveDuration);
            }

            if (!makeActive)
            {
                SetActiveStateImmediate(false);
            }

            yield break;
        }

        Vector3 start = movingPart.localPosition;
        Vector3 target = makeActive ? _activeLocalPosition : _inactiveLocalPosition;

        if (moveDuration <= 0f)
        {
            movingPart.localPosition = target;
            SetActiveStateImmediate(makeActive);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            movingPart.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }

        movingPart.localPosition = target;
        SetActiveStateImmediate(makeActive);
    }

    private bool UsesAnimatorMotion()
    {
        return useAnimatorMotion && trapAnimator != null;
    }

    private void TriggerAnimatorState(bool makeActive)
    {
        if (trapAnimator == null)
        {
            return;
        }

        string setTrigger = makeActive ? activeTrigger : inactiveTrigger;
        string resetTrigger = makeActive ? inactiveTrigger : activeTrigger;

        if (!string.IsNullOrWhiteSpace(resetTrigger))
        {
            trapAnimator.ResetTrigger(resetTrigger);
        }

        if (!string.IsNullOrWhiteSpace(setTrigger))
        {
            trapAnimator.SetTrigger(setTrigger);
        }
    }

    private void SetActiveStateImmediate(bool active)
    {
        _isActive = active;

        if (damageTrigger != null)
        {
            damageTrigger.enabled = true;
        }

        if (collidersToToggle != null)
        {
            for (int i = 0; i < collidersToToggle.Length; i++)
            {
                Collider trapCollider = collidersToToggle[i];
                if (trapCollider != null)
                {
                    trapCollider.enabled = true;
                }
            }
        }

        if (visualsToToggle != null)
        {
            for (int i = 0; i < visualsToToggle.Length; i++)
            {
                if (visualsToToggle[i] != null)
                {
                    visualsToToggle[i].SetActive(active);
                }
            }
        }
    }

    private void AutoPopulateCollidersIfNeeded()
    {
        if (!autoPopulateCollidersToToggle)
        {
            return;
        }

        Transform root = movingPart != null ? movingPart : transform;
        Collider[] found = root.GetComponentsInChildren<Collider>(true);
        if (found == null || found.Length == 0)
        {
            return;
        }

        List<Collider> result = new List<Collider>(found.Length);
        for (int i = 0; i < found.Length; i++)
        {
            Collider col = found[i];
            if (col == null || col == damageTrigger || !IsSpearPart(col.transform))
            {
                continue;
            }

            result.Add(col);
        }

        if (result.Count == 0)
        {
            for (int i = 0; i < found.Length; i++)
            {
                Collider col = found[i];
                if (col == null || col == damageTrigger)
                {
                    continue;
                }

                result.Add(col);
            }
        }

        collidersToToggle = result.ToArray();
        DebugLog("Auto-populated collidersToToggle with " + collidersToToggle.Length + " colliders.");
    }

    private void EnsureSpearBodyColliders()
    {
        if (!autoCreateBodyColliders)
        {
            return;
        }

        Transform root = movingPart != null ? movingPart : transform;
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters == null || meshFilters.Length == 0)
        {
            return;
        }

        int addedCount = 0;
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || !IsSpearPart(meshFilter.transform))
            {
                continue;
            }

            Collider col = meshFilter.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = meshFilter.gameObject.AddComponent<BoxCollider>();
                if (meshFilter.sharedMesh != null)
                {
                    box.center = meshFilter.sharedMesh.bounds.center;
                    box.size = meshFilter.sharedMesh.bounds.size;
                }

                col = box;
                addedCount++;
            }

            col.isTrigger = false;
            if (spearPhysicMaterial != null)
            {
                col.sharedMaterial = spearPhysicMaterial;
            }
        }

        if (addedCount > 0)
        {
            DebugLog("Added " + addedCount + " body colliders to spear meshes.");
        }
    }

    private bool IsSpearPart(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(spearNameKeyword))
        {
            return target != transform;
        }

        return target.name.IndexOf(spearNameKeyword) >= 0;
    }

    private void ValidateSpearCollisionSetup()
    {
        if (collidersToToggle == null || collidersToToggle.Length == 0)
        {
            DebugLog("No spear colliders found.");
            return;
        }

        int missingRigidbodyLinks = 0;
        for (int i = 0; i < collidersToToggle.Length; i++)
        {
            Collider col = collidersToToggle[i];
            if (col == null)
            {
                continue;
            }

            if (col.isTrigger)
            {
                DebugLog("Collider is trigger (should be solid): " + col.name);
            }

            if (col.attachedRigidbody == null)
            {
                missingRigidbodyLinks++;
            }
        }

        if (missingRigidbodyLinks > 0)
        {
            Debug.LogWarning(
                "[PeriodicTrap:" + name + "] " + missingRigidbodyLinks +
                " spear colliders have no Rigidbody connection. " +
                "For reliable box collisions on moving spears, add kinematic Rigidbody " +
                "to each spear or their common parent.",
                this);
        }
    }

    private bool TryGetActiveTrapBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        if (collidersToToggle == null)
        {
            return false;
        }

        for (int i = 0; i < collidersToToggle.Length; i++)
        {
            Collider trapCollider = collidersToToggle[i];
            if (trapCollider == null || !trapCollider.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = trapCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(trapCollider.bounds);
            }
        }

        return hasBounds;
    }

    private bool IsPlayerInsideBounds(Bounds bounds)
    {
        if (_playerController != null && bounds.Intersects(_playerController.bounds))
        {
            return true;
        }

        return _playerObject != null && bounds.Contains(_playerObject.transform.position);
    }

    private bool IsPlayerTouchingTrapColliders(out string trapColliderName)
    {
        trapColliderName = string.Empty;

        if (collidersToToggle == null || collidersToToggle.Length == 0)
        {
            return false;
        }

        if (_playerColliders == null || _playerColliders.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < collidersToToggle.Length; i++)
        {
            Collider trapCollider = collidersToToggle[i];
            if (trapCollider == null || !trapCollider.enabled)
            {
                continue;
            }

            for (int j = 0; j < _playerColliders.Length; j++)
            {
                Collider playerCollider = _playerColliders[j];
                if (!IsValidPlayerCollider(playerCollider))
                {
                    continue;
                }

                // Broad-phase check before expensive penetration test.
                if (!trapCollider.bounds.Intersects(playerCollider.bounds))
                {
                    continue;
                }

                bool overlaps = Physics.ComputePenetration(
                    trapCollider,
                    trapCollider.transform.position,
                    trapCollider.transform.rotation,
                    playerCollider,
                    playerCollider.transform.position,
                    playerCollider.transform.rotation,
                    out _,
                    out _);

                if (!overlaps)
                {
                    continue;
                }

                trapColliderName = trapCollider.name;
                return true;
            }
        }

        return false;
    }

    private static bool IsValidPlayerCollider(Collider playerCollider)
    {
        return playerCollider != null &&
               playerCollider.enabled &&
               !playerCollider.isTrigger &&
               playerCollider.gameObject.activeInHierarchy;
    }

    private void TryKillPlayer(Collider other)
    {
        if (_playerCaught || other == null)
        {
            return;
        }

        GameObject target = FindPlayerFromCollider(other);
        if (target == null)
        {
            DebugLog("Trigger callback ignored: collider " + other.name + " is not player.");
            return;
        }

        _playerCaught = true;
        DebugLog("Kill triggered by OnTrigger (" + other.name + ").");
        StartCoroutine(KillPlayer(target));
    }

    private void TryKillCachedPlayer(string reason)
    {
        if (_playerCaught)
        {
            return;
        }

        EnsurePlayerReferences();
        if (_playerObject == null)
        {
            DebugLog("Kill skipped (" + reason + "): player reference is null.");
            return;
        }

        _playerCaught = true;
        DebugLog("Kill triggered by " + reason + ".");
        StartCoroutine(KillPlayer(_playerObject));
    }

    private GameObject FindPlayerFromCollider(Collider other)
    {
        if (_playerObject != null)
        {
            Transform playerTransform = _playerObject.transform;
            if (other.transform == playerTransform || other.transform.IsChildOf(playerTransform))
            {
                return _playerObject;
            }
        }

        if (playerRoot != null)
        {
            Transform rootTransform = playerRoot.transform;
            if (other.transform == rootTransform || other.transform.IsChildOf(rootTransform))
            {
                return playerRoot.gameObject;
            }
        }

        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            Transform current = other.transform;
            while (current != null)
            {
                if (current.CompareTag(playerTag))
                {
                    return current.gameObject;
                }

                current = current.parent;
            }
        }

        return null;
    }

    private IEnumerator KillPlayer(GameObject playerObject)
    {
        DebugLog("KillPlayer coroutine started for: " + playerObject.name);

        PlayerMovement movement = playerObject.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            movement = playerObject.GetComponentInChildren<PlayerMovement>();
        }

        CharacterController controller = playerObject.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = playerObject.GetComponentInChildren<CharacterController>();
        }

        if (movement != null)
        {
            movement.enabled = false;
        }

        if (controller != null)
        {
            controller.enabled = false;
        }

        if (deathDelay > 0f)
        {
            yield return new WaitForSeconds(deathDelay);
        }

        if (askForAnotherRoundOnDeath)
        {
            if (deathRoundPromptUI == null)
            {
                deathRoundPromptUI = FindObjectOfType<DeathRoundPromptUI>(true);
            }

            if (deathRoundPromptUI != null)
            {
                DebugLog("Showing death prompt UI.");
                deathRoundPromptUI.Show();
                yield break;
            }
        }

        if (reloadSceneOnDeath)
        {
            DebugLog("Reloading scene due to death.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield break;
        }

        if (respawnPoint != null)
        {
            playerObject.transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
        }

        if (controller != null)
        {
            controller.enabled = true;
        }

        if (movement != null)
        {
            movement.enabled = true;
        }

        _playerCaught = false;
        DebugLog("Death flow completed without scene reload.");
    }

    private void CachePlayerReferences()
    {
        if (playerRoot != null)
        {
            _playerObject = playerRoot.gameObject;
        }
        else if (_playerObject == null && !string.IsNullOrWhiteSpace(playerTag))
        {
            _playerObject = GameObject.FindWithTag(playerTag);
        }

        if (_playerObject == null)
        {
            PlayerMovement movement = FindObjectOfType<PlayerMovement>();
            if (movement != null)
            {
                _playerObject = movement.gameObject;
            }
        }

        if (_playerObject != null)
        {
            _playerController = _playerObject.GetComponentInChildren<CharacterController>();
            _playerColliders = _playerObject.GetComponentsInChildren<Collider>(true);
            DebugLog("Player cached: " + _playerObject.name);
        }
        else
        {
            _playerColliders = null;
            DebugLog("Player cache failed.");
        }
    }

    private void EnsurePlayerReferences()
    {
        if (_playerObject == null || !_playerObject.activeInHierarchy)
        {
            _playerObject = null;
            _playerController = null;
            _playerColliders = null;
            CachePlayerReferences();
            return;
        }

        if (_playerController == null)
        {
            _playerController = _playerObject.GetComponentInChildren<CharacterController>();
        }

        if (_playerColliders == null || _playerColliders.Length == 0)
        {
            _playerColliders = _playerObject.GetComponentsInChildren<Collider>(true);
        }
    }

    private void DebugLog(string message)
    {
        if (!debugCollision)
        {
            return;
        }

        Debug.Log("[PeriodicTrap:" + name + "] " + message, this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugBounds)
        {
            return;
        }

        if (TryGetActiveTrapBounds(out Bounds trapBounds))
        {
            Gizmos.color = _isActive ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(trapBounds.center, trapBounds.size);
        }

        if (_playerController != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_playerController.bounds.center, _playerController.bounds.size);
        }
        else if (_playerObject != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_playerObject.transform.position, 0.25f);
        }
    }
}
