using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scene1_Gravity : MonoBehaviour
{
    private Vector3[] gravityDirections = {
        Vector3.down,    // down(default)
        Vector3.up,      // up
        Vector3.left,    // left
        Vector3.right    // right
    };

    private int currentGravityIndex = 0;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // disable the Unity default rb.gravity setting
        rb.useGravity = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentGravityIndex = (currentGravityIndex + 1) % gravityDirections.Length;
        }
        // apply gravity
        rb.AddForce(gravityDirections[currentGravityIndex] * 9.81f, ForceMode.Force);
    }
}
