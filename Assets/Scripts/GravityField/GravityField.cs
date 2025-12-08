using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
public class GravityField : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float gravityAccel = 25f;
    
    [Header("Rotation Settings")]
    public bool rotatePlayer = true;
    public float rotationDuration = 0.25f;
    public bool freezePlayerDuringRotation = true;
    
    [Header("Transition Delay")]
    [Tooltip("进入重力场后等待多久才切换重力")]
    public float enterDelayTime = 0.25f;
    [Tooltip("离开重力场后等待多久才恢复重力")]
    public float exitDelayTime = 0.25f;
    
    [Header("Exit Behavior")]
    public bool restoreOnExit = false;
    
    private Vector3 _previousGravityDir;
    private Coroutine _transitionCoroutine;
    private bool _playerInside = false;
    private float _enterTime;
    private float _exitTime;
    private bool _gravityActivated = false; // 当前是否已激活重力场

    // 重力方向基于物体的 forward 轴
    private Vector3 gravityDirection => transform.forward;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        _playerInside = true;
        _enterTime = Time.time;
        
        // 停止之前的过渡
        StopTransition();
        
        // 保存当前重力方向用于退出时恢复
        if (GravityManager.Instance)
            _previousGravityDir = GravityManager.Instance.GravityDir;
        
        // 启动延迟确认协程
        _transitionCoroutine = StartCoroutine(WaitAndEnter(other.transform.root));
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        _playerInside = false;
        _exitTime = Time.time;
        
        // 如果还没有激活重力，直接取消
        if (!_gravityActivated)
        {
            StopTransition();
            return;
        }
        
        // 如果需要恢复，启动延迟确认协程
        if (restoreOnExit)
        {
            StopTransition();
            _transitionCoroutine = StartCoroutine(WaitAndExit(other.transform.root));
        }
    }

    private void StopTransition()
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }
    }

    /// <summary>
    /// 等待确认进入：在重力场内持续 enterDelayTime 后才切换重力
    /// </summary>
    private System.Collections.IEnumerator WaitAndEnter(Transform playerRoot)
    {
        float startTime = _enterTime;
        
        // 等待延迟时间
        while (Time.time - startTime < enterDelayTime)
        {
            // 如果在等待期间玩家离开了，取消切换
            if (!_playerInside)
            {
                yield break;
            }
            yield return null;
        }
        
        // 确认玩家仍在场内，执行重力切换
        if (_playerInside)
        {
            _gravityActivated = true;
            yield return TransitionGravity(playerRoot, gravityDirection);
        }
        
        _transitionCoroutine = null;
    }

    /// <summary>
    /// 等待确认退出：在重力场外持续 exitDelayTime 后才恢复重力
    /// </summary>
    private System.Collections.IEnumerator WaitAndExit(Transform playerRoot)
    {
        float startTime = _exitTime;
        
        // 等待延迟时间
        while (Time.time - startTime < exitDelayTime)
        {
            // 如果在等待期间玩家重新进入了，取消恢复
            if (_playerInside)
            {
                yield break;
            }
            yield return null;
        }
        
        // 确认玩家仍在场外，执行重力恢复
        if (!_playerInside)
        {
            _gravityActivated = false;
            yield return TransitionGravity(playerRoot, _previousGravityDir);
        }
        
        _transitionCoroutine = null;
    }

    private System.Collections.IEnumerator TransitionGravity(Transform playerRoot, Vector3 newGravityDir)
    {
        if (!GravityManager.Instance) yield break;

        var fpc = playerRoot.GetComponentInChildren<StarterAssets.FirstPersonController>();
        
        // 冻结玩家
        if (freezePlayerDuringRotation && fpc)
            fpc.SetMovementFrozen(true);
        
        // 更新重力
        GravityManager.Instance.GravityAccel = gravityAccel;
        GravityManager.Instance.SetGravityDirection(newGravityDir);
        
        // 旋转玩家
        if (rotatePlayer)
            yield return RotatePlayer(playerRoot, -newGravityDir.normalized, fpc);
        
        // 解冻玩家
        if (freezePlayerDuringRotation && fpc)
            fpc.SetMovementFrozen(false);
    }

    private System.Collections.IEnumerator RotatePlayer(
        Transform playerRoot,
        Vector3 targetUp,
        StarterAssets.FirstPersonController fpc
    )
    {
        // 暂停自动对齐
        if (fpc) fpc.SetAutoAlignEnabled(false);
        
        var startRot = playerRoot.rotation;
        var targetRot = Quaternion.FromToRotation(playerRoot.up, targetUp) * startRot;
        
        // 确定旋转轴心点（世界空间）
        var controller = playerRoot.GetComponentInChildren<CharacterController>();
        Vector3 pivotWorld;
        
        if (controller != null)
        {
            // 使用 CharacterController 的世界中心作为轴心
            pivotWorld = controller.transform.TransformPoint(controller.center);
        }
        else
        {
            // 默认使用玩家位置向上1米
            pivotWorld = playerRoot.position + playerRoot.up * 1f;
        }
        
        // 计算从轴心点到玩家根的初始偏移向量
        Vector3 offsetFromPivot = playerRoot.position - pivotWorld;
        
        // 计算旋转角度（从当前up到目标up）
        Quaternion deltaRotation = Quaternion.FromToRotation(playerRoot.up, targetUp);
        
        // 平滑旋转
        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            
            // 插值旋转
            Quaternion currentRot = Quaternion.Slerp(startRot, targetRot, t);
            Quaternion currentDelta = Quaternion.Slerp(Quaternion.identity, deltaRotation, t);
            
            // 旋转偏移向量
            Vector3 rotatedOffset = currentDelta * offsetFromPivot;
            
            // 设置新的位置和旋转
            playerRoot.rotation = currentRot;
            playerRoot.position = pivotWorld + rotatedOffset;
            
            yield return null;
        }
        
        // 确保最终精确对齐
        playerRoot.rotation = targetRot;
        playerRoot.position = pivotWorld + deltaRotation * offsetFromPivot;
        
        // 恢复自动对齐
        if (fpc) fpc.SetAutoAlignEnabled(true);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = _gravityActivated ? Color.cyan : Color.green;
        Handles.ArrowHandleCap(
            0,
            transform.position,
            Quaternion.LookRotation(gravityDirection),
            2f,
            EventType.Repaint
        );
        
        // 显示激活状态
        if (_gravityActivated)
        {
            Handles.color = new Color(0, 1, 1, 0.1f);
            var bounds = GetComponent<Collider>().bounds;
            Handles.DrawWireCube(bounds.center, bounds.size);
        }
    }
#endif
}
