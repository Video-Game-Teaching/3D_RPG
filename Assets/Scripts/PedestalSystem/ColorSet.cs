using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    public Color targetColor;
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        renderer.material.color = targetColor;
    }
}
