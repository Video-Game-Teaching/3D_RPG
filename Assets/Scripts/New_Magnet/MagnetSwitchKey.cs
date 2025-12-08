using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class MagnetSwitchKey : MonoBehaviour
{
	public AudioClip pickupClip;
	public GameObject pickupVfxPrefab;
	public bool destroyOnPickup = true;
	[Tooltip("If true, only objects tagged Player can pick the key.")]
	public bool requirePlayerTag = true;

	void Reset()
	{
		// Ensure collider is set as trigger for pickups
		var col = GetComponent<Collider>();
		if (col) col.isTrigger = true;
	}

	void OnTriggerEnter(Collider other)
	{
		if (requirePlayerTag && !other.CompareTag("Player")) return;

		// Prefer the equipped gun under the player hierarchy
		var gun = other.GetComponentInChildren<MagneticGun>(true);
		// Fallback: find any MagneticGun in scene
		if (!gun) gun = Object.FindObjectOfType<MagneticGun>();

		if (gun)
		{
			gun.UnlockModeSwitching();
		}
		else
		{
			// No instance yet (e.g., gun not picked). Mark session unlock so it applies when equipped later
			MagneticGun.MarkSessionUnlock();
			Debug.Log("MagnetSwitchKey: No MagneticGun instance found. Session unlock applied.");
		}

		if (pickupClip) AudioSource.PlayClipAtPoint(pickupClip, transform.position);
		if (pickupVfxPrefab) Instantiate(pickupVfxPrefab, transform.position, transform.rotation);

		if (destroyOnPickup) Destroy(gameObject);
		else gameObject.SetActive(false);
	}
}

