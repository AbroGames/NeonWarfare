using Godot;
using GodotBox.Godot.Nodes.MpSync;
using KludgeBox.DI.Requests;
using KludgeBox.Reflection.Access;

namespace GodotBox.DI.Requests.MpSyncInjection;

public class MpSyncInjectionRequest : IProcessingRequest
{
    public static readonly string MpSyncNodeName = "MultiplayerSynchronizer";
    
    private readonly IMemberAccessor _memberAccessor;

    public MpSyncInjectionRequest(IMemberAccessor memberAccessor)
    {
        Di.Process(this);
        
        _memberAccessor = memberAccessor;
    }

    public void ProcessOnInstance(object instance)
    {
        if (instance is Node node)
        {
            var mpSync = node.GetNodeOrNull<AttributeMultiplayerSynchronizer>(MpSyncNodeName);
            if (mpSync == null)
            {
                mpSync = new AttributeMultiplayerSynchronizer(node);
                node.AddChildWithName(mpSync, MpSyncNodeName);
            }
        }
    }
}