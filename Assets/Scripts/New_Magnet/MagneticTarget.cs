using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class MagneticTarget : MonoBehaviour
{
    [Header("Magnet")]
    public bool isMagnetic = true;       // Toggle
    [Tooltip("Only different types attract each other, e.g., 0 and 1.")]
    public int typeId = 0;               // Type identifier to determine attraction/repulsion
    public float strength = 1f;          // Strength factor (higher => stronger pull)
    public float range = 20f;            // Effective range (meters)

    [Header("Snap")]
    public bool useSnap = true;          // Stick together when close
    public float snapDistance = 0.5f;    // Stick if within this distance
    public float breakForce = 1500f;     // FixedJoint break force
    public float breakTorque = 1500f;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public FixedJoint jointToOther;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false; // Must be a dynamic rigidbody
    }

    void OnEnable()  => MagnetSolver.Register(this);
    void OnDisable() => MagnetSolver.Unregister(this);
}