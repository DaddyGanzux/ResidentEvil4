using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Crouch speed of the character in m/s")]
        public float CrouchSpeed = 1.2f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face camera direction when moving")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;
        public float Sensitivity = 1.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("Crouch Settings")]
        [Tooltip("Height of the CharacterController when crouching")]
        public float CrouchHeight = 1.0f;

        [Tooltip("Center Y offset of the CharacterController when crouching")]
        public float CrouchCenterY = 0.5f;

        [Tooltip("How fast the CharacterController resizes when crouching/standing")]
        public float CrouchTransitionSpeed = 10.0f;

        [Tooltip("Mask for obstacle detection above player when standing up")]
        public LayerMask CeilingLayers;

        [Header("Crouch Camera Settings")]
        [Tooltip("Altura objetivo del Cinemachine Target cuando estás de pie")]
        public float StandCameraTargetHeight = 1.37f;

        [Tooltip("Altura objetivo del Cinemachine Target cuando estás agachado")]
        public float CrouchCameraTargetHeight = 0.8f;

        [Tooltip("Velocidad de transición de la cámara al agacharse/levantarse")]
        public float CameraHeightSmoothTime = 8.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // crouch state
        public bool IsCrouched { get; private set; }
        private float _defaultHeight;
        private Vector3 _defaultCenter;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        // RE4 Remake Animation IDs
        private int _animIDIsStrafeLeft;
        private int _animIDIsStrafeRight;
        private int _animIDIsWalkingBack;

        // Crouch Animation IDs
        private int _animIDIsCrouched;
        private int _animIDCrouchStrafeLeft;
        private int _animIDCrouchStrafeRight;
        private int _animIDCrouchWalkingBack;
        private int _animIDIsCrouchWalking;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            _defaultHeight = _controller.height;
            _defaultCenter = _controller.center;

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            HandleCrouch();
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            UpdateCameraHeight();
            CameraRotation();
        }

        private void UpdateCameraHeight()
        {
            if (CinemachineCameraTarget == null) return;

            // Selecciona la altura según si está agachado
            float targetHeight = IsCrouched ? CrouchCameraTargetHeight : StandCameraTargetHeight;
            Vector3 currentPos = CinemachineCameraTarget.transform.localPosition;

            // Interpola suavemente la posición Y local del Cinemachine Target
            float newY = Mathf.Lerp(currentPos.y, targetHeight, Time.deltaTime * CameraHeightSmoothTime);
            CinemachineCameraTarget.transform.localPosition = new Vector3(currentPos.x, newY, currentPos.z);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

            _animIDIsStrafeLeft = Animator.StringToHash("IsStrafeLeft");
            _animIDIsStrafeRight = Animator.StringToHash("IsStrafeRight");
            _animIDIsWalkingBack = Animator.StringToHash("IsWalkingBack");

            _animIDIsCrouched = Animator.StringToHash("IsCrouched");
            _animIDCrouchStrafeLeft = Animator.StringToHash("CrouchStrafeLeft");
            _animIDCrouchStrafeRight = Animator.StringToHash("CrouchStrafeRight");
            _animIDCrouchWalkingBack = Animator.StringToHash("CrouchWalkingBack");
            _animIDIsCrouchWalking = Animator.StringToHash("IsCrouchWalking");
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier * Sensitivity;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * Sensitivity;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private void HandleCrouch()
        {
            bool crouchInput = _input.crouch;

            if (crouchInput && Grounded)
            {
                IsCrouched = true;
            }
            else if (!crouchInput && IsCrouched)
            {
                if (!CanStandUp())
                {
                    IsCrouched = true;
                }
                else
                {
                    IsCrouched = false;
                }
            }

            float targetHeight = IsCrouched ? CrouchHeight : _defaultHeight;
            Vector3 targetCenter = IsCrouched ? new Vector3(_defaultCenter.x, CrouchCenterY, _defaultCenter.z) : _defaultCenter;

            _controller.height = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * CrouchTransitionSpeed);
            _controller.center = Vector3.Lerp(_controller.center, targetCenter, Time.deltaTime * CrouchTransitionSpeed);
        }

        private bool CanStandUp()
        {
            Vector3 rayOrigin = transform.position + Vector3.up * CrouchHeight;
            float checkDistance = _defaultHeight - CrouchHeight;
            return !Physics.SphereCast(rayOrigin, _controller.radius, Vector3.up, out _, checkDistance, CeilingLayers == 0 ? GroundLayers : CeilingLayers, QueryTriggerInteraction.Ignore);
        }

        private void Move()
        {
            float targetSpeed = MoveSpeed;

            if (IsCrouched)
            {
                targetSpeed = CrouchSpeed;
            }
            else if (_input.sprint)
            {
                targetSpeed = SprintSpeed;
            }

            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            bool isMoving = _input.move != Vector2.zero;

            if (isMoving && !_input.aim)
            {
                _targetRotation = _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = (transform.forward * _input.move.y) + (transform.right * _input.move.x);

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            bool isStrafeLeft = _input.move.x < -0.1f;
            bool isStrafeRight = _input.move.x > 0.1f;
            bool isWalkingBack = _input.move.y < -0.1f;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

                _animator.SetBool(_animIDIsStrafeLeft, isStrafeLeft && !IsCrouched);
                _animator.SetBool(_animIDIsStrafeRight, isStrafeRight && !IsCrouched);
                _animator.SetBool(_animIDIsWalkingBack, isWalkingBack && !IsCrouched);

                _animator.SetBool(_animIDIsCrouched, IsCrouched);
                _animator.SetBool(_animIDCrouchStrafeLeft, isStrafeLeft && IsCrouched);
                _animator.SetBool(_animIDCrouchStrafeRight, isStrafeRight && IsCrouched);
                _animator.SetBool(_animIDCrouchWalkingBack, isWalkingBack && IsCrouched);
                bool isCrouchWalking = IsCrouched && _input.move != Vector2.zero;
                _animator.SetBool(_animIDIsCrouchWalking, isCrouchWalking);
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

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f && !IsCrouched)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        public void SetSensitivity(float newSensitivity)
        {
            Sensitivity = newSensitivity;
        }
    }
}