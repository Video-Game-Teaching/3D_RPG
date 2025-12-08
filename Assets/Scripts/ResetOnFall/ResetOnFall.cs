using UnityEngine;

public class ResetOnFall : MonoBehaviour
{
    // 模式枚举
    public enum ResetMode
    {
        AbsoluteThreshold,  // 绝对阈值模式（使用minY）
        RelativeToStart     // 相对起始位置模式（使用relativeY）
    }

    // 初始位置 / 初始朝向
    private Vector3 _startPos;
    private Quaternion _startRot;

    [Header("重置模式")]
    [Tooltip("选择重置触发模式")]
    public ResetMode resetMode = ResetMode.AbsoluteThreshold;

    [Header("绝对阈值模式设置")]
    [Tooltip("绝对阈值模式：如果物体的 y 坐标低于这个值，就触发重置")]
    public float minY = -5f;          // 最低高度阈值（threshold）

    [Header("相对模式设置")]
    [Tooltip("相对模式：如果物体 y 坐标低于起始位置多少距离时触发重置")]
    public float relativeY = 10f;     // 相对起始位置的下落距离

    [Header("通用设置")]
    [Tooltip("需要持续低于阈值多久才触发重置（秒）")]
    public float confirmTime = 3f;    // 确认时间（秒）

    [Tooltip("消失多久后回到原位")]
    public float respawnDelay = 0.5f; // 重生延迟（秒）

    private bool _isResetting = false;
    private bool _isBelowThreshold = false;  // 是否低于阈值
    private float _belowThresholdTimer = 0f; // 低于阈值的计时器
    private Rigidbody _rb;
    private Renderer[] _renderers;
    private Collider[] _colliders;

    private void Awake()
    {
        // 记录开局的位置和旋转
        _startPos = transform.position;
        _startRot = transform.rotation;

        _rb = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (_isResetting)
            return;

        bool isBelowThreshold = false;

        // 根据模式判断是否低于阈值
        switch (resetMode)
        {
            case ResetMode.AbsoluteThreshold:
                // 绝对阈值模式：判断 y 坐标是否低于 minY
                isBelowThreshold = transform.position.y < minY;
                break;

            case ResetMode.RelativeToStart:
                // 相对模式：判断是否低于起始位置 relativeY 距离
                isBelowThreshold = transform.position.y < (_startPos.y - relativeY);
                break;
        }

        // 如果当前低于阈值
        if (isBelowThreshold)
        {
            // 如果之前不在阈值以下，开始计时
            if (!_isBelowThreshold)
            {
                _isBelowThreshold = true;
                _belowThresholdTimer = 0f;
            }

            // 累加计时器
            _belowThresholdTimer += Time.deltaTime;

            // 如果持续时间超过确认时间，触发重置
            if (_belowThresholdTimer >= confirmTime)
            {
                StartCoroutine(Respawn());
            }
        }
        else
        {
            // 如果回到阈值以上，重置计时器
            if (_isBelowThreshold)
            {
                _isBelowThreshold = false;
                _belowThresholdTimer = 0f;
            }
        }
    }

    private System.Collections.IEnumerator Respawn()
    {
        _isResetting = true;
        _isBelowThreshold = false;
        _belowThresholdTimer = 0f;

        // 禁用渲染器和碰撞器来让物体"消失"
        SetObjectVisible(false);

        // 如果有刚体，暂时禁用物理模拟
        if (_rb != null)
        {
            _rb.isKinematic = true;
        }

        yield return new WaitForSeconds(respawnDelay);

        // 回到初始位置和朝向
        transform.position = _startPos;
        transform.rotation = _startRot;

        // 如果有刚体，先恢复物理模拟再清空速度
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        // 重新显示物体
        SetObjectVisible(true);

        _isResetting = false;
    }

    private void SetObjectVisible(bool visible)
    {
        // 启用/禁用所有渲染器
        foreach (var renderer in _renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }

        // 启用/禁用所有碰撞器
        foreach (var collider in _colliders)
        {
            if (collider != null)
                collider.enabled = visible;
        }
    }
}
