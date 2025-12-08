# How to use it

## For magnetic object: create a object(eg. Cube), then choose the both size of the object add an empty object and add the Magnetic_poles to these empty object. Set the polarity -1 or 1. Then create an empty object to add the Magnet_Solver.

## For GrapplingGun:
### Basic Setup:
1. Create a GameObject as the grappling gun
2. Add the GrapplingGun script to the object
3. Configure the following essential parameters:
   - **Fire Origin**: Set the firing point (usually the gun muzzle position)
   - **Player Rb**: Set the player's Rigidbody (for pull player mode)
   - **Line Renderer**: Add LineRenderer component to display the rope
   - **Magnetic Layer Mask**: Set the layer for grabbable objects
   - **Magnetic Tag**: Set the tag for grabbable objects (e.g., "Magnetic obj")

### Controls:
- **Left Mouse Hold**: Fire grappling hook and maintain connection
- **Left Mouse Release**: Release grappling hook
- **Right Mouse**: Toggle soft/hard connection mode
- **X Key**: Switch pull mode (pull object to player / pull player to object)
- **Q/E Keys**: Horizontally rotate the grabbed object
- **Mouse Movement**: Spherical movement of grabbed object (maintains distance)
- **Mouse Scroll**: Adjust grappling distance

### Two Pull Modes:
1. **PullTargetToPlayer** (default): Pull the grabbed object towards the player
2. **PullPlayerToTarget**: Pull the player towards the grabbed object (classic grappling mode)

### Connection Modes:
- **Soft Connection**: Uses spring force, object can swing
- **Hard Connection**: Uses FixedJoint, object remains fixed

### Target Object Requirements:
- Must have Rigidbody component
- Must have Collider component (non-trigger)
- Need to add Magnetic_Poles component or set correct Tag

