using ICities;

namespace SkylinesAgentBridge
{
    public sealed class AgentBridgeLoading : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            AgentBridge.Instance.OnLevelLoaded();
        }

        public override void OnLevelUnloading()
        {
            AgentBridge.Instance.OnLevelUnloading();
        }
    }
}
