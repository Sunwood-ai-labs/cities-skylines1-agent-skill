using ColossalFramework;

namespace SkylinesAgentBridge
{
    // Renames every road segment that carries one name (a whole street at once),
    // using the same NetManager call as the in-game road name editor.
    public static class NameCommands
    {
        // Switch a junction's traffic lights on or off, as the game's own
        // junction toggle does: mark the node custom and set/clear its lights.
        public static CommandResult SetJunctionLights(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "nodeId", 0f);
            bool enabled = JsonUtil.GetBool(body, "enabled", false);
            NetManager net = Singleton<NetManager>.instance;
            if (id == 0 || (net.m_nodes.m_buffer[id].m_flags & NetNode.Flags.Created) == NetNode.Flags.None)
            {
                return CommandResult.Fail("Node was not found: " + id);
            }
            NetNode.Flags flags = net.m_nodes.m_buffer[id].m_flags | NetNode.Flags.CustomTrafficLights;
            if (enabled) flags |= NetNode.Flags.TrafficLights; else flags &= ~NetNode.Flags.TrafficLights;
            net.m_nodes.m_buffer[id].m_flags = flags;
            net.UpdateNode(id);
            bool now = (net.m_nodes.m_buffer[id].m_flags & NetNode.Flags.TrafficLights) != NetNode.Flags.None;
            return CommandResult.FromJson("{\"ok\":true,\"nodeId\":" + id + ",\"trafficLights\":" + JsonUtil.Bool(now) + "}");
        }

        // The city's own name (what the info panel shows and saves carry).
        public static CommandResult SetCityName(string body)
        {
            string name = JsonUtil.GetString(body, "name", "");
            SimulationManager sim = Singleton<SimulationManager>.instance;
            if (name.Length == 0 || sim.m_metaData == null)
            {
                return CommandResult.Fail("A name is required and a city must be loaded.");
            }
            string old = sim.m_metaData.m_CityName;
            sim.m_metaData.m_CityName = name;
            return CommandResult.FromJson("{\"ok\":true,\"from\":\"" + JsonUtil.Escape(old) + "\",\"name\":\"" + JsonUtil.Escape(sim.m_metaData.m_CityName) + "\"}");
        }

        public static CommandResult RenameRoad(string body)
        {
            string from = JsonUtil.GetString(body, "from", "");
            string to = JsonUtil.GetString(body, "to", "");
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            if (from.Length == 0 || to.Length == 0)
            {
                return CommandResult.Fail("from and to are required.");
            }

            NetManager net = Singleton<NetManager>.instance;
            int count = 0;
            for (ushort i = 1; i < net.m_segments.m_size; i++)
            {
                if ((net.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    continue;
                }
                if (net.GetSegmentName(i) != from)
                {
                    continue;
                }
                if (!dryRun)
                {
                    net.SetSegmentNameImpl(i, to);
                }
                count++;
            }
            if (count == 0)
            {
                return CommandResult.Fail("No segments are named: " + from);
            }
            return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) + ",\"from\":\"" + JsonUtil.Escape(from) + "\",\"to\":\"" + JsonUtil.Escape(to) + "\",\"segments\":" + count + "}");
        }
    }
}
