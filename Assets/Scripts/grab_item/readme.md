# Grab Item System First Person

This script allows the player to pick up, hold, rotate, and place objects in the game world.

## How to Use

   - Add the `PlayerPickUp` script to your player GameObject.
   - Assign the `Camera` and `HoldPoint` references in the Inspector.
   - add `Grabbable.cs` to the items you want to be grabbable.



3. **Controls**
   - **Left Mouse Button**: Pick up or place an object.
   - **Right Mouse Button**: Drop the held object immediately.
   - **Q / E or Mouse Wheel**: Rotate the held object.

4. **Collision**
   - While holding, the object's collider is disabled to prevent collision with the player. When placed or dropped, the collider is re-enabled.

## Customization
- You can adjust pickup distance, follow speed, rotation speed, and other settings in the Inspector.
- Use the `useParenting` option for different hold behaviors.

