using UnityEngine;

public class Pickable : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Item"; // Name of the item
    public string itemDescription = "A useful item"; // Description of the item
    
    [Header("Equipment")]
    public GameObject equippedPrefab; // Prefab to show when equipped in hand
}
