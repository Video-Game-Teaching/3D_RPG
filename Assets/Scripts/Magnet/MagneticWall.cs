using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class MagneticWall : MonoBehaviour
{
    [Header("Activation")]
    public bool enableMagnet = true;
    public LayerMask magneticBodyLayer;     // 只吸这些层的物体（把你的磁性物体放到这个层）
    public float maxRange = 3.0f;           // 作用范围（从墙表面向外）
    
    [Header("Force & Damping")]
    public float pullStrength = 60f;        // 吸力系数
    public float damping = 4f;              // 阻尼（越大越稳）
    public float maxPullSpeed = 25f;        // 限制吸附速度

    [Header("Snap (贴合)")]
    public bool useSnap = true;
    public float snapDistance = 0.08f;      // 进入该距离触发贴合
    public float surfaceOffset = 0.01f;     // 贴合时离墙面的安全间隙，避免穿模
    public bool snapUseFixedJoint = true;   // 贴住后用关节“锁”住
    public float jointBreakForce = 1200f;   // 过大外力会断开
    public float jointBreakTorque = 1200f;

    [Header("Alignment")]
    public bool alignToWallNormal = true;   // 贴住时旋转让“面朝墙”
    public Vector3 localForwardOnObject = Vector3.forward; // 物体哪一个局部轴指向墙（例如 forward 朝墙）
    
    [Header("Physics Tweaks")]
    public bool setContinuousCollision = true; // 避免高速穿墙
    
    [Header("Debug")]
    public bool showDebugInfo = false; // 显示调试信息
    
    // 内部
    private Collider wallCol;
    private Rigidbody wallAnchorRb; // 作为关节的连接刚体（运动学）
    private readonly Collider[] overlapBuf = new Collider[64];

    void Awake()
    {
        wallCol = GetComponent<Collider>();
        if (wallCol == null)
        {
            Debug.LogError($"MagneticWall on {gameObject.name} requires a Collider component!");
            return;
        }
        wallCol.isTrigger = false; // 我们用 ClosestPoint，需要非触发器

        // 创建一个隐藏的"锚点刚体"，方便 FixedJoint 连接
        GameObject anchor = new GameObject(name + "_MagneticAnchor");
        anchor.transform.SetParent(transform, false);
        wallAnchorRb = anchor.AddComponent<Rigidbody>();
        wallAnchorRb.isKinematic = true;
        wallAnchorRb.useGravity = false;
        wallAnchorRb.detectCollisions = false;
    }

    void OnDestroy()
    {
        // 清理创建的锚点GameObject，避免内存泄漏
        if (wallAnchorRb != null && wallAnchorRb.gameObject != null)
        {
            DestroyImmediate(wallAnchorRb.gameObject);
        }
    }

    void FixedUpdate()
    {
        if (!enableMagnet || wallCol == null || wallAnchorRb == null) return;

        // 用墙的包围盒做一次体素检测，范围向外扩一圈
        Bounds b = wallCol.bounds;
        b.Expand(maxRange * 2f);

        int count = Physics.OverlapBoxNonAlloc(
            b.center, b.extents, overlapBuf, Quaternion.identity,
            ~0, QueryTriggerInteraction.Ignore  // 临时检测所有层
        );

        // 早期退出：如果没有检测到任何物体，直接返回
        if (count == 0) 
        {
            if (showDebugInfo)
                Debug.Log($"MagneticWall: No objects detected in range. LayerMask: {magneticBodyLayer.value}");
            return;
        }

        if (showDebugInfo)
            Debug.Log($"MagneticWall: Detected {count} objects in range");

        for (int i = 0; i < count; i++)
        {
            var col = overlapBuf[i];
            if (!col || col.attachedRigidbody == null) continue;

            Rigidbody rb = col.attachedRigidbody;

            // 你自己的标记：只对带 Magnet_Body 的刚体生效（可按需放松）
            var magnetBody = rb.GetComponent<Magnet_Body>();
            if (!magnetBody) 
            {
                if (showDebugInfo)
                    Debug.Log($"MagneticWall: {rb.name} has no Magnet_Body component, skipping");
                continue;
            }

            // 最近点（世界坐标）
            Vector3 closest = wallCol.ClosestPoint(rb.worldCenterOfMass);
            Vector3 toSurface = closest - rb.worldCenterOfMass;

            // 超出范围就忽略
            float dist = toSurface.magnitude;
            if (dist > maxRange) continue;

            // 如果目前已经有 FixedJoint 贴在本墙，跳过计算（避免重复）
            if (HasJointToThisWall(rb)) continue;

            // === 磁力（朝“最近点 + 法线外推 surfaceOffset”） ===
            Vector3 wallNormal = EstimateWallNormal(closest);
            Vector3 targetPos = closest + wallNormal * surfaceOffset;
            Vector3 dir = (targetPos - rb.worldCenterOfMass);
            float d = dir.magnitude;

            // 速度限制（避免抖动）
            Vector3 desiredVel = dir.normalized * Mathf.Min(maxPullSpeed, d * pullStrength);
            Vector3 deltaVel = desiredVel - rb.velocity;
            // 改进的力计算：保持足够的吸力强度
            float effectiveStrength = pullStrength - damping;
            // 确保最小吸力，避免完全失去吸力
            effectiveStrength = Mathf.Max(effectiveStrength, pullStrength * 0.2f);
            Vector3 force = deltaVel * effectiveStrength;

            rb.AddForce(force, ForceMode.Acceleration);

            // 调试信息
            if (showDebugInfo)
            {
                Debug.Log($"MagneticWall: Applying force {force.magnitude:F2} to {rb.name}, distance: {d:F3}, effectiveStrength: {effectiveStrength:F2}");
            }

            // 碰撞检测模式尽量提升，减少穿模
            if (setContinuousCollision && rb.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic)
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // === 贴合判定 ===
            if (useSnap && d <= snapDistance)
            {
                // 对齐姿态（可选）
                if (alignToWallNormal)
                {
                    // 让物体的某个局部轴朝向墙的反向法线（物体朝墙）
                    Vector3 desiredForward = -wallNormal;
                    // 把 localForwardOnObject 转成世界向量
                    Vector3 currentForward = rb.transform.TransformDirection(localForwardOnObject).normalized;
                    Quaternion rot = Quaternion.FromToRotation(currentForward, desiredForward) * rb.rotation;
                    rb.MoveRotation(rot);
                }

                // 把物体拉到表面安全位置
                rb.MovePosition(targetPos);

                if (snapUseFixedJoint)
                {
                    var fj = rb.gameObject.AddComponent<FixedJoint>();
                    fj.connectedBody = wallAnchorRb;
                    fj.breakForce = jointBreakForce;
                    fj.breakTorque = jointBreakTorque;
                    fj.enableCollision = false;
                }
                else
                {
                    // 不用关节时，悄悄“冻结”朝墙方向的速度以稳定贴合
                    Vector3 v = rb.velocity;
                    float vn = Vector3.Dot(v, wallNormal);
                    if (vn > 0f) v -= wallNormal * vn;
                    rb.velocity = v;
                }
            }
        }
    }

    bool HasJointToThisWall(Rigidbody rb)
    {
        var joints = rb.GetComponents<Joint>();
        foreach (var j in joints)
        {
            // 检查所有类型的关节，不仅仅是FixedJoint
            if (j != null && j.connectedBody == wallAnchorRb)
                return true;
        }
        return false;
    }

    // 估计墙面的法线：优先用 Raycast 法线，不行就用碰撞器朝向估计
    Vector3 EstimateWallNormal(Vector3 pointOnWall)
    {
        Vector3 fallback = transform.forward; // 给个默认
        // 从墙的外侧往里射线，尝试拿到准确法线
        Vector3[] probes = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
        foreach (var p in probes)
        {
            Vector3 origin = pointOnWall + p * 0.2f;
            // 使用更精确的LayerMask，检测所有非触发器层
            if (Physics.Raycast(origin, -p, out RaycastHit hit, 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == wallCol) return hit.normal.normalized;
            }
        }
        // 对于 BoxCollider，取最近面的法线
        if (wallCol is BoxCollider)
        {
            Vector3 local = transform.InverseTransformPoint(pointOnWall);
            var box = wallCol as BoxCollider;
            Vector3 half = box.size * 0.5f;
            Vector3 n = Vector3.zero;
            float dx = Mathf.Abs(half.x - Mathf.Abs(local.x));
            float dy = Mathf.Abs(half.y - Mathf.Abs(local.y));
            float dz = Mathf.Abs(half.z - Mathf.Abs(local.z));
            float m = Mathf.Min(dx, Mathf.Min(dy, dz));
            if (m == dx) n = new Vector3(Mathf.Sign(local.x), 0, 0);
            else if (m == dy) n = new Vector3(0, Mathf.Sign(local.y), 0);
            else n = new Vector3(0, 0, Mathf.Sign(local.z));
            return transform.TransformDirection(n).normalized;
        }
        return fallback.normalized;
    }

    void OnDrawGizmosSelected()
    {
        if (!GetComponent<Collider>()) return;
        Gizmos.matrix = Matrix4x4.identity;
        Bounds b = GetComponent<Collider>().bounds;
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.12f);
        Gizmos.DrawCube(b.center, b.size + Vector3.one * (maxRange * 2f));
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f);
        Gizmos.DrawWireCube(b.center, b.size + Vector3.one * (maxRange * 2f));
    }

}
