using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GrapplingGun : MonoBehaviour
{
    public enum GrapplePullMode
    {
        PullTargetToPlayer,   // 拉目标到玩家（默认）
        PullPlayerToTarget    // 拉玩家到目标（经典抓钩）
    }

    [Header("Aim / Fire")]
    public Transform fireOrigin; 
    public float maxDistance = 25f;
    public LayerMask magneticLayerMask;
    // 注意：现在只检测有Magnetic_Poles组件的物体，不再使用Tag检测

    [Header("Pull Mode")]
    public GrapplePullMode pullMode = GrapplePullMode.PullTargetToPlayer;
    public bool allowModeToggleKey = true;
    public KeyCode toggleKey = KeyCode.X;
    public Rigidbody playerRb; // PullPlayerToTarget 模式需要

    [Header("Pulling (soft)")]
    public float pullForce = 80f;
    public float pullDamping = 5f;
    public float pullMaxSpeed = 30f;

    [Header("Snap / Attach (hard)")]
    public bool useSnap = true;
    public float snapDistance = 0.6f;
    public bool snapUseFixedJoint = true;
    public float snapBreakForce = 800f;
    public float snapBreakTorque = 800f;

    [Header("Rope / Visual")]
    public LineRenderer lineRenderer;
    public float ropeLerpSpeed = 15f;

    [Header("Orbit (Mouse)")]
    public bool mouseOrbitEnabled = true;         // 鼠标球面平移（维持半径不变）
    public float mouseOrbitSensitivityX = 120f;   // 水平方向（度/单位鼠标）
    public float mouseOrbitSensitivityY = 80f;    // 垂直方向（度/单位鼠标）
    public float orbitPitchMin = -80f;            // 俯仰角限制（度）
    public float orbitPitchMax = 80f;

    [Header("Keep Level")]
    public bool keepTargetLevel = true;           // 抓取中始终保持物体水平，仅允许绕世界Y旋转

    [Header("Assist (Lift / Stability)")]
    public bool disableTargetGravityOnGrapple = true; // 抓取时关闭目标重力，便于抬起
    public bool reduceTargetMassOnGrapple = false;    // 可选：抓取时临时降低质量
    public float targetMassWhileGrappling = 0.5f;     // 抓取期间使用的质量（需 > 0）
    public float extraDragWhileGrappling = 0.0f;      // 抓取期间额外线性阻力（0 表示不改）

    [Header("Rotation (Yaw Only)")]
    public bool allowRotation = true;
    public float rotationSpeed = 120f; // Q/E 水平旋转速度（度/秒）

    [Header("Mouse Lateral Move")]
    public bool mouseStrafeEnabled = true;      // 鼠标左右→物体左右平移
    public float mouseStrafeSensitivity = 0.6f; // 侧移灵敏度（配合 Mouse X）

    [Header("Scroll Distance")]
    public bool useScrollForDistance = true;    // 滚轮调节远近
    public float scrollSpeed = 2.0f;            // 每滚一格改变的距离（米）
    public float minDistance = 0.35f;           // 最近距离
    public float maxDistanceScroll = 25f;       // 最远距离（仅滚轮控制用）

    [Header("Debug")]
    [Tooltip("Enable debug logging for grappling detection")]
    public bool showDebugInfo = true; // 显示调试信息 - FORCE RECOMPILE

    // runtime
    Rigidbody targetBody = null;            // 命中目标
    SpringJoint springJointOnTarget = null; // 目标上的弹簧（拉目标）
    SpringJoint springJointOnPlayer = null; // 玩家上的弹簧（拉玩家）
    FixedJoint fixedJoint = null;           // 硬吸附
    bool isGrappling = false;

    Vector3 pendingAnchorPosition;
    bool hasPendingAnchorPosition = false;
    Quaternion pendingAnchorRotation;
    bool hasPendingAnchorRotation = false;

    // 命中/附着点（记录为目标的局部坐标，便于目标移动时跟随）
    Vector3 attachPointLocalOnTarget = Vector3.zero;

    // 期望保持的距离（默认用开火瞬间的距离，可用滚轮调整）
    float desiredDistance = 0f;

    // 提示频率限制（缺少 Rigidbody 的提示避免刷屏）
    float lastNoRbWarnTime = -999f;
    const float noRbWarnCooldown = 0.25f;

    // 抓取前的目标物理参数备份
    bool prevTargetUseGravity = false;
    float prevTargetMass = 1f;
    float prevTargetDrag = 0f;
    float prevTargetAngularDrag = 0.05f;

    void Start()
    {
        // 强制启用调试模式，因为装备系统会重新实例化对象
        showDebugInfo = true;
        Debug.Log("GrapplingGun: Start() called - Debug mode enabled: " + showDebugInfo);
        
        if (fireOrigin == null) fireOrigin = transform;

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = 0.05f;
		// 若未在 Inspector 配置图层掩码，则回退到默认可射线层，避免无法命中
		if (magneticLayerMask.value == 0)
		{
			magneticLayerMask = Physics.DefaultRaycastLayers;
		}
    }

    void Update()
    {
        // 切换拉力模式（可选）
        if (allowModeToggleKey && Input.GetKeyDown(toggleKey))
        {
            pullMode = (pullMode == GrapplePullMode.PullTargetToPlayer)
                ? GrapplePullMode.PullPlayerToTarget
                : GrapplePullMode.PullTargetToPlayer;
        }

        // 开火/释放
        if (Input.GetMouseButtonDown(0)) 
        {
            if (showDebugInfo)
                Debug.Log("GrapplingGun: Left mouse button pressed!");
            TryFire();
        }
        if (Input.GetMouseButtonUp(0)) ReleaseGrapple();

        // 右键：硬链接开/关（开：建立 FixedJoint；关：恢复软连接，不释放抓取）
        if (Input.GetMouseButtonDown(1) && isGrappling)
        {
            if (fixedJoint != null)
                CancelFixedSnapToSoft();
            else
                MakeFixedSnap();
        }

        // Q/E：只水平旋转（绕世界Y轴）
        if (isGrappling && allowRotation && targetBody != null)
        {
            float yaw = 0f;
            if (Input.GetKey(KeyCode.Q)) yaw = -1f;
            if (Input.GetKey(KeyCode.E)) yaw = 1f;

            if (yaw != 0f)
            {
                // 硬连：旋转锚点；软连：旋转目标
                if (fixedJoint != null && fixedJoint.connectedBody != null)
                {
                    Rigidbody anchorRb = fixedJoint.connectedBody;
                    if (anchorRb != null && anchorRb.isKinematic && anchorRb.gameObject.name.StartsWith("GrappleAnchor_"))
                    {
                        Quaternion delta = Quaternion.Euler(0f, yaw * rotationSpeed * Time.deltaTime, 0f);
                        pendingAnchorRotation = delta * anchorRb.rotation;
                        hasPendingAnchorRotation = true;
                    }
                }
                else
                {
                    Quaternion delta = Quaternion.Euler(0f, yaw * rotationSpeed * Time.deltaTime, 0f);
                    Quaternion targetRot = delta * targetBody.rotation;
                    Vector3 e = targetRot.eulerAngles;
                    targetRot = Quaternion.Euler(0f, e.y, 0f);
                    targetBody.MoveRotation(targetRot);
                }
            }
        }

        // 鼠标球面平移：按鼠标移动在球面上绕参考点转动（保持半径不变）
        if (isGrappling && mouseOrbitEnabled && targetBody != null)
        {
            float mx;
            float my;
#if ENABLE_INPUT_SYSTEM
            Vector2 md = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            mx = md.x * 0.02f; // 经验缩放，避免过敏感
            my = md.y * 0.02f;
#else
            mx = Input.GetAxis("Mouse X");
            my = Input.GetAxis("Mouse Y");
#endif
            if (Mathf.Abs(mx) > 0.0001f || Mathf.Abs(my) > 0.0001f)
            {
                Vector3 worldAttach = targetBody.transform.TransformPoint(attachPointLocalOnTarget);
                Vector3 refPos = (pullMode == GrapplePullMode.PullPlayerToTarget && playerRb != null)
                    ? playerRb.worldCenterOfMass : fireOrigin.position;
                Vector3 fromRef = worldAttach - refPos;
                float radius = Mathf.Max(fromRef.magnitude, 0.0001f);

                // 当前方向的球坐标（以 refPos 为原点）
                Vector3 dir = fromRef.normalized;
                // 构造局部基（右、上）
                Vector3 up = Vector3.up;
                Vector3 right = Vector3.Cross(up, dir).normalized;
                if (right.sqrMagnitude < 1e-6f) right = Vector3.right; // 退化时给默认
                up = Vector3.Cross(dir, right).normalized;

                float yawDeg = mx * mouseOrbitSensitivityX * Time.deltaTime;   // 绕世界Y近似
                float pitchDeg = -my * mouseOrbitSensitivityY * Time.deltaTime; // 鼠标上抬为负，向上看

                // 限制 pitch（相对于水平）
                float currentPitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(dir, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;
                float clampedPitch = Mathf.Clamp(currentPitch + pitchDeg, orbitPitchMin, orbitPitchMax);
                float appliedPitch = clampedPitch - currentPitch;

                // 应用旋转：先绕 up（近似世界Y）的 yaw，再绕 right 的 pitch
                Quaternion qYaw = Quaternion.AngleAxis(yawDeg, Vector3.up);
                Quaternion qPitch = Quaternion.AngleAxis(appliedPitch, right);
                Vector3 newDir = (qYaw * (qPitch * dir)).normalized;

                Vector3 newWorldAttach = refPos + newDir * radius;

                if (fixedJoint != null && fixedJoint.connectedBody != null)
                {
                    // 硬吸附：移动锚点刚体到新方向的同半径处
                    Rigidbody anchorRb = fixedJoint.connectedBody;
                    if (anchorRb != null && anchorRb.isKinematic && anchorRb.gameObject.name.StartsWith("GrappleAnchor_"))
                    {
                        pendingAnchorPosition = newWorldAttach;
                        hasPendingAnchorPosition = true;
                    }
                }
                else
                {
                    attachPointLocalOnTarget = targetBody.transform.InverseTransformPoint(newWorldAttach);

                    // 同步 SpringJoint（若存在）
                    if (springJointOnTarget != null)
                    {
                        springJointOnTarget.anchor = attachPointLocalOnTarget;
                        springJointOnTarget.connectedAnchor = fireOrigin.position;
                    }
                    if (springJointOnPlayer != null)
                    {
                        springJointOnPlayer.connectedAnchor = attachPointLocalOnTarget;
                    }
                }
            }
        }

        // 滚轮调节远近（维护 desiredDistance）
        if (isGrappling && targetBody != null && useScrollForDistance)
        {
            float scroll;
#if ENABLE_INPUT_SYSTEM
            scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            scroll *= (1f / 120f); // Windows 一格常为 120
#else
            scroll = Input.mouseScrollDelta.y; // 正通常代表向前滚
#endif
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                Vector3 worldAttach = targetBody.transform.TransformPoint(attachPointLocalOnTarget);
                Vector3 refPos = (pullMode == GrapplePullMode.PullPlayerToTarget && playerRb != null)
                    ? playerRb.worldCenterOfMass : fireOrigin.position;
                Vector3 fromRef = worldAttach - refPos;
                float currentDist = Mathf.Max(fromRef.magnitude, 0.0001f);
                Vector3 dir = fromRef / currentDist;

                float newDist = Mathf.Clamp(currentDist - scroll * scrollSpeed, minDistance, maxDistanceScroll);
                desiredDistance = newDist;

                if (fixedJoint != null && fixedJoint.connectedBody != null)
                {
                    // 硬吸附：移动锚点保持新距离
                    Rigidbody anchorRb = fixedJoint.connectedBody;
                    Vector3 newAnchorPos = refPos + dir * newDist;
                    if (anchorRb != null && anchorRb.isKinematic && anchorRb.gameObject.name.StartsWith("GrappleAnchor_"))
                    {
                        pendingAnchorPosition = newAnchorPos;
                        hasPendingAnchorPosition = true;
                    }
                }

                // 稍微衰减速度，减少抖动（可选）
                targetBody.velocity *= 0.9f;
                targetBody.angularVelocity *= 0.9f;
            }
        }

        UpdateRopeVisual();
    }

    void FixedUpdate()
    {
        if (!isGrappling || targetBody == null) return;

        Vector3 attachWorldPoint = targetBody.transform.TransformPoint(attachPointLocalOnTarget);
        float distFromGun = (attachWorldPoint - fireOrigin.position).magnitude;

        // 自动靠近到 snap 距离则硬吸附
        if (useSnap && distFromGun <= snapDistance && fixedJoint == null)
            MakeFixedSnap();

        // 软拉力学（保持 desiredDistance）
        if (pullMode == GrapplePullMode.PullTargetToPlayer)
        {
            Vector3 refPos = fireOrigin.position;
            Vector3 dirFromRef = attachWorldPoint - refPos;
            float d = dirFromRef.magnitude;
            if (d > 0.0001f)
            {
                Vector3 dir = dirFromRef / d; // 从 ref 指向目标
                float error = d - desiredDistance; // >0 过远，需要拉近；<0 过近，需要推远
                Vector3 force = -dir * pullForce * Mathf.Clamp(error, -5f, 5f);
                force -= targetBody.velocity * pullDamping;
                targetBody.AddForceAtPosition(force, attachWorldPoint, ForceMode.Acceleration);
            }
        }
        else // 拉玩家到目标
        {
            if (playerRb != null)
            {
                Vector3 refPos = playerRb.worldCenterOfMass;
                Vector3 vec = attachWorldPoint - refPos;
                float d = vec.magnitude;
                if (d > 0.0001f)
                {
                    Vector3 dir = vec / d;
                    float error = d - desiredDistance;
                    Vector3 force = dir * pullForce * Mathf.Clamp(error, -5f, 5f);
                    force -= playerRb.velocity * pullDamping;
                    playerRb.AddForce(force, ForceMode.Acceleration);
                }
            }
        }

        // 在物理步中应用待移动的锚点位置（只移动我们创建的锚点刚体）
        if (hasPendingAnchorPosition && fixedJoint != null && fixedJoint.connectedBody != null)
        {
            Rigidbody anchorRb = fixedJoint.connectedBody;
            if (anchorRb != null && anchorRb.isKinematic && anchorRb.gameObject.name.StartsWith("GrappleAnchor_"))
            {
                anchorRb.MovePosition(pendingAnchorPosition);
            }
            hasPendingAnchorPosition = false;
        }

        // 在物理步中应用待旋转的锚点姿态（用于硬连下的 Q/E）
        if (hasPendingAnchorRotation && fixedJoint != null && fixedJoint.connectedBody != null)
        {
            Rigidbody anchorRb = fixedJoint.connectedBody;
            if (anchorRb != null && anchorRb.isKinematic && anchorRb.gameObject.name.StartsWith("GrappleAnchor_"))
            {
                anchorRb.MoveRotation(pendingAnchorRotation);
            }
            hasPendingAnchorRotation = false;
        }

        // 保持水平：移除目标俯仰和翻滚，仅保留世界Y朝向
        if (keepTargetLevel)
        {
            Vector3 e = targetBody.rotation.eulerAngles;
            Quaternion leveled = Quaternion.Euler(0f, e.y, 0f);
            targetBody.MoveRotation(Quaternion.Slerp(targetBody.rotation, leveled, 0.5f));
        }
    }

    void TryFire()
    {
        if (showDebugInfo)
            Debug.Log("GrapplingGun: TryFire called");
            
        Ray ray = new Ray(fireOrigin.position, fireOrigin.forward);
        bool hasHit;
        RaycastHit hit;
        // 临时检测所有层，方便调试
        hasHit = Physics.Raycast(ray, out hit, maxDistance);
        if (hasHit)
        {
            if (showDebugInfo)
                Debug.Log($"GrapplingGun: Hit {hit.collider.name}");
                
            // 只检测有Magnetic_Poles组件的物体
            // 修复：使用GetComponentInChildren向下搜索，因为Magnetic_Poles在子物体上
            var magneticPoles = hit.collider.GetComponentInChildren<Magnetic_Poles>();
            
            // 调试：检查搜索过程
            if (showDebugInfo)
            {
                Debug.Log($"GrapplingGun: Searching for Magnetic_Poles from {hit.collider.name}");
                
                // 检查所有子物体
                var allChildren = hit.collider.GetComponentsInChildren<Transform>();
                Debug.Log($"GrapplingGun: Found {allChildren.Length} child objects");
                foreach (var child in allChildren)
                {
                    Debug.Log($"GrapplingGun: - Child: {child.name} (Active: {child.gameObject.activeInHierarchy})");
                    var pole = child.GetComponent<Magnetic_Poles>();
                    if (pole != null)
                    {
                        Debug.Log($"GrapplingGun:   -> Has Magnetic_Poles component!");
                    }
                }
                
                var allPoles = hit.collider.GetComponentsInChildren<Magnetic_Poles>();
                Debug.Log($"GrapplingGun: Found {allPoles.Length} Magnetic_Poles components in children hierarchy");
                foreach (var pole in allPoles)
                {
                    Debug.Log($"GrapplingGun: - {pole.name} (Parent: {pole.transform.parent?.name})");
                }
            }
            
            if (magneticPoles != null)
            {
                if (showDebugInfo)
                    Debug.Log($"GrapplingGun: Found Magnetic_Poles on {magneticPoles.name}");
                    
                Rigidbody hitRb = hit.rigidbody;
                
                // 如果射线没有直接命中Rigidbody，尝试从Magnetic_Poles获取
                if (hitRb == null)
                {
                    hitRb = magneticPoles.GetComponent<Rigidbody>();
                    if (showDebugInfo)
                        Debug.Log($"GrapplingGun: Got Rigidbody from Magnetic_Poles: {hitRb != null}");
                }
                
                // 如果还是没有Rigidbody，尝试从父对象获取
                if (hitRb == null)
                {
                    var magnetBody = magneticPoles.GetComponentInParent<Magnet_Body>();
                    if (magnetBody != null)
                    {
                        hitRb = magnetBody.rb;
                        if (showDebugInfo)
                            Debug.Log($"GrapplingGun: Got Rigidbody from Magnet_Body: {hitRb != null}");
                    }
                }
                
                if (hitRb != null)
                {
                    if (showDebugInfo)
                        Debug.Log($"GrapplingGun: Starting grapple with {hitRb.name}");
                    StartGrapple(hitRb, hit.point);
                }
                else if (Time.time - lastNoRbWarnTime > noRbWarnCooldown)
                {
                    lastNoRbWarnTime = Time.time;
                    Debug.LogWarning("GrapplingGun: Hit a magnetic target but no Rigidbody was found. Please add a Rigidbody (with a non-trigger Collider) to the magnetic target.");
                }
            }
            else 
            {
                // 备用检测：直接搜索命中的GameObject
                var directPoles = hit.collider.GetComponent<Magnetic_Poles>();
                if (directPoles != null)
                {
                    if (showDebugInfo)
                        Debug.Log($"GrapplingGun: Found Magnetic_Poles directly on {hit.collider.name}");
                    
                    Rigidbody hitRb = hit.rigidbody;
                    if (hitRb == null)
                    {
                        var magnetBody = directPoles.GetComponentInParent<Magnet_Body>();
                        if (magnetBody != null)
                        {
                            hitRb = magnetBody.rb;
                            if (showDebugInfo)
                                Debug.Log($"GrapplingGun: Got Rigidbody from Magnet_Body: {hitRb != null}");
                        }
                    }
                    
                    if (hitRb != null)
                    {
                        if (showDebugInfo)
                            Debug.Log($"GrapplingGun: Starting grapple with {hitRb.name}");
                        StartGrapple(hitRb, hit.point);
                    }
                }
                else if (showDebugInfo)
                {
                    Debug.Log($"GrapplingGun: No Magnetic_Poles found on {hit.collider.name}, ignoring");
                }
            }
        }
    }

    void StartGrapple(Rigidbody targetRb, Vector3 hitPointWorld)
    {
        ReleaseGrapple();

        targetBody = targetRb;
        attachPointLocalOnTarget = targetBody.transform.InverseTransformPoint(hitPointWorld);
        isGrappling = true;

        // 初始化期望距离为当前参考点与附着点的距离
        Vector3 refPos = (pullMode == GrapplePullMode.PullPlayerToTarget && playerRb != null)
            ? playerRb.worldCenterOfMass : fireOrigin.position;
        desiredDistance = (hitPointWorld - refPos).magnitude;

        // 备份并根据设置调整目标物理属性，便于抬起与跟随
        prevTargetUseGravity = targetBody.useGravity;
        prevTargetMass = targetBody.mass;
        prevTargetDrag = targetBody.drag;
        prevTargetAngularDrag = targetBody.angularDrag;

        if (disableTargetGravityOnGrapple)
            targetBody.useGravity = false;
        if (reduceTargetMassOnGrapple && targetMassWhileGrappling > 0f)
            targetBody.mass = Mathf.Max(0.01f, targetMassWhileGrappling);
        if (extraDragWhileGrappling > 0f)
            targetBody.drag = prevTargetDrag + extraDragWhileGrappling;

        EnsureRigidbodySettings(targetBody);
        if (playerRb != null) EnsureRigidbodySettings(playerRb);

        // 建基础“软拉”约束
        if (pullMode == GrapplePullMode.PullTargetToPlayer)
        {
            springJointOnTarget = targetBody.gameObject.AddComponent<SpringJoint>();
            springJointOnTarget.autoConfigureConnectedAnchor = false;
            springJointOnTarget.connectedBody = null; // 世界点
            springJointOnTarget.anchor = attachPointLocalOnTarget;
            springJointOnTarget.connectedAnchor = fireOrigin.position;
            springJointOnTarget.spring = 0f; // 主要用 AddForce
            springJointOnTarget.damper = 0f;
            springJointOnTarget.maxDistance = maxDistance;
        }
        else
        {
            if (playerRb != null)
            {
                springJointOnPlayer = playerRb.gameObject.AddComponent<SpringJoint>();
                springJointOnPlayer.autoConfigureConnectedAnchor = false;
                springJointOnPlayer.connectedBody = targetBody;
                springJointOnPlayer.anchor = Vector3.zero;
                springJointOnPlayer.connectedAnchor = attachPointLocalOnTarget;
                springJointOnPlayer.spring = 0f;
                springJointOnPlayer.damper = 0f;
                springJointOnPlayer.maxDistance = maxDistance;
            }
        }

        // 视觉绳子
        lineRenderer.enabled = true;
    }

    void MakeFixedSnap()
    {
        if (targetBody == null) return;

        // 清理 spring
        if (springJointOnTarget != null) Destroy(springJointOnTarget);
        springJointOnTarget = null;
        if (springJointOnPlayer != null) Destroy(springJointOnPlayer);
        springJointOnPlayer = null;

        if (snapUseFixedJoint)
        {
            if (pullMode == GrapplePullMode.PullTargetToPlayer)
            {
                // 在枪口创建锚点
                GameObject anchor = new GameObject("GrappleAnchor_TargetToPlayer");
                anchor.transform.position = fireOrigin.position;
                anchor.transform.rotation = fireOrigin.rotation;
                var anchorRb = anchor.AddComponent<Rigidbody>();
                anchorRb.isKinematic = true;
                anchor.transform.SetParent(transform, true);

                fixedJoint = targetBody.gameObject.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = anchorRb;
                fixedJoint.breakForce = snapBreakForce;
                fixedJoint.breakTorque = snapBreakTorque;
            }
            else
            {
                if (playerRb == null) return;
                fixedJoint = playerRb.gameObject.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = targetBody;
                fixedJoint.breakForce = snapBreakForce;
                fixedJoint.breakTorque = snapBreakTorque;
            }
        }
        else
        {
            // 强 Spring 近似固定
            if (pullMode == GrapplePullMode.PullTargetToPlayer)
            {
                springJointOnTarget = targetBody.gameObject.AddComponent<SpringJoint>();
                springJointOnTarget.autoConfigureConnectedAnchor = false;
                springJointOnTarget.connectedBody = null;
                springJointOnTarget.anchor = attachPointLocalOnTarget;
                springJointOnTarget.connectedAnchor = fireOrigin.position;
                springJointOnTarget.spring = 10000f;
                springJointOnTarget.damper = 100f;
                springJointOnTarget.maxDistance = 0.01f;
            }
            else
            {
                if (playerRb != null)
                {
                    springJointOnPlayer = playerRb.gameObject.AddComponent<SpringJoint>();
                    springJointOnPlayer.autoConfigureConnectedAnchor = false;
                    springJointOnPlayer.connectedBody = targetBody;
                    springJointOnPlayer.anchor = Vector3.zero;
                    springJointOnPlayer.connectedAnchor = attachPointLocalOnTarget;
                    springJointOnPlayer.spring = 10000f;
                    springJointOnPlayer.damper = 100f;
                    springJointOnPlayer.maxDistance = 0.01f;
                }
            }
        }
    }

    // 仅取消硬链接，保持软连接与抓取状态
    void CancelFixedSnapToSoft()
    {
        if (fixedJoint != null)
        {
            var connected = fixedJoint.connectedBody;
            bool isAnchor = (connected != null && connected.isKinematic && connected.gameObject.name.StartsWith("GrappleAnchor_"));
            Destroy(fixedJoint);
            fixedJoint = null;
            if (isAnchor) Destroy(connected.gameObject);
        }

        // 软连已由现有逻辑维持（AddForce），无需额外重建
        // 若需要，可在此根据 attachPointLocalOnTarget 同步 springJoint，但当前版本软拉直接使用 AddForce
    }

    public void ReleaseGrapple()
    {
        isGrappling = false;
        lineRenderer.enabled = false;

        if (springJointOnTarget != null) Destroy(springJointOnTarget);
        springJointOnTarget = null;

        if (springJointOnPlayer != null) Destroy(springJointOnPlayer);
        springJointOnPlayer = null;

        if (fixedJoint != null)
        {
            var connected = fixedJoint.connectedBody;
            bool isAnchor = (connected != null && connected.isKinematic && connected.gameObject.name.StartsWith("GrappleAnchor_"));
            Destroy(fixedJoint);
            fixedJoint = null;
            if (isAnchor) Destroy(connected.gameObject);
        }

        if (targetBody != null)
        {
            // 恢复目标的物理属性
            targetBody.useGravity = prevTargetUseGravity;
            targetBody.mass = prevTargetMass;
            targetBody.drag = prevTargetDrag;
            targetBody.angularDrag = prevTargetAngularDrag;
        }

        targetBody = null;
    }

    void UpdateRopeVisual()
    {
        if (!lineRenderer.enabled) return;

        Vector3 start = fireOrigin.position;
        Vector3 end = (targetBody != null)
            ? targetBody.transform.TransformPoint(attachPointLocalOnTarget)
            : fireOrigin.position;

        // 插值平滑
        Vector3 prevEnd = lineRenderer.GetPosition(1);
        if (prevEnd == Vector3.zero) prevEnd = end;
        Vector3 currentEnd = Vector3.Lerp(prevEnd, end, Time.deltaTime * ropeLerpSpeed);

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, currentEnd);
    }

    void EnsureRigidbodySettings(Rigidbody rb)
    {
        if (rb == null) return;
        if (rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (rb.interpolation == RigidbodyInterpolation.None)
            rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnJointBreak(float breakForce)
    {
        ReleaseGrapple();
    }
}