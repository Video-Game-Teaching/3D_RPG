# Pedestal System - How to Create Effects

This guide shows you how to create custom effects for the pedestal system.

## Quick Start

### Step 1: Create a New Effect Script

Create a new C# script and make it implement `IItemEffect`:

```csharp
using UnityEngine;

public class MyCustomEffect : MonoBehaviour, IItemEffect
{
    [Header("Effect Settings")]
    public string effectName = "My Effect";
    public string requiredItemName = "MyItem";
    
    // Implement interface properties
    public string EffectName { get { return effectName; } }
    public string RequiredItemName { get { return requiredItemName; } }
    
    public void TriggerEffect(Pedestal pedestal, Pickable item)
    {
        Debug.Log($"My effect triggered by {item.itemName}");
        // Add your effect logic here
    }
    
    public void StopEffect(Pedestal pedestal, Pickable item)
    {
        Debug.Log("My effect stopped");
        // Add cleanup logic here
    }
    
    public bool CanTriggerWith(Pickable item)
    {
        return item.itemName == requiredItemName;
    }
}
```

### Step 2: Add Effect to Pedestal

1. Select your Pedestal object in the scene
2. In the Inspector, find "Item Effects" section (under Pedestal component)
3. Set the Size to 1 (or however many effects you want)
4. Drag your effect script into the Element 0 slot
5. Configure the settings in the Inspector

**Note:** If you don't see the "Item Effects" section, make sure you have the latest version of the Pedestal script with the `[Header("Item Effects")]` attribute.

### Step 3: Test

1. Create a Pickable item with the same name as your `requiredItemName`
2. Place the item on the pedestal
3. Your effect should trigger!

## Tips

1. **Always implement all 3 methods**: `TriggerEffect`, `StopEffect`, `CanTriggerWith`
2. **Use the Inspector**: Set your effect name and required item name in the Inspector
3. **Test with simple items**: Start with basic effects before making complex ones
4. **Use Debug.Log**: Add logging to see when your effects trigger
5. **Clean up**: Always clean up in `StopEffect` if you create objects in `TriggerEffect`
6. **Pedestal Types**: 
   - **Universal**: Accepts any item
   - **Specific**: Only accepts items that match the effects in the Item Effects list
   - **Combination**: Requires multiple specific items

## Common Issues

- **Effect not triggering**: Check that your `requiredItemName` matches the item's name exactly
- **Effect not stopping**: Make sure you implement `StopEffect` properly
- **CanTriggerWith returning false**: Check your item name matching logic
- **"This pedestal cannot place item"**: 
  - Check if the pedestal type is "Specific" but has no effects in the Item Effects list
  - Either add effects to the list or change the pedestal type to "Universal"
- **Item Effects list not visible**: Make sure you have the latest Pedestal script with the `[Header("Item Effects")]` attribute

## Next Steps

Once you understand the basics, you can create more complex effects like:
- Door opening/closing
- Item spawning
- Area effects
- Timed effects
- Combination effects
