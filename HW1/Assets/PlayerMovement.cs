using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(StarterAssetsInputs))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 3.0f;
        public float SprintSpeed = 5.5f;
        public float SpeedChangeRate = 12.0f;
        [Range(0f, 1f)] public float AirControl = 0.35f;

        [Header("Look")]
        public Transform CameraRoot;
        [Range(10f, 20000f)] public float MouseSensitivity = 7000f;
        public float TopClamp = 85f;
        public float BottomClamp = -85f;

        [Header("Jump")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.5f;
        public float FallTimeout = 0.15f;
        public bool Grounded;

        [Header("Push")]
        public bool CanPush = true;
        public LayerMask PushLayers = ~0;
        [Range(0.5f, 20f)] public float PushStrength = 4f;
        [Range(0.5f, 10f)] public float MaxPushSpeed = 2f;

        private const float TerminalVelocity = 53.0f;

        private float _cameraPitch;
        private float _animationBlend;
        private float _verticalVelocity;
        private Vector3 _horizontalVelocity;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;

        private bool _hasAnimator;

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            if (CameraRoot == null && Camera.main != null)
            {
                CameraRoot = Camera.main.transform;
            }

            if (CameraRoot != null)
            {
                _cameraPitch = NormalizeAngle(CameraRoot.localEulerAngles.x);
            }
        }

        private void Update()
        {
            if (_controller == null || _input == null)
                return;

            _hasAnimator = TryGetComponent(out _animator);

            Look();
            GroundedCheck();
            JumpAndGravity();
            Move();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            Grounded = _controller.isGrounded;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void Look()
        {
            if (_input.look.sqrMagnitude < 0.0001f)
                return;

            float lookDelta = MouseSensitivity * Time.deltaTime;
            float yawDelta = _input.look.x * lookDelta;
            float pitchDelta = _input.look.y * lookDelta;

            // Horizontal look rotates the whole player, affecting direction and camera yaw.
            transform.Rotate(Vector3.up * yawDelta, Space.World);

            // Vertical look only rotates the view.
            _cameraPitch += pitchDelta;
            _cameraPitch = Mathf.Clamp(_cameraPitch, BottomClamp, TopClamp);

            if (CameraRoot != null)
            {
                if (CameraRoot.IsChildOf(transform))
                {
                    CameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
                }
                else
                {
                    CameraRoot.rotation = Quaternion.Euler(_cameraPitch, transform.eulerAngles.y, 0f);
                }
            }
        }

        private void Move()
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            if (_input.move == Vector2.zero)
                targetSpeed = 0f;

            float inputMagnitude = _input.analogMovement
                ? _input.move.magnitude
                : (_input.move == Vector2.zero ? 0f : 1f);
            Vector3 inputDirection = new Vector3(_input.move.x, 0f, _input.move.y);
            inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

            Vector3 worldDirection =
                (transform.right * inputDirection.x + transform.forward * inputDirection.z).normalized;
            Vector3 targetHorizontalVelocity = worldDirection * (targetSpeed * inputMagnitude);

            if (Grounded)
            {
                // Grounded movement is direct to avoid drifting/ice-like controls.
                _horizontalVelocity = targetHorizontalVelocity;
            }
            else
            {
                float acceleration = SpeedChangeRate * AirControl;
                _horizontalVelocity = Vector3.MoveTowards(
                    _horizontalVelocity,
                    targetHorizontalVelocity,
                    acceleration * Time.deltaTime);
            }

            _animationBlend = Mathf.Lerp(
                _animationBlend,
                targetSpeed,
                Time.deltaTime * SpeedChangeRate);

            if (_animationBlend < 0.01f)
                _animationBlend = 0f;

            _controller.Move(
                (_horizontalVelocity + new Vector3(0f, _verticalVelocity, 0f)) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0)
                    _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                        _animator.SetBool(_animIDJump, true);
                }

                if (_jumpTimeoutDelta >= 0)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0)
                    _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator)
                    _animator.SetBool(_animIDFreeFall, true);

                _input.jump = false;
            }

            if (_verticalVelocity > -TerminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!CanPush)
                return;

            if (_input == null || _input.move.sqrMagnitude < 0.0001f)
                return;

            Rigidbody body = hit.collider.attachedRigidbody;

            if (body == null || body.isKinematic)
                return;

            int bodyLayerMask = 1 << body.gameObject.layer;
            if ((bodyLayerMask & PushLayers.value) == 0)
                return;

            // Ignore downward hits so we do not push objects under the player.
            if (hit.moveDirection.y < -0.3f)
                return;

            Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;
            if (pushDirection.sqrMagnitude < 0.0001f)
                return;

            Vector3 playerHorizontalDirection = new Vector3(_horizontalVelocity.x, 0f, _horizontalVelocity.z);
            if (playerHorizontalDirection.sqrMagnitude < 0.0001f)
                return;

            float pushAlignment = Vector3.Dot(playerHorizontalDirection.normalized, pushDirection);
            if (pushAlignment <= 0f)
                return;

            body.AddForce(pushDirection * (PushStrength * pushAlignment), ForceMode.Acceleration);

            Vector3 bodyHorizontalVelocity = new Vector3(body.velocity.x, 0f, body.velocity.z);
            if (bodyHorizontalVelocity.sqrMagnitude > MaxPushSpeed * MaxPushSpeed)
            {
                bodyHorizontalVelocity = bodyHorizontalVelocity.normalized * MaxPushSpeed;
                body.velocity = new Vector3(bodyHorizontalVelocity.x, body.velocity.y, bodyHorizontalVelocity.z);
            }
        }

        private static float NormalizeAngle(float angle)
        {
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }

        // Fix animation event warnings

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // placeholder
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // placeholder
            }
        }
    }
}
