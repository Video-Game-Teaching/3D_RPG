using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    private Camera activeCamera;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        activeCamera = Camera.main;
    }

    // 平滑切换到目标相机
    public IEnumerator SmoothSwitchToCamera(Camera targetCam, float duration)
    {
        if (targetCam == null) yield break;

        Camera fromCam = activeCamera;
        if (fromCam == targetCam) yield break;

        // 确保目标相机开启、原相机也存在
        targetCam.enabled = true;
        if (fromCam != null) fromCam.enabled = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = t / duration;
            // 简单线性混合FOV（也可以用更复杂blend）
            targetCam.fieldOfView = Mathf.Lerp(fromCam.fieldOfView, targetCam.fieldOfView, alpha);
            yield return null;
        }

        // 最后启用目标相机、关闭原相机
        if (fromCam != null) fromCam.enabled = false;
        targetCam.enabled = true;
        activeCamera = targetCam;
    }
}
