using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    // Compatibility shim for Starter Assets prefab references.
    // This project uses PlayerMovement for control logic.
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 2f;
        public float SprintSpeed = 5.335f;
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10f;

        [Header("Audio")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0f, 1f)] public float FootstepAudioVolume = 0.3f;

        [Header("Jump")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15f;
        public float JumpTimeout = 0.3f;
        public float FallTimeout = 0.15f;
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers = 1;

        [Header("Camera")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70f;
        public float BottomClamp = -30f;
        public float CameraAngleOverride = 0f;
        public bool LockCameraPosition = false;

        private StarterAssetsInputs _inputs;

        private void Awake()
        {
            _inputs = GetComponent<StarterAssetsInputs>();
        }

#if ENABLE_INPUT_SYSTEM
        public void InputMove(InputValue value)
        {
            if (_inputs != null)
            {
                _inputs.MoveInput(value.Get<Vector2>());
            }
        }

        public void InputLook(InputValue value)
        {
            if (_inputs != null && _inputs.cursorInputForLook)
            {
                _inputs.LookInput(value.Get<Vector2>());
            }
        }

        public void InputJump(InputValue value)
        {
            if (_inputs != null)
            {
                _inputs.JumpInput(value.isPressed);
            }
        }

        public void InputSprint(InputValue value)
        {
            if (_inputs != null)
            {
                _inputs.SprintInput(value.isPressed);
            }
        }
#endif

        private void OnFootstep(AnimationEvent animationEvent) { }

        private void OnLand(AnimationEvent animationEvent) { }
    }
}
