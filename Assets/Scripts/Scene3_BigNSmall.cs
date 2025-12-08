using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scene3_BigNSmall : MonoBehaviour
{
    // Size Settings
    private float[] sizes = {
        0.3f,
        1.0f,
        3.0f
    };
    private int currentSizeIndex = 1;

    void Start()
    {
        SetSize(1);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentSizeIndex = (currentSizeIndex + 1) % sizes.Length;
            SetSize(currentSizeIndex);
        }
    }

    void SetSize(int newSizeIndex)
    {
        currentSizeIndex = newSizeIndex;
        transform.localScale = Vector3.one * sizes[newSizeIndex];
    }
}
