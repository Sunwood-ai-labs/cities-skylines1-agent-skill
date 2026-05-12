using ICities;

namespace SkylinesAgentBridge
{
    public sealed class AgentBridgeThreading : ThreadingExtensionBase
    {
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            AgentBridge.Instance.ProcessGameThreadQueue(realTimeDelta);
        }
    }
}
