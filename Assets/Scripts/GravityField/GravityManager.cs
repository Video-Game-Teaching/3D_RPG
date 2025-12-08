using UnityEngine;
using System;

public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance { get; private set; }

    [Tooltip("重力指向（世界向量）")]
    public Vector3 GravityDir = Vector3.down;
    public float GravityAccel = 25f;

    public event Action<Vector3> OnGravityChanged;

    private void Awake()
    {
        Instance = this;
        GravityDir = GravityDir.normalized;
    }

    public void SetGravityDirection(Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        dir.Normalize();
        if (dir == GravityDir) return;
        GravityDir = dir;
        OnGravityChanged?.Invoke(dir);
    }
}
