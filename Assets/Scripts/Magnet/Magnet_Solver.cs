using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pairwise magnetic solver:
/// - Computes forces between magnetic poles (1/r^p falloff).
/// - Optional damping and near-distance softening for stability.
/// - Optional "horizontal only" force (good near ground).
/// - Optional "center-force only" to avoid torques (apply net force at center of mass).
/// - Optional snap: when close enough, attach with FixedJoint or keep a small pulling force.
/// </summary>
public class Magnet_Solver : MonoBehaviour
{
    // Global registries (filled by Magnet_Body / Magnetic_Poles on enable/disable)
    static readonly List<Magnet_Body> Bodies = new List<Magnet_Body>();
    static readonly List<Magnetic_Poles> Poles = new List<Magnetic_Poles>();

    // ---------- Snap / Stick ----------
    [Header("Snap (stick together)")]
    [Tooltip("Enable snapping when two poles are close enough.")]
    public bool useSnap = true;

    [Tooltip("If distance < snapDistance (meters), we stick them.")]
    public float snapDistance = 0.06f;

    [Tooltip("If true, use FixedJoint to hard-stick; otherwise apply a small constant pulling force.")]
    public bool snapUseFixedJoint = true;

    [Tooltip("Constant pulling force used in soft-snap mode.")]
    public float snapPullForce = 15f;

    [Tooltip("Break force for the FixedJoint (hard-snap mode).")]
    public float snapBreakForce = 800f;

    [Tooltip("Break torque for the FixedJoint (hard-snap mode).")]
    public float snapBreakTorque = 800f;

    // ---------- Stability ----------
    [Header("Stability")]
    [Tooltip("Secondary softening radius near contact (meters). Reduces force when very close.")]
    public float nearRadius = 0.25f;

    [Tooltip("Relative velocity damping coefficient along the line of action (2~10 is typical).")]
    public float dampingK = 5f;

    [Tooltip("Project force onto the horizontal plane (remove vertical component).")]
    public bool horizontalOnly = false;

    [Tooltip("Apply only net force at the rigidbody center (no torque).")]
    public bool centerForceOnly = false;

    // ---------- Global tuning ----------
    [Header("Global Tuning")]
    [Tooltip("Distance exponent p in 1/r^p (2 = inverse-square).")]
    public float distancePower = 2f;

    [Tooltip("Global constant K for magnetic strength.")]
    public float globalK = 5f;

    [Tooltip("Cap the final force magnitude per pole-pair (0 = unlimited).")]
    public float maxForcePerPair = 200f;

    [Tooltip("Use ForceMode.Acceleration (mass-independent acceleration).")]
    public bool useAccelerationMode = false;

    // ---- Registration API (used by Magnet_Body / Magnetic_Poles) ----
    public static void RegisterBody(Magnet_Body b) { if (b && !Bodies.Contains(b)) Bodies.Add(b); }
    public static void UnregisterBody(Magnet_Body b) { Bodies.Remove(b); }
    public static void RegisterPole(Magnetic_Poles p) { if (p && !Poles.Contains(p)) Poles.Add(p); }
    public static void UnregisterPole(Magnetic_Poles p) { Poles.Remove(p); }

    // Check whether two rigidbodies are already connected by any Joint
    bool HasJointBetween(Rigidbody ra, Rigidbody rb)
    {
        foreach (var j in ra.GetComponents<Joint>()) if (j && j.connectedBody == rb) return true;
        foreach (var j in rb.GetComponents<Joint>()) if (j && j.connectedBody == ra) return true;
        return false;
    }

    // Accumulator for "centerForceOnly" mode
    readonly Dictionary<Magnet_Body, Vector3> _forceSum = new Dictionary<Magnet_Body, Vector3>();

