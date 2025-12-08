using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;

        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip(
            "Time required to pass before being able to jump again. Set to 0f to instantly jump again"
        )]
        public float JumpTimeout = 0.1f;

        [Tooltip(
            "Time required to pass before entering the fall state. Useful for walking down stairs"
        )]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip(
            "If the character is grounded or not. Not part of the CharacterController built in grounded check"
        )]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip(
            "The radius of the grounded check. Should match the radius of the CharacterController"
        )]
        public float GroundedRadius = 0.5f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip(
            "The follow target set in the Cinemachine Virtual Camera that the camera will follow"
        )]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        // === Moving Platform ===
        [Header("Moving Platform")]
        [Tooltip(
            "Which layers count as platforms (if the platform is also in GroundLayers, it can be left blank)"
        )]
        public LayerMask PlatformLayers;

        public bool isOnPlatform = false;

        public moving_platform _platform;
        private Vector3 _platformOffsetThisFrame;
        private Vector3 _smoothedPlatformVelocity;
        private bool _wasOnPlatform;

        // cinemachine
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _rotationVelocity;



        // replaced with _gravityVelocity
        // private float _verticalVelocity;


        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;


        // === Gravity frame ===
        private Vector3 gravityDir => GravityManager.Instance ? GravityManager.Instance.GravityDir : Vector3.down;
        private float gravityAccel => GravityManager.Instance ? GravityManager.Instance.GravityAccel : 15f;
        private Vector3 up => -gravityDir;

        // 取代 float _verticalVelocity
        private Vector3 _gravityVelocity = Vector3.zero;
        private Vector3 _lastGravityDir = Vector3.down; // 记录上一帧的重力方向

        private float _terminalVelocity = 53.0f; // 依然保留，作为重力向量的最大模长

        // GravityField 对齐控制
        private bool _allowAutoAlign = true; // 是否允许自动对齐（当 GravityField 在对齐时暂停）
        private bool _movementFrozen = false; // 是否冻结玩家移动（重力场切换时使用）





        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError(
                "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it"
            );
