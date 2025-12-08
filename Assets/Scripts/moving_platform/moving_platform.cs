using UnityEngine;

public class moving_platform : MonoBehaviour
{
    [Header("Platform Settings")]
    public Transform[] waypoints;
    public float speed = 2f;
    public float[] dwellSeconds;
    public bool pingPong = true;
    public AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

    private Rigidbody _rb;
    private int currIndex = 0;
    private int dir = 1;
    private float tAlong = 0f;
    private Vector3 prevPos;
    private Quaternion prevRot;
    private Vector3 linearVel;
    private Vector3 angularVel;
    private float dwellTimer = 0f;

    public Vector3 DeltaPosition { get; private set; }
    public Vector3 LinearVelocity => linearVel;
    public Vector3 AngularVelocity => angularVel;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        prevPos = transform.position;
        prevRot = transform.rotation;
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        if (dwellTimer > 0f)
        {
            dwellTimer -= Time.deltaTime;
            ApplyPose(prevPos, prevRot);
            return;
        }

        var a = waypoints[currIndex].position;
        var b = waypoints[(currIndex + dir + waypoints.Length) % waypoints.Length].position;
        float segLen = Vector3.Distance(a, b);
        if (segLen < 1e-4f)
        {
            AdvanceNode();
            return;
        }

        // Move along segment with easing
        float dtNorm = (speed * Time.deltaTime) / segLen;
        tAlong = Mathf.Clamp01(tAlong + dtNorm);
        float eased = ease.Evaluate(tAlong);

        Vector3 targetPos = Vector3.LerpUnclamped(a, b, eased);
        Quaternion targetRot = transform.rotation;

        ApplyPose(targetPos, targetRot);

        if (tAlong >= 1f - 1e-5f)
            AdvanceNode();
    }

    void AdvanceNode()
    {
        tAlong = 0f;
        currIndex += dir;
        if (currIndex >= waypoints.Length - 1 || currIndex <= 0)
        {
            if (pingPong)
                dir *= -1;
            else
                currIndex = (currIndex + waypoints.Length) % waypoints.Length;
        }
        if (dwellSeconds != null && dwellSeconds.Length > 0)
        {
            int idx = Mathf.Clamp(currIndex, 0, dwellSeconds.Length - 1);
            dwellTimer = Mathf.Max(0f, dwellSeconds[idx]);
        }
    }

    void ApplyPose(Vector3 pos, Quaternion rot)
    {
        _rb.MovePosition(pos);
        _rb.MoveRotation(rot);

        // Calculate velocity
        DeltaPosition = pos - prevPos;
        linearVel = DeltaPosition / Time.deltaTime;
        
        // Clamp linear velocity
        float maxLinearSpeed = 50f;
        if (linearVel.magnitude > maxLinearSpeed)
        {
            linearVel = linearVel.normalized * maxLinearSpeed;
        }

        // Calculate angular velocity
        Quaternion deltaRotation = rot * Quaternion.Inverse(prevRot);
        
        if (deltaRotation.w < 0.9999f)
        {
            deltaRotation.ToAngleAxis(out float angleDeg, out Vector3 axis);
            if (!float.IsNaN(axis.x)
                && !float.IsNaN(axis.y)
                && !float.IsNaN(axis.z)
                && axis.magnitude > 0.001f)
            {
                float angleRad = Mathf.Deg2Rad * angleDeg;
                angularVel = axis.normalized * (angleRad / Time.deltaTime);
                
                // Clamp angular velocity
                float maxAngularSpeed = 20f;
                if (angularVel.magnitude > maxAngularSpeed)
                {
                    angularVel = angularVel.normalized * maxAngularSpeed;
                }
            }
            else
            {
                angularVel = Vector3.zero;
            }
        }
        else
        {
            angularVel = Vector3.zero;
        }

        prevPos = pos;
        prevRot = rot;
    }

    // Calculate surface velocity (linear + angular velocity)
    public Vector3 GetSurfaceVelocity(Vector3 worldPoint)
    {
        Vector3 r = worldPoint - transform.position;
        Vector3 tangentialVelocity = Vector3.Cross(AngularVelocity, r);
        Vector3 surfaceVelocity = LinearVelocity + tangentialVelocity;

        // Clamp surface velocity
        float maxSurfaceSpeed = 30f;
        if (surfaceVelocity.magnitude > maxSurfaceSpeed)
        {
            surfaceVelocity = surfaceVelocity.normalized * maxSurfaceSpeed;
        }

        return surfaceVelocity;
    }
}
