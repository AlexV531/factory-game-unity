using UnityEngine;
using Unity.Netcode.Components;

[RequireComponent(typeof(VehicleController))]
public class VehicleNetworkTransformHook : NetworkTransform
{
    // private VehicleController vehicleController;

    // protected override void Awake()
    // {
    //     base.Awake();
    //     vehicleController = GetComponent<VehicleController>();
    // }

    // protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
    // {
    //     base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
        
    //     // Notify vehicle controller of server position updates
    //     if (!IsServer && vehicleController != null)
    //     {
    //         vehicleController.OnServerPositionUpdate(newState.GetPosition(), newState.GetRotation());
    //     }
    // }
}