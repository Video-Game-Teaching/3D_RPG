using UnityEngine;

// SnapZone: when a moving object enters this trigger, snap it to a fixed coordinate.
// Usage:
// - Add this script to a GameObject with a Trigger Collider (set isTrigger = true).
// - Configure the "Snap Position" (local or world), rotation, and behavior options in Inspector.
// - Eligible objects: any object with a Rigidbody or tagged with the optional `requiredTag`.
// - The script checks Rigidbody.velocity.magnitude > minSnapSpeed to detect "moving" objects.

public class SnapZone : MonoBehaviour
{
    [Header("Target/Filter")]
    [Tooltip("Optional: only snap objects with this tag. Leave empty to allow any Rigidbody-carrying object.")]
    public string requiredTag = "";

    [Header("Snap Transform")]
    [Tooltip("If true, snap position is interpreted as local to this zone; otherwise world-space.")]
    public bool useLocalSnapPosition = true;
    [Tooltip("Position to snap to (local or world depending on flag).")]
    public Vector3 snapPosition = Vector3.zero;
    [Tooltip("When true, will also set the snapped object's rotation to `snapRotation`.")]
    public bool snapRotation = false;
    public Quaternion snapRotationValue = Quaternion.identity;

    [Header("Behavior")]
    [Tooltip("Minimum speed required to consider the object 'moving' and eligible for snapping.")]
    public float minSnapSpeed = 0.1f;
    [Tooltip("If true, set Rigidbody.isKinematic = true on the snapped object (freezes physics).")]
    public bool makeKinematic = true;
    [Tooltip("If true, set the snapped object's parent to this zone (useful to keep it fixed relative to zone).")]
    public bool parentToZone = false;
    [Tooltip("If true, the zone will only snap the first eligible object and then disable itself.")]
    public bool singleUse = false;

    [Header("Magnet / Control Overrides")]
    [Tooltip("If true, snap the object regardless of its Rigidbody velocity. Useful when objects are being manipulated by a magnet gun.")]
    public bool snapRegardlessOfVelocity = true;
    [Tooltip("If true, unparent the object (set parent = null) before snapping. Useful to detach objects that are parented to a controller.")]
    public bool forceDetachParent = true;

    [Tooltip("If true, allow snapping while the object stays inside the trigger (OnTriggerStay checks). Otherwise only OnTriggerEnter triggers snapping.")]
    public bool allowOnStay = false;

    // Optional: interface callback for objects that want to receive a notification
    public interface ISnappable
    {
        void OnSnapped(Transform zone, Vector3 snapWorldPosition, Quaternion snapWorldRotation);
    }

    void Reset()
    {
        // sensible defaults
        useLocalSnapPosition = true;
        snapPosition = Vector3.zero;
        snapRotation = false;
        snapRotationValue = Quaternion.identity;
        minSnapSpeed = 0.1f;
        makeKinematic = true;
        parentToZone = false;
        singleUse = false;
        allowOnStay = false;
    }

    void OnTriggerEnter(Collider other)
    {
        TrySnap(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        if (allowOnStay) TrySnap(other.gameObject);
    }

    void TrySnap(GameObject obj)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !obj.CompareTag(requiredTag))
            return;

        // Prefer Rigidbody for speed check and kinematic toggling
        Rigidbody rb = obj.GetComponent<Rigidbody>();

        // If no rigidbody, don't snap (you can change this if you want to support non-rigidbody objects)
        if (rb == null) return;

        // If configured, snap regardless of velocity (useful for magnet-held objects)
        if (!snapRegardlessOfVelocity)
        {
            if (rb.velocity.magnitude < minSnapSpeed)
                return; // object not moving enough to trigger snap
        }

        // Before snapping: clean up physics constraints created by grappling/magnet systems
        // 1) Remove Joint components (FixedJoint, SpringJoint, etc.) so the object isn't pulled back
        var joints = obj.GetComponents<Joint>();
        foreach (var j in joints)
        {
            var connected = j.connectedBody;
            Destroy(j);
            // If the connected body is a temporary anchor created by grapple, destroy it too
            if (connected != null && connected.gameObject != null && connected.gameObject.name.StartsWith("GrappleAnchor_"))
            {
                Destroy(connected.gameObject);
            }
        }

        // 2) If the object is a MagneticTarget, temporarily disable magnetic behaviour to avoid re-grab
        var mt = obj.GetComponent<MagneticTarget>();
        if (mt != null)
        {
            mt.isMagnetic = false;
        }

        // compute world snap position
        Vector3 worldSnap = useLocalSnapPosition ? transform.TransformPoint(snapPosition) : snapPosition;
        Quaternion worldRot = useLocalSnapPosition ? transform.rotation * snapRotationValue : snapRotationValue;

        // apply snap
        // if the object is parented (e.g. magnet gun parenting), optionally detach first
        if (forceDetachParent && obj.transform.parent != null)
        {
            obj.transform.SetParent(null, true);
        }

        obj.transform.position = worldSnap;
        if (snapRotation) obj.transform.rotation = worldRot;

        if (parentToZone) obj.transform.SetParent(transform, true);

        if (makeKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // notify if object implements the optional interface
        var snappable = obj.GetComponent<ISnappable>();
        if (snappable != null)
        {
            snappable.OnSnapped(transform, worldSnap, worldRot);
        }

        if (singleUse)
        {
            // disable the trigger collider to prevent future snaps
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            enabled = false;
        }
    }
}
