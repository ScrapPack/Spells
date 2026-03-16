using UnityEngine;

/// <summary>
/// Spider Shoes item: enables full surface traversal (walls + ceiling).
/// Sets HasSpiderShoes on PlayerStateMachine, which allows AirborneState
/// and WallSlideState to transition into SurfaceTraversalState.
///
/// On unequip: clears HasSpiderShoes. If currently in SurfaceTraversalState,
/// forces back to AirborneState.
/// </summary>
public class SpiderShoesItem : ItemBehavior
{
    private PlayerStateMachine stateMachine;

    public override void OnEquip()
    {
        stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.HasSpiderShoes = true;
        }
    }

    public override void OnUnequip()
    {
        if (stateMachine != null)
        {
            stateMachine.HasSpiderShoes = false;

            // Force off surface if currently traversing
            if (stateMachine.CurrentState is SurfaceTraversalState)
            {
                stateMachine.ChangeState(stateMachine.AirborneState);
            }
        }
    }
}
