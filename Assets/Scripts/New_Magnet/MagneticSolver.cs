using System.Collections.Generic;
using UnityEngine;

public class MagnetSolver : MonoBehaviour
{
    static readonly List<MagneticTarget> targets = new List<MagneticTarget>();

    [Header("Global Tuning")]
    public float k = 20f;                // Global coefficient
    public float minDistance = 0.05f;    // Avoid singularity
    public float maxForcePerPair = 200f; // Max force per pair

    public static void Register(MagneticTarget t)
    {
        if (t != null && !targets.Contains(t)) targets.Add(t);
    }
    public static void Unregister(MagneticTarget t)
    {
        if (t != null) targets.Remove(t);
    }

    void FixedUpdate()
    {
        int n = targets.Count;
        for (int i = 0; i < n; i++)
        {
            var a = targets[i];
            if (a == null || !a.isMagnetic || a.rb == null) continue;

            for (int j = i + 1; j < n; j++)
            {
                var b = targets[j];
                if (b == null || !b.isMagnetic || b.rb == null) continue;

                Vector3 pa = a.rb.worldCenterOfMass;
                Vector3 pb = b.rb.worldCenterOfMass;
                Vector3 ab = pb - pa;
                float dist = ab.magnitude;

                // Effective range: use the smaller range of the pair
                float effectiveRange = Mathf.Min(a.range, b.range);
                if (dist > effectiveRange) continue;

                float safeDist = Mathf.Max(dist, minDistance);
                Vector3 dirAB = ab / safeDist; // a -> b

                // Key rule: same type repels, different types attract
                // same typeId => repel (negative sign); different => attract (positive sign)
                float sign = (a.typeId == b.typeId) ? -1f : +1f;

                // Allow snap only under attraction (different types)
                bool allowSnap = (sign > 0f)
                                 && a.useSnap && b.useSnap
                                 && dist <= Mathf.Min(a.snapDistance, b.snapDistance);

                if (allowSnap)
                {
                    if (a.jointToOther == null && b.jointToOther == null)
                    {
                        var host  = a.rb.mass <= b.rb.mass ? a : b;
                        var other = (host == a) ? b : a;

                        var jnt = host.gameObject.AddComponent<FixedJoint>();
                        jnt.connectedBody = other.rb;
                        jnt.breakForce  = Mathf.Min(host.breakForce,  other.breakForce);
                        jnt.breakTorque = Mathf.Min(host.breakTorque, other.breakTorque);

                        host.jointToOther = jnt;
                    }
                    // After snapping, no more forces are applied
                    continue;
                }

                // Force magnitude (Coulomb/Newton-style): F = sign * k * sA * sB / d^2
                float fMag = sign * k * a.strength * b.strength / (safeDist * safeDist);

                // Clamp both positive and negative to avoid spikes/oscillation
                fMag = Mathf.Clamp(fMag, -maxForcePerPair, maxForcePerPair);

                Vector3 fAB = dirAB * fMag;

                // Action and reaction
                a.rb.AddForce( fAB,  ForceMode.Force);
                b.rb.AddForce(-fAB,  ForceMode.Force);
            }
        }

        // Cleanup broken joint references
        for (int i = 0; i < n; i++)
        {
            var t = targets[i];
            if (t == null) continue;
            if (t.jointToOther != null && t.jointToOther.connectedBody == null)
            {
                t.jointToOther = null;
            }
        }
    }
}
