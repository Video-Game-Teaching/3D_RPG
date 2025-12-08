using UnityEngine;
using TMPro; 

/// <summary>
/// Magnetic gun with active mode switching:
/// - Red mode attracts only MagneticTarget.typeId == -1 blue obj
/// - Blue mode attracts only MagneticTarget.typeId == 1 red obj
/// </summary>
public class MagneticGun : MonoBehaviour
{
    public enum GunMode { Red = 1, Blue = -1 }
    public TMP_Text modeText;          // assign via Inspector
    public bool autoFindModeText = true;
    public float messageDuration = 1.5f;
    private float messageTimer = 0f;

	// Persisted key so unlocking via key works even if gun isn't picked yet
	const string PrefsKey_Unlock = "MagnetSwitchUnlocked";

	[Header("Unlock")]
	[Tooltip("If true, the key unlock persists across sessions via PlayerPrefs. If false, unlock only lasts for this play session.")]
	public bool persistUnlockAcrossSessions = false;

	// Session-level unlock (not persisted). Allows: pick key first → equip gun later in same run
	public static bool sessionSwitchUnlocked = false;

    [Header("Basic")]
    public Camera cam;
    public Transform fireOrigin;
    public float maxDistance = 25f;
    public float basePullForce = 80f;
    public LayerMask magneticLayers;
	public LayerMask ignoreLayers;

    [Header("Visual")]
    public Renderer gunRenderer;
    public Color redColor = Color.red;
    public Color blueColor = Color.blue;
    public Color idleColor = Color.white; // color when idle or unequipped (optional)

    [Header("Input")]
    public KeyCode redKey = KeyCode.Q;    // switch to Red mode
    public KeyCode blueKey = KeyCode.E;   // switch to Blue mode
    public bool allowHoldToLock = true;        // hold to continuously try locking
    public bool showDebugInfo = true;

	[Header("Mode Lock")]
[Tooltip("When enabled, only Blue mode attracts Red blocks; switching to Red is disabled.")]
	public bool lockBlueOnly = true;

	[Header("Collisions")]
[Tooltip("While locked, ignore collisions between target and player/gun; restore on release.")]
	public bool ignorePlayerAndGunCollisionsWhileLocked = true;

	[Header("Gun Physics")]
[Tooltip("While equipped, make the gun rigidbody kinematic and frozen to prevent being pushed; restore on unequip.")]
	public bool makeGunKinematicWhileEquipped = true;

	[Header("Same-type Push")]
	[Tooltip("In Blue mode, push Blue blocks; in Red mode, push Red blocks.")]
	public bool enableSameTypePush = true;
	public float sameTypePushForce = 60f;

    private Rigidbody target;
    private MagneticTarget targetMag;
    private bool isEquipped = false;
    private GunMode mode = GunMode.Blue;       // default Blue mode
    private float _nextDebugTick;

// record ignored collision pairs for restoring on release
	private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Collider, Collider>> ignoredPairs
		= new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Collider, Collider>>();

// original gun rigidbody state (for equip/unequip switching)
	private Rigidbody _gunRb;
	private bool _origKinematic;
	private bool _origUseGravity;
	private RigidbodyConstraints _origConstraints;

