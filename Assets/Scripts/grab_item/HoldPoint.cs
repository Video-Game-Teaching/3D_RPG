using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldPoint : MonoBehaviour
{
    private Transform holdPointTransform;
    private Transform cameraTransform;

    public Vector3 offset = new Vector3(0f, -0.6f, 1.5f);
    public float minimumYAngle = 30f;  // 与相机“正下方”(local down) 的最小夹角（度）

    void Start()
    {
        holdPointTransform = transform;
        cameraTransform = Camera.main.transform;
    }
    void Update()
    {
        // 1) 仍然用你的 offset 计算期望位置（不改 offset）
        Vector3 desiredPos = cameraTransform.position + cameraTransform.TransformVector(offset);
        Vector3 dir = desiredPos - cameraTransform.position;   // world-space 从相机到holdpoint的向量

        // 2) 和“世界向下”比较角度（可随相机俯仰变化）
        Vector3 worldDown = Vector3.down;                       // 重力方向/世界下方
        float angle = Vector3.Angle(dir, worldDown);
        // Debug.Log("Angle to WORLD down: " + angle);

        // 3) 若低于阈值，则把方向旋到恰好 minimumYAngle（保持距离不变）
        if (angle < minimumYAngle)
        {
            Vector3 axis = Vector3.Cross(worldDown, dir);
            if (axis.sqrMagnitude < 1e-6f) axis = cameraTransform.right; // 防共线退化
            axis.Normalize();

            Quaternion q = Quaternion.AngleAxis(minimumYAngle, axis);
            Vector3 newDir = (q * worldDown).normalized;

            dir = newDir * dir.magnitude;                       // 只改方向，不改距离
        }

        // 4) 应用最终位置与旋转（旋转仍跟随相机）
        holdPointTransform.position = cameraTransform.position + dir;
        holdPointTransform.rotation = cameraTransform.rotation;
    }

}