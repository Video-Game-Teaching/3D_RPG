# Key Display Effect Setup Guide

This guide shows how to set up a pedestal that glows when a "Key" item is placed on it.

## Setup Steps

### Step 1: Create the Pedestal
1. Create a GameObject in your scene
2. Add a `Pedestal` component to it
3. Add a `KeyDisplayEffect` component to it
4. Configure the pedestal settings:
   - Set `Pedestal Type` to `Specific`
   - Set `Item Effects` size to 1
   - Drag the `KeyDisplayEffect` script to Element 0

### Step 2: Configure the Key Display Effect
In the Inspector, set these values:
- **Effect Name**: "Key Display"
- **Required Item Name**: "Key"
- **Glow Color**: Yellow (or any color you prefer)
- **Glow Intensity**: 2.0
- **Glow Range**: 5.0

### Step 3: Create the Key Item
1. Create a GameObject for your key
2. Add a `Pickable` component to it
3. Set the `Item Name` to "Key"
4. Add a `Grabbable` component if you want it to be grabbable
5. Add a Collider and Rigidbody

### Step 4: Test
1. Play the scene
2. Pick up the key item
3. Place it on the pedestal
4. The pedestal should start glowing!

## Customization

### Change Glow Color
```csharp
// In the Inspector, change the Glow Color
glowColor = Color.blue;  // Blue glow
glowColor = Color.red;   // Red glow
glowColor = Color.green; // Green glow
```

### Change Glow Intensity
```csharp
// Make it brighter
glowIntensity = 5f;

// Make it dimmer
glowIntensity = 1f;
```

### Change Glow Range
```csharp
// Make it glow in a larger area
glowRange = 10f;

// Make it glow in a smaller area
glowRange = 2f;
```

## How It Works

1. **Item Detection**: The effect checks if the placed item's name matches "Key"
2. **Light Addition**: When triggered, adds a Point Light to the pedestal
3. **Material Change**: Changes the pedestal's material to a glowing version
4. **Cleanup**: When the key is removed, restores the original material and removes the light

## Troubleshooting

- **No glow**: Check that the item name is exactly "Key"
- **Wrong color**: Adjust the Glow Color in the Inspector
- **Too bright/dim**: Adjust the Glow Intensity
- **Wrong range**: Adjust the Glow Range

## Advanced Features

You can extend this effect by:
- Adding particle effects
- Playing sounds
- Animating the glow
- Adding multiple glow colors
- Creating different glow patterns
