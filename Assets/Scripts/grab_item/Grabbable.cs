using UnityEngine;
public class Grabbable : MonoBehaviour
{
    public bool isGrabbing = false;


    private Rigidbody rb;


    void Start()
    {
        isGrabbing = false;
        rb = GetComponent<Rigidbody>();
    }

    public void SetGrabbingState(bool state)
    {
        if (isGrabbing == state)
            return;

        if (state)
        {
            // grabbed this item
        }
        else
        {
            // released this item
        }
        isGrabbing = state;
    }

}