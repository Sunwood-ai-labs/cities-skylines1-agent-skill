using System;
using System.Reflection;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class BulldozeCommands
    {
        public static CommandResult Bulldoze(string body)
        {
            string entityType = JsonUtil.GetString(body, "entityType", "");
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            bool keepNodes = JsonUtil.GetBool(body, "keepNodes", false);
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);

            if (id == 0)
            {
                return CommandResult.Fail("id is required.");
            }

            if (entityType == "building")
            {
                BuildingManager manager = BuildingManager.instance;
                if ((manager.m_buildings.m_buffer[id].m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    return CommandResult.Fail("Building was not found: " + id);
                }

                if (!dryRun)
                {
                    manager.ReleaseBuilding(id);
                }

                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) + ",\"entityType\":\"building\",\"id\":" + id + "}");
            }

            if (entityType == "netSegment")
            {
                NetManager manager = NetManager.instance;
                if ((manager.m_segments.m_buffer[id].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    return CommandResult.Fail("Net segment was not found: " + id);
                }

                if (!dryRun)
                {
                    ReleaseSegment(manager, id, keepNodes);
                }

                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) + ",\"entityType\":\"netSegment\",\"id\":" + id + ",\"keepNodes\":" + JsonUtil.Bool(keepNodes) + "}");
            }

            if (entityType == "netNode")
            {
                NetManager manager = NetManager.instance;
                if ((manager.m_nodes.m_buffer[id].m_flags & NetNode.Flags.Created) == NetNode.Flags.None)
                {
                    return CommandResult.Fail("Net node was not found: " + id);
                }

                if (!dryRun)
                {
                    manager.ReleaseNode(id);
                }

                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) + ",\"entityType\":\"netNode\",\"id\":" + id + "}");
            }

            return CommandResult.Fail("Unsupported entityType. Use building, netSegment, or netNode.");
        }

        private static void ReleaseSegment(NetManager manager, ushort id, bool keepNodes)
        {
            try
            {
                manager.ReleaseSegment(id, keepNodes);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == null || ex.Message.IndexOf("Already in the same thread") < 0)
                {
                    throw;
                }

                MethodInfo method = typeof(NetManager).GetMethod(
                    "ReleaseSegmentImplementation",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(ushort) },
                    null);

                if (method == null)
                {
                    throw;
                }

                method.Invoke(manager, new object[] { id });
            }
        }
    }
}