    void FixedUpdate()
    {
        // Clear accumulator at the start of each physics step (prevents stale forces).
        if (centerForceOnly) _forceSum.Clear();

        int n = Poles.Count;
        if (n < 2) return;

        for (int i = 0; i < n - 1; i++)
        {
            var a = Poles[i];
            if (!a || !a.isActiveAndEnabled || a.body == null || a.body.rb == null) continue;

            for (int j = i + 1; j < n; j++)
            {
                var b = Poles[j];
                if (!b || !b.isActiveAndEnabled || b.body == null || b.body.rb == null) continue;
                if (!a.CanInteractWith(b)) continue;

                Vector3 pa = a.transform.position;
                Vector3 pb = b.transform.position;
                Vector3 ab = pb - pa;
                float r = ab.magnitude;
                if (r < 1e-6f) continue;

                // Primary softening to avoid singularity when very close
                float eps = Mathf.Max(a.softening, b.softening, 1e-4f);
                float rSoft = Mathf.Max(r, eps);

                // Polarity: same sign -> repulsion; opposite sign -> attraction
                float sign = Mathf.Sign(a.polarity * b.polarity);
                Vector3 dir = ab.normalized * -sign; // attraction direction

                // Base magnitude: K * s1*s2 / r^p
                float pairStrength = a.strength * b.strength;
                float denom = Mathf.Pow(rSoft, distancePower);
                float mag = (denom > 0f) ? (globalK * pairStrength / denom) : 0f;

                // Secondary softening near contact (smoothly reduce magnitude when r << nearRadius)
                float nr = Mathf.Max(nearRadius, 1e-4f);
                float t = Mathf.Clamp01(r / nr);
                float softFalloff = Mathf.SmoothStep(0.2f, 1f, t); // 0.2..1.0
                mag *= softFalloff;

                // Relative-velocity damping along the line of action (prevents "springy" overshoot)
                Vector3 vA = a.body.rb.GetPointVelocity(pa);
                Vector3 vB = b.body.rb.GetPointVelocity(pb);
                float vRel = Vector3.Dot(vB - vA, dir);
                Vector3 dampingF = -dampingK * vRel * dir;

                // Compose final force vector for this pole-pair
                Vector3 F = dir * mag + dampingF;

                // Optional: keep only horizontal component (good near ground to avoid pop-ups)
                if (horizontalOnly)
                    F = Vector3.ProjectOnPlane(F, Vector3.up);

                // Limit the final force magnitude (after projection and damping)
                if (maxForcePerPair > 0f && F.magnitude > maxForcePerPair)
                    F = F.normalized * maxForcePerPair;

                // Snap logic: when very close, either join by FixedJoint or keep a small pulling force
                if (useSnap && r < snapDistance)
                {
                    if (snapUseFixedJoint)
                    {
                        if (!HasJointBetween(a.body.rb, b.body.rb))
                        {
                            var jnt = a.body.gameObject.AddComponent<FixedJoint>();
                            jnt.connectedBody = b.body.rb;
                            jnt.breakForce = snapBreakForce;
                            jnt.breakTorque = snapBreakTorque;
                        }
                        // Once snapped, skip applying magnetic force for this pair to avoid jitter.
                        continue;
                    }
                    else
                    {
                        // Soft snap: use a constant small pulling force in the attraction direction.
                        F = dir * snapPullForce;
                        if (horizontalOnly)
                            F = Vector3.ProjectOnPlane(F, Vector3.up);
                    }
                }

                // Apply: either accumulate net force at center (no torque), or apply at pole positions (with torque)
                if (centerForceOnly)
                {
                    AccumulateForce(a.body, F);
                    AccumulateForce(b.body, -F);
                }
                else
                {
                    if (useAccelerationMode)
                    {
                        // ForceMode.Acceleration expects acceleration (mass-independent). Do NOT divide by mass.
                        a.body.rb.AddForceAtPosition( F, pa, ForceMode.Acceleration);
                        b.body.rb.AddForceAtPosition(-F, pb, ForceMode.Acceleration);
                    }
                    else
                    {
                        a.body.rb.AddForceAtPosition( F, pa, ForceMode.Force);
                        b.body.rb.AddForceAtPosition(-F, pb, ForceMode.Force);
                    }
                }
            }
        }

        // Flush accumulated forces in center-force mode
        if (centerForceOnly && _forceSum.Count > 0)
        {
            foreach (var kv in _forceSum)
            {
                var body = kv.Key;
                var F = kv.Value;
                if (useAccelerationMode)
                    body.rb.AddForce(F, ForceMode.Acceleration); // do NOT divide by mass
                else
                    body.rb.AddForce(F, ForceMode.Force);
            }
            _forceSum.Clear();
        }
    }

    void AccumulateForce(Magnet_Body b, Vector3 F)
    {
        if (!_forceSum.ContainsKey(b)) _forceSum[b] = Vector3.zero;
        _forceSum[b] += F;
    }
}