using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GunModeTextBootstrap : MonoBehaviour
{
	void Awake()
	{
		Clear();
	}

	void Start()
	{
		Clear();
	}

	void OnEnable()
	{
		Clear();
	}

	void Clear()
	{
		var tmp = GetComponent<TextMeshProUGUI>();
		if (tmp) tmp.text = "";
	}
}