    private Ray GetAimRay()
    {
        if (cam) return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Transform origin = fireOrigin ? fireOrigin : transform;
        return new Ray(origin.position, origin.forward);
    }

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!fireOrigin) fireOrigin = transform;
        if (!gunRenderer) gunRenderer = GetComponentInChildren<Renderer>();

		// Apply unlock state at startup
		if (persistUnlockAcrossSessions && PlayerPrefs.GetInt(PrefsKey_Unlock, 0) == 1)
		{
			lockBlueOnly = false;
		}
		else if (sessionSwitchUnlocked)
		{
			lockBlueOnly = false;
		}
        ApplyModeVisual();
    }

    void Update()
    {
        // equipped gate
        if (!isEquipped)
        {
            if (showDebugInfo && Input.GetMouseButtonDown(0))
                Debug.Log("MagneticGun: Not equipped.");
            return;
        }

		// mode switching (when locked to Blue, forbid Red and enforce Blue)
		if (!lockBlueOnly)
		{
			if (Input.GetKeyDown(redKey))  SetMode(GunMode.Red);
			if (Input.GetKeyDown(blueKey)) SetMode(GunMode.Blue);
		}
		else if (mode != GunMode.Blue)
		{
			SetMode(GunMode.Blue);
		}

        if (!cam) cam = Camera.main;
        if (!fireOrigin) fireOrigin = transform;

        var aimRay = GetAimRay();
        Debug.DrawRay(aimRay.origin, aimRay.direction * maxDistance, Color.cyan);

        // try to lock (only targets compatible with current mode)
        if (Input.GetMouseButtonDown(0) || (allowHoldToLock && Input.GetMouseButton(0)))
            TryLockTarget();

        // release when mouse button up
        if (Input.GetMouseButtonUp(0))
            ReleaseTarget();
    }

    void ResolveModeText()
    {
        if (modeText) return;
        GameObject canvasObj = GameObject.Find("HUDCanvas");
        if (canvasObj == null)
        {
            Debug.LogWarning("MagneticGun: No HUDCanvas found in scene!");
            return;
        }

        // 2️⃣ find ModeText under this Canvas only
        Transform t = canvasObj.transform.Find("ModeText");
        if (t != null)
        {
            modeText = t.GetComponent<TextMeshProUGUI>();
            if (modeText != null)
            {
                Debug.Log("MagneticGun: ModeText found under Canvas!");
                return;
            }
        }
        // 3️⃣ if exact name not found, find the first TextMeshProUGUI under Canvas
        modeText = canvasObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (modeText != null)
        {
            Debug.Log($"MagneticGun: Found TMP text under Canvas ({modeText.name}).");
        }
        else
        {
            Debug.LogWarning("MagneticGun: No TextMeshProUGUI found under Canvas!");
        }
    }
    

	void FixedUpdate()
	{
		if (!isEquipped) return;

		// Same-type push: while holding fire, apply push to same-type target under crosshair (no lock)
		if (enableSameTypePush && Input.GetMouseButton(0))
		{
			Ray ray = GetAimRay();
			LayerMask effectiveLayers = magneticLayers & ~ignoreLayers;
			if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, effectiveLayers))
			{
				var mag = hit.collider.GetComponentInParent<MagneticTarget>();
				if (mag != null && mag.isMagnetic)
				{
					int sameType = (int)mode; // Blue:-1, Red:1
					if (mag.typeId == sameType)
					{
						var rb = hit.rigidbody ?? mag.GetComponent<Rigidbody>();
						if (rb != null)
						{
							Vector3 pushDir = ray.direction;
							float push = sameTypePushForce * Mathf.Max(0.0f, mag.strength);
							rb.AddForce(pushDir.normalized * push, ForceMode.Acceleration);
						}
					}
				}
			}
		}

		// Opposite-type pull for locked target
		if (target == null || targetMag == null) return;

		// if target no longer compatible with current mode → release immediately (avoid pulling after mode change)
		if (!IsTargetCompatible(targetMag))
		{
			if (showDebugInfo) Debug.Log("MagneticGun: Target no longer compatible with current mode. Releasing.");
			ReleaseTarget();
			return;
		}

		Vector3 dir = (fireOrigin.position - target.worldCenterOfMass);
		if (dir.sqrMagnitude < 1e-6f) return;

		float force = basePullForce * targetMag.strength;
		// use Acceleration to stay mass-independent
		target.AddForce(dir.normalized * force, ForceMode.Acceleration);
	}

    void TryLockTarget()
    {
        Ray ray = GetAimRay();

        LayerMask effectiveLayers = magneticLayers & ~ignoreLayers;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, effectiveLayers))
        {
            var mag = hit.collider.GetComponentInParent<MagneticTarget>();
				if (mag != null && mag.isMagnetic && IsTargetCompatible(mag))
            {
					// only lock targets compatible with current mode
                target = hit.rigidbody ?? mag.GetComponent<Rigidbody>();
                targetMag = mag;
                if (showDebugInfo) Debug.Log($"MagneticGun: Locked {hit.collider.name} (typeId={mag.typeId}) in {mode} mode.");
					SetupCollisionIgnores();
                return;
            }

            if (showDebugInfo)
                Debug.Log($"MagneticGun: Hit {hit.collider.name} but it's not compatible (hasMag={mag!=null}, isMag={mag?.isMagnetic}, type={mag?.typeId}).");
        }
        else
        {
            if (showDebugInfo) Debug.Log("MagneticGun: Raycast no hit.");
        }
    }

    bool IsTargetCompatible(MagneticTarget mag)
    {
        // Red allows typeId==1; Blue allows typeId==-1
        int need = -(int)mode; // Red=1, Blue=-1
        return mag.typeId == need;
    }

    void ReleaseTarget()
    {
        target = null;
        targetMag = null;
        // keep mode color on release; call SetGunColor(idleColor) to revert to idle color if desired
		RestoreCollisionIgnores();
    }

    // explicit mode switch (can also be called externally)
    public void SetMode(GunMode newMode)
    {
		// when Blue-only lock is on, ignore requests to switch to Red
		if (lockBlueOnly && newMode != GunMode.Blue) return;
        if (mode == newMode) return;
        mode = newMode;
        ApplyModeVisual();

        // after switching modes, release immediately if the current target is incompatible
        if (targetMag && !IsTargetCompatible(targetMag))
            ReleaseTarget();

        if (showDebugInfo) Debug.Log($"MagneticGun: Switched to {mode} mode.");

        ResolveModeText();
        
        if (modeText)
        {
            if (mode == GunMode.Red)
                modeText.text = "You are on the RED mode";
            else
                modeText.text = "You are on the BLUE mode";
        }

        Debug.Log($"<color={(mode==GunMode.Red?"red":"blue")}>You are on the {mode} mode</color>");
    }

    void ApplyModeVisual()
    {
        // Red mode shows red; Blue mode shows blue
        if (mode == GunMode.Red)  SetGunColor(redColor);
        if (mode == GunMode.Blue) SetGunColor(blueColor);
    }

    void SetGunColor(Color c)
    {
        if (!gunRenderer) return;
        var mats = gunRenderer.materials; // instantiate materials to avoid global color changes
        for (int i = 0; i < mats.Length; i++)
            mats[i].color = c;
    }

    // called by the equipment system
    public void OnEquipped()
    {
        if (!cam) cam = Camera.main;
        if (!fireOrigin)
        {
            var child = transform.Find("FireOrigin");
            fireOrigin = child ? child : transform;
        }

        // ✅ auto resolve UI reference
        ResolveModeText();

		// Ensure instance respects unlock when equipped
		if (persistUnlockAcrossSessions && PlayerPrefs.GetInt(PrefsKey_Unlock, 0) == 1)
		{
			lockBlueOnly = false;
		}
		else if (sessionSwitchUnlocked)
		{
			lockBlueOnly = false;
		}

		isEquipped = true;

		// protect gun from physics while equipped
		if (makeGunKinematicWhileEquipped)
		{
			_gunRb = GetComponent<Rigidbody>();
			if (_gunRb)
			{
				_origKinematic = _gunRb.isKinematic;
				_origUseGravity = _gunRb.useGravity;
				_origConstraints = _gunRb.constraints;
				_gunRb.isKinematic = true;
				_gunRb.useGravity = false;
				_gunRb.constraints = RigidbodyConstraints.FreezeAll;
			}
		}
		// if Blue-only is locked, force Blue mode on equip
		if (lockBlueOnly) SetMode(GunMode.Blue);
		ApplyModeVisual(); // apply current mode color
        if (showDebugInfo) Debug.Log("MagneticGun equipped.");
        
        if (modeText)
        {
            modeText.text = mode == GunMode.Red
                ? "You are on the RED mode"
                : "You are on the BLUE mode";
        }

        Debug.Log("MagneticGun equipped.");
    }

    public void OnUnequipped()
    {
        isEquipped = false;
        ReleaseTarget();
        SetGunColor(idleColor);
		// Clear mode text when unequipped (e.g., EmptyHand)
		ResolveModeText();
		if (modeText)
		{
			modeText.text = "";
		}
		// restore gun physics state
		if (_gunRb)
		{
			_gunRb.isKinematic = _origKinematic;
			_gunRb.useGravity = _origUseGravity;
			_gunRb.constraints = _origConstraints;
		}
        if (showDebugInfo) Debug.Log("MagneticGun unequipped.");
    }

	void SetupCollisionIgnores()
	{
		if (!ignorePlayerAndGunCollisionsWhileLocked || target == null) return;

		// ensure previous ignore pairs are cleared
		RestoreCollisionIgnores();

		var targetCols = target.GetComponentsInChildren<Collider>(true);
		var gunCols = GetComponentsInChildren<Collider>(true);

		Collider[] playerCols = null;
		var pc = GetComponentInParent<PlayerController>();
		if (pc)
			playerCols = pc.GetComponentsInChildren<Collider>(true);
		else
		{
			var playerGo = GameObject.FindWithTag("Player");
			if (playerGo) playerCols = playerGo.GetComponentsInChildren<Collider>(true);
		}

		// ignore: target vs gun
		for (int i = 0; i < targetCols.Length; i++)
		{
			var tc = targetCols[i];
			if (!tc) continue;
			for (int j = 0; j < gunCols.Length; j++)
			{
				var gc = gunCols[j];
				if (!gc || gc == tc) continue;
				Physics.IgnoreCollision(tc, gc, true);
				ignoredPairs.Add(new System.Collections.Generic.KeyValuePair<Collider, Collider>(tc, gc));
			}
			// ignore: target vs player
			if (playerCols != null)
			{
				for (int k = 0; k < playerCols.Length; k++)
				{
					var pcoll = playerCols[k];
					if (!pcoll || pcoll == tc) continue;
					Physics.IgnoreCollision(tc, pcoll, true);
					ignoredPairs.Add(new System.Collections.Generic.KeyValuePair<Collider, Collider>(tc, pcoll));
				}
			}
		}
	}

	void RestoreCollisionIgnores()
	{
		for (int i = 0; i < ignoredPairs.Count; i++)
		{
			var a = ignoredPairs[i].Key;
			var b = ignoredPairs[i].Value;
			if (a && b) Physics.IgnoreCollision(a, b, false);
		}
		ignoredPairs.Clear();
	}

	// Unlock red/blue switching (called by key pickup)
	public void UnlockModeSwitching()
	{
		if (!lockBlueOnly) return;
		lockBlueOnly = false;

		// Always unlock for current session
		sessionSwitchUnlocked = true;

		// Optionally persist across sessions
		if (persistUnlockAcrossSessions)
		{
			PlayerPrefs.SetInt(PrefsKey_Unlock, 1);
			PlayerPrefs.Save();
		}
		if (showDebugInfo) Debug.Log("MagneticGun: Mode switch unlocked. Use Q/E to switch.");
		ResolveModeText();
		if (modeText)
		{
			modeText.text = "Mode switch unlocked! Use Q/E";
		}
	}

	// Mark session unlock (used when key is picked before any gun instance is available)
	public static void MarkSessionUnlock()
	{
		sessionSwitchUnlocked = true;
	}

	// Query if switching is currently unlocked
	public bool IsSwitchUnlocked()
	{
		return !lockBlueOnly;
	}

	// Query if the gun is currently equipped and active
	public bool IsEquipped()
	{
		return isEquipped;
	}
}
