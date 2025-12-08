# Pedestal Interaction Setup Guide

This guide shows how to set up player interaction with pedestals.

## Setup Steps

### Step 1: Add Interaction Component to Player
1. Select your Player object in the scene
2. Add the `PedestalInteraction` component to it
3. Configure the settings:
   - **Interaction Distance**: 3.0 (how far player can interact)
   - **Pedestal Layer Mask**: Set to the layer your pedestals are on
   - **Interaction Key**: E (or any key you prefer)

### Step 2: Create Interaction UI (Optional)
1. Create a UI Canvas in your scene
2. Add a Text component for the interaction prompt
3. Position it in the center of the screen
4. Assign it to the **Interaction Prompt** field in PedestalInteraction

### Step 3: Set Up Pedestal Layers
1. Create a new layer called "Pedestal" (or use existing layer)
2. Assign your pedestal objects to this layer
3. Set the **Pedestal Layer Mask** in PedestalInteraction to this layer

### Step 4: Test the Interaction
1. Play the scene
2. Pick up a key item (F key)
3. Look at the pedestal
4. Press E to place the key
5. Press E again to remove the key

## How It Works

### Interaction Flow
1. **Detection**: Player looks at pedestal within interaction distance
2. **Prompt**: UI shows "Press E to place item" or "Press E to remove item"
3. **Action**: Player presses E to interact
4. **Result**: Item is placed on or removed from pedestal

### Key Features
- **Automatic Detection**: No need to manually target pedestals
- **Visual Feedback**: UI prompt shows what action is available
- **Inventory Integration**: Works with existing PlayerController system
- **Flexible**: Can be customized for different interaction types

## Customization

### Change Interaction Key
```csharp
// In the Inspector, change the Interaction Key
interactionKey = KeyCode.F;  // Use F key instead of E
```

### Change Interaction Distance
```csharp
// Make interaction range larger
interactionDistance = 5f;

// Make interaction range smaller
interactionDistance = 2f;
```

### Change UI Text
```csharp
// Customize the prompt text
placeItemText = "Press E to place item";
removeItemText = "Press E to take item";
```

## Troubleshooting

### No Interaction Prompt
- Check that the pedestal is on the correct layer
- Verify the Pedestal Layer Mask is set correctly
- Make sure the player is close enough to the pedestal

### Can't Place Items
- Check that the item name matches the effect's required item name
- Verify the pedestal type allows the item
- Make sure the player has an equipped item

### Can't Remove Items
- Check that the player's inventory isn't full
- Verify the pedestal has an item on it

## Advanced Features

You can extend this system by:
- Adding different interaction types
- Creating custom UI prompts
- Adding sound effects
- Implementing animation
- Adding particle effects
