# Dual Interaction System

A comprehensive Unity interaction system that provides two distinct modes for object interaction: **Grab Mode** for manipulating objects and **Pick Mode** for collecting items.

## Features

### Dual Mode System
- **Grab Mode**: Pick up, carry, and place objects in the world
- **Pick Mode**: Collect items and equip them for use
- Seamless switching between modes with keyboard shortcuts
- Visual feedback through crosshair and UI indicators

### Smart Crosshair System
- Dynamic crosshair that changes appearance when hovering over interactable objects
- Mode-aware detection (different behaviors for each mode)
- Smooth animation transitions for visual feedback

### Item Management
- Inventory system for collected items
- Equipment display in hand/on character
- Item switching capabilities

## Controls

### Mode Switching
- **1 Key**: Switch to Grab Mode
- **2 Key**: Switch to Pick Mode

### Pick Mode (Items)
- **F Key**: Pick up items
- **R Key**: Switch to next item in inventory
- **T Key**: Switch to previous item in inventory

## System Architecture

### Core Scripts

#### `InteractionModeManager.cs`
The central controller that manages mode switching and system coordination.

**Key Features:**
- Singleton pattern for global access
- Mode switching logic (1/2 keys)
- UI updates for current mode display
- System enable/disable management

**Public Properties:**
```csharp
public InteractionMode currentMode     // Current active mode
public TextMeshProUGUI modeDisplayText // UI text for mode display
public PlayerPickup playerPickup       // Reference to grab system
public PlayerPickItem playerPickItem   // Reference to pick system
public CrosshairUI crosshairUI         // Reference to crosshair system
```

#### `PlayerPickup.cs` (Grab System)
Handles object grabbing, carrying, and placement mechanics.

**Key Features:**
- Raycast-based object detection
- Physics-based object manipulation
- Placement validation
- Two holding modes: physics-based and parenting

**Public Properties:**
```csharp
public Camera cam                    // Camera for raycasting
public float interactDistance        // Maximum interaction distance
public Transform holdPoint           // Position where objects are held
public LayerMask placeOnMask         // Valid placement surfaces
public CrosshairUI crosshairUI       // Crosshair reference
```

#### `PlayerPickItem.cs` (Pick System)
Manages item collection, inventory, and equipment display.

**Key Features:**
- Item collection with F key
- Inventory management (max 5 items)
- Item switching with R/T keys
- 3D equipment display in hand

**Public Properties:**
```csharp
public Camera cam                    // Camera for raycasting
public float interactDistance        // Maximum interaction distance
public Transform handPoint           // Position where equipped items appear
public int maxItems                  // Maximum inventory capacity
```

#### `CrosshairUI.cs`
Provides visual feedback through a dynamic crosshair system.

**Key Features:**
- Mode-aware interaction detection
- Smooth color and size transitions
- Configurable visual states

**Public Properties:**
```csharp
public Image crosshairImage          // UI Image component for crosshair
public Color normalColor             // Default crosshair color
public Color interactableColor       // Color when object is interactable
public float animationSpeed          // Animation transition speed
```

### Component Scripts

#### `Grabbable.cs`
Simple marker component for objects that can be grabbed.

```csharp
public class Grabbable : MonoBehaviour { }
```

#### `Pickable.cs`
Component for items that can be collected and equipped.

**Properties:**
```csharp
public string itemName               // Display name of the item
public string itemDescription        // Item description
public GameObject equippedPrefab     // 3D model shown when equipped
```