#endif

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // 初始化重力方向
            _lastGravityDir = gravityDir;
        }

        private void Update()
        {
            // Skip movement when game is paused
            if (PauseMenuController.IsPaused)
                return;

            // 检测重力方向变化
            if (Vector3.Dot(_lastGravityDir, gravityDir) < 0.999f) // 方向有明显变化
            {
                OnGravityDirectionChanged(_lastGravityDir, gravityDir);
                _lastGravityDir = gravityDir;
            }

            JumpAndGravity();
            GroundedCheck();
            Move();

            // 每帧对齐角色的 up 方向到重力反方向，防止累积旋转误差
            AlignCharacterToGravity();
        }

        private void LateUpdate()
        {
            // Skip camera rotation when game is paused
            if (PauseMenuController.IsPaused)
                return;

            CameraRotation();
        }

        private void GroundedCheck()
        {
            // 以"新上方向"偏移
            Vector3 spherePosition = transform.position + up * GroundedOffset;

            bool new_state = Physics.CheckSphere(
                spherePosition,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );


            // Not allow to stand on grabbable objects while grabbing
            // get transformer under player feet
            if (Physics.Raycast(spherePosition, -up, out RaycastHit hitt, GroundedRadius, GroundLayers))
            {
                if (hitt.collider.GetComponent<Grabbable>() != null)
                {
                    if (hitt.collider.GetComponent<Grabbable>().isGrabbing)
                    {
                        new_state = false;
                    }
                }
            }

            // === Identify the platform under the player's feet ===
            if (new_state && !Grounded) // only check when just became grounded
            {
                // 沿"新下方向"做短球投射
                Vector3 castStart = spherePosition + up * 0.05f;
                float castRadius = GroundedRadius * 0.95f;
                float castDistance = 0.3f;
                LayerMask combinedLayers = GroundLayers | PlatformLayers;

                bool sphereCastHit = Physics.SphereCast(
                    castStart,
                    castRadius,
                    -up,
                    out RaycastHit hit,
                    castDistance,
                    combinedLayers,
                    QueryTriggerInteraction.Ignore
                );

                if (sphereCastHit)
                {
                    // attachedRigidbody can be used on Kinematic; GetComponentInParent is safe
                    _platform = hit.collider.attachedRigidbody
                        ? hit.collider.GetComponent<moving_platform>()
                        : hit.collider.GetComponentInParent<moving_platform>();
                    //Debug.Log("getted platform: " + _platform);
                }
            }
            else if (!new_state) // lost grounding
            {
                _platform = null;
                //Debug.Log("Left platform");
            }
            Grounded = new_state;
        }

        private void CameraRotation()
        {
            // if there is an input
            if (_input.look.sqrMagnitude >= _threshold)
            {
                //Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                // clamp our pitch rotation
                _cinemachineTargetPitch = ClampAngle(
                    _cinemachineTargetPitch,
                    BottomClamp,
                    TopClamp
                );

                // Update Cinemachine camera target pitch
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(
                    _cinemachineTargetPitch,
                    0.0f,
                    0.0f
                );

                // rotate the player left and right
                transform.Rotate(up * _rotationVelocity, Space.World);
            }
        }

        private void Move()
        {
            // 如果移动被冻结，直接返回
            if (_movementFrozen) return;

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero)
                targetSpeed = 0.0f;

            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;



            // === Moving Platform Adjustments ===

            _platformOffsetThisFrame = Vector3.zero;
            isOnPlatform = Grounded && (_platform);

            Vector3 surfaceVel = Vector3.zero;
            if (isOnPlatform)
            {
                surfaceVel = _platform.GetSurfaceVelocity(transform.position);

                if (_wasOnPlatform)
                {
                    // 已在平台上：平滑跟随
                    _smoothedPlatformVelocity = Vector3.Lerp(
                        _smoothedPlatformVelocity, surfaceVel, Time.deltaTime * 15f);
                }
                else
                {
                    // 刚落到平台：更柔和切入
                    _smoothedPlatformVelocity = Vector3.Lerp(
                        Vector3.zero, surfaceVel, Time.deltaTime * 8f);
                }

                _platformOffsetThisFrame = _smoothedPlatformVelocity * Time.deltaTime;
            }
            else if (_wasOnPlatform)
            {
                // 刚离开平台：平滑衰减残余平台速度
                _smoothedPlatformVelocity = Vector3.Lerp(
                    _smoothedPlatformVelocity, Vector3.zero, Time.deltaTime * 10f);
                _platformOffsetThisFrame = _smoothedPlatformVelocity * Time.deltaTime;
            }
            else
            {
                _smoothedPlatformVelocity = Vector3.zero;
            }

            // 记录平台接触态，用于下帧
            _wasOnPlatform = isOnPlatform;

            // 2) 基于"新上方向"的前/右（把角色 forward/right 投影到新平面）
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.ProjectOnPlane(transform.forward + transform.right * 0.001f, up).normalized;
            Vector3 right = Vector3.Cross(up, fwd).normalized;



            // calculate current horizontal speed    
            Vector3 platformVel = isOnPlatform ? _smoothedPlatformVelocity : Vector3.zero;

            // 控制器世界速度 → 去掉沿 up 的分量，得到"平面速度"
            Vector3 worldPlanarVelNow = Vector3.ProjectOnPlane(_controller.velocity, up);
            // 相对平台的平面速度
            Vector3 relPlanarVelNow = worldPlanarVelNow - Vector3.ProjectOnPlane(platformVel, up);
            float currentPlanarSpeed = relPlanarVelNow.magnitude;

            const float speedOffset = 0.1f;
            if (currentPlanarSpeed < targetSpeed - speedOffset ||
                currentPlanarSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(
                    currentPlanarSpeed,
                    targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate
                );

                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // 4) 输入方向（落在平面上）
            Vector3 inputDirection = Vector3.zero;
            if (_input.move != Vector2.zero)
                inputDirection = (right * _input.move.x + fwd * _input.move.y).normalized;

            Vector3 planarDisplacement = inputDirection * (_speed * Time.deltaTime);

            // 5) 重力位移（向量），在 JumpAndGravity() 里维护 _gravityVelocity
            Vector3 gravityDisplacement = _gravityVelocity * Time.deltaTime;

            // 6) 合成（平台位移 + 平面移动 + 重力）
            Vector3 totalMovement = _platformOffsetThisFrame + planarDisplacement + gravityDisplacement;

            // move the player
            _controller.Move(totalMovement);
        }


        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // fall timeout
                _fallTimeoutDelta = FallTimeout;

                // 贴地时清掉"沿重力方向"的速度，避免继续下坠
                _gravityVelocity -= Vector3.Project(_gravityVelocity, gravityDir);

                // 轻微压住地面（原脚本的 -2f 效果，沿重力反方向给一点点）
                _gravityVelocity += -up * 2f * Time.deltaTime * 20f; // 可选，保持接触稳定

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // v0 = sqrt(2 g h)
                    float jumpV = Mathf.Sqrt(2f * gravityAccel * Mathf.Max(0f, JumpHeight));
                    // 沿"上方向"给初速度
                    _gravityVelocity += up * jumpV;
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset jump timeout
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                    _fallTimeoutDelta -= Time.deltaTime;

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // 施加重力（沿 gravityDir），并限制终端速度（只限制"沿重力方向"的分量）
            // 先加重力
            _gravityVelocity += gravityDir * gravityAccel * Time.deltaTime;

            // 终端速度：限制投影到 gravityDir 的分量大小
            Vector3 alongG = Vector3.Project(_gravityVelocity, gravityDir);
            Vector3 tangential = _gravityVelocity - alongG;
            float sign = Mathf.Sign(Vector3.Dot(alongG, gravityDir)); // 应该为正
            float mag = alongG.magnitude;
            if (mag > _terminalVelocity)
                alongG = gravityDir * (_terminalVelocity * sign);

            _gravityVelocity = tangential + alongG;
        }

        /// <summary>
        /// 当重力方向改变时，重新映射重力速度向量
        /// </summary>
        private void OnGravityDirectionChanged(Vector3 oldDir, Vector3 newDir)
        {
            // 将旧重力速度分解为：沿旧重力方向的分量 + 切线分量
            Vector3 alongOldGravity = Vector3.Project(_gravityVelocity, oldDir);
            Vector3 tangentialToOldGravity = _gravityVelocity - alongOldGravity;

            // 将沿旧重力方向的分量映射到新重力方向
            // 保持速度的大小，但改变方向
            float speedAlongGravity = alongOldGravity.magnitude * Mathf.Sign(Vector3.Dot(alongOldGravity, oldDir));
            Vector3 newAlongGravity = newDir * speedAlongGravity;

            // 将切线分量投影到新的地面平面，并大幅衰减以防止"冲刺"效果
            Vector3 newUp = -newDir;
            Vector3 tangentialOnNewPlane = Vector3.ProjectOnPlane(tangentialToOldGravity, newUp);

            // 只保留很小一部分切线速度（10%），避免离开重力场后向前冲
            _gravityVelocity = newAlongGravity + tangentialOnNewPlane * 0.1f;
        }

        /// <summary>
        /// 每帧对齐角色的 up 方向到重力反方向，同时保持水平朝向不变
        /// 这防止了由于绕不同轴旋转导致的累积误差
        /// </summary>
        private void AlignCharacterToGravity()
        {
            // 如果外部系统（如 GravityField）正在控制旋转，暂停自动对齐
            if (!_allowAutoAlign) return;

            // 当前角色的 up 方向
            Vector3 currentUp = transform.up;
            Vector3 targetUp = up; // 重力反方向

            // 如果已经对齐，跳过（节省性能）
            if (Vector3.Dot(currentUp, targetUp) > 0.9999f) return;

            // 获取当前角色在"新地面平面"上的前方向（投影）
            Vector3 currentForwardOnPlane = Vector3.ProjectOnPlane(transform.forward, targetUp);

            // 如果前方向几乎垂直于目标 up（极端情况），使用 right 来辅助
            if (currentForwardOnPlane.sqrMagnitude < 0.001f)
            {
                currentForwardOnPlane = Vector3.ProjectOnPlane(transform.right, targetUp);
                if (currentForwardOnPlane.sqrMagnitude < 0.001f)
                {
                    // 如果还是不行，随便选一个垂直于 targetUp 的方向
                    currentForwardOnPlane = Vector3.Cross(targetUp, Vector3.right);
                    if (currentForwardOnPlane.sqrMagnitude < 0.001f)
                        currentForwardOnPlane = Vector3.Cross(targetUp, Vector3.forward);
                }
            }
            currentForwardOnPlane.Normalize();

            // 构建新的旋转：up 对齐 targetUp，forward 保持在平面上的投影
            Quaternion targetRotation = Quaternion.LookRotation(currentForwardOnPlane, targetUp);

            // 平滑插值（可选，如果想要即时对齐，直接赋值）
            // transform.rotation = targetRotation; // 即时对齐
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f); // 平滑对齐
        }

        /// <summary>
        /// 允许外部系统（如 GravityField）临时禁用自动对齐
        /// </summary>
        public void SetAutoAlignEnabled(bool enabled)
        {
            _allowAutoAlign = enabled;
        }

        /// <summary>
        /// 允许外部系统（如 GravityField）冻结玩家移动
        /// </summary>
        public void SetMovementFrozen(bool frozen)
        {
            _movementFrozen = frozen;
            
            // 冻结时清空当前速度，防止滑动
            if (frozen && _controller != null)
            {
                // 保留重力速度，但清空平面移动速度
                _speed = 0f;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f)
                lfAngle += 360f;
            if (lfAngle > 360f)
                lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded)
                Gizmos.color = transparentGreen;
            else
                Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Vector3 spherePosition = transform.position + up * GroundedOffset;
            Gizmos.DrawSphere(spherePosition, GroundedRadius);
        }

        private void OnDrawGizmos()
        {
            Vector3 spherePosition = transform.position + up * GroundedOffset;

            Gizmos.color = Color.green;
            Vector3 castStart = spherePosition + up * 0.05f;
            Gizmos.DrawWireSphere(castStart, GroundedRadius * 0.95f);

            Gizmos.color = Color.green;
            Vector3 castEnd = castStart + (-up) * 0.3f;
            Gizmos.DrawLine(castStart, castEnd);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(castEnd, GroundedRadius * 0.95f);
        }
    }
}
