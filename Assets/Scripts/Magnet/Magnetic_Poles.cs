using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Magnetic_Poles : MonoBehaviour
{
    [Header("Magnet Setting")]
    [Tooltip("Magnet Strength, the magent strength of two objct decide the strength of the Magnet.")]
    public float strength = 1f;

    [Tooltip("Polarity: +1 indicates the north pole faces outward; -1 indicates the south pole faces outward. Positive and negative poles attract, and like poles repel.")]
    [Range(-1f, 1f)] public float polarity = 1f;

    [Tooltip("Range (meters). Calculations beyond this range are ignored to save performance. 0 means infinite range.")]
    public float range = 0f;

    [Header("Safety / Turning")]
    [Tooltip("avoid large force in too small distance")]
    public float softening = 0.1f;

    [Tooltip("When each pair interacts, whether to generate different accelerations according to mass (Force) or ignore mass (Acceleration).")]
    public bool useAccelerationMode = false;

    [Tooltip("Interact with these layers only; leave blank = interact with all layers.")]
    public LayerMask interactLayers = ~0;

    [HideInInspector] public Magnet_Body body;


    void OnEnable()
    {
        body = GetComponentInParent<Magnet_Body>();
        if (body && !body.poles.Contains(this)) body.poles.Add(this);
        Magnet_Solver.RegisterPole(this);
    }
    void OnDisable()
    {
        if (body) body.poles.Remove(this);
        Magnet_Solver.UnregisterPole(this);
    }

    public bool CanInteractWith(Magnetic_Poles other)
    {
        if (!other || other == this) return false;

        if (((1 << other.gameObject.layer) & interactLayers) == 0) return false;
        if (((1 << gameObject.layer) & other.interactLayers) == 0) return false;

        // The poles on the same rigid body can choose whether to exclude each other. By default, the self-body is not calculated (to avoid self-absorption)
        if (other.body == body) return false;

        // distance
        if (range > 0f || other.range > 0f)
        {
            float r = Vector3.Distance(transform.position, other.transform.position);
            if (range > 0f && r > range) return false;
            if (other.range > 0f && r > other.range) return false;
        }

        return true;
    }
}
