using UnityEngine;

// Interface for item effects that can be triggered on pedestals
// This is the core interface that all pedestal effects must implement
public interface IItemEffect
{
    // Name of the effect for identification
    string EffectName { get; }
    
    // Name of the item required to trigger this effect
    string RequiredItemName { get; }
    
    // Trigger the effect when an item is placed on the pedestal
    void TriggerEffect(Pedestal pedestal, Pickable item);
    
    // Stop the effect when the item is removed from the pedestal
    void StopEffect(Pedestal pedestal, Pickable item);
    
    // Check if this effect can be triggered by the given item
    bool CanTriggerWith(Pickable item);
}
