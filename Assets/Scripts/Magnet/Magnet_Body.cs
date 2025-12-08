using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnet_Body : MonoBehaviour
{
    [Tooltip("auto register")]
    public List<Magnetic_Poles> poles = new List<Magnetic_Poles>();

    [HideInInspector] public Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }
    void OnEnable() { Magnet_Solver.RegisterBody(this); }
    void OnDisable() { Magnet_Solver.UnregisterBody(this); }
}
