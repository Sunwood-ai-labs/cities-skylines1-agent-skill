using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class RoadCommands
    {
        public static CommandResult BuildRoad(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            string prefabName = JsonUtil.GetString(body, "roadPrefab", "Basic Road");
            string name = JsonUtil.GetString(body, "name", "");

            Vector3 start = ReadPoint(body, "start");
            Vector3 end = ReadPoint(body, "end");

            NetInfo info = PrefabCollection<NetInfo>.FindLoaded(prefabName);
            if (info == null)
            {
                return CommandResult.Fail("Road prefab was not found: " + prefabName);
            }

            if ((end - start).sqrMagnitude < 16f)
            {
                return CommandResult.Fail("Road is too short.");
            }

            TerrainManager terrain = TerrainManager.instance;
            start.y = terrain.SampleRawHeightSmoothWithWater(start, false, 0f);
            end.y = terrain.SampleRawHeightSmoothWithWater(end, false, 0f);

            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"message\":\"Build-road validation passed.\",\"roadPrefab\":\"" + JsonUtil.Escape(prefabName) + "\"}");
            }

            SimulationManager simulation = Singleton<SimulationManager>.instance;
            NetManager net = NetManager.instance;
            Randomizer randomizer = simulation.m_randomizer;

            ushort startNode = FindNearbyNode(start, 2f, info);
            ushort endNode = FindNearbyNode(end, 2f, info);
            bool createdStartNode = false;
            bool createdEndNode = false;

            if (startNode == 0)
            {
                if (!net.CreateNode(out startNode, ref randomizer, info, start, simulation.m_currentBuildIndex))
                {
                    return CommandResult.Fail("Failed to create start node.");
                }
                createdStartNode = true;
            }
            if (createdStartNode)
            {
                simulation.m_currentBuildIndex += 1u;
            }

            if (endNode == 0)
            {
                if (!net.CreateNode(out endNode, ref randomizer, info, end, simulation.m_currentBuildIndex))
                {
                    return CommandResult.Fail("Failed to create end node.");
                }
                createdEndNode = true;
            }
            if (createdEndNode)
            {
                simulation.m_currentBuildIndex += 1u;
            }

            Vector3 direction = (end - start).normalized;
            ushort segment;
            bool created = net.CreateSegment(
                out segment,
                ref randomizer,
                info,
                startNode,
                endNode,
                direction,
                -direction,
                simulation.m_currentBuildIndex,
                simulation.m_currentBuildIndex,
                false);

            simulation.m_randomizer = randomizer;

            if (!created)
            {
                return CommandResult.Fail("Failed to create road segment.");
            }

            simulation.m_currentBuildIndex += 2u;

            if (name != null && name.Length > 0)
            {
                net.SetSegmentNameImpl(segment, name);
            }

            string json = "{\"ok\":true,\"dryRun\":false,\"segmentId\":" + segment +
                ",\"startNodeId\":" + startNode +
                ",\"endNodeId\":" + endNode +
                ",\"roadPrefab\":\"" + JsonUtil.Escape(prefabName) + "\"}";

            Debug.Log("[SkylinesAgentBridge] Built road segment " + segment + " with prefab " + prefabName);
            return CommandResult.FromJson(json);
        }

        private static ushort FindNearbyNode(Vector3 position, float maxDistance, NetInfo info)
        {
            NetManager net = NetManager.instance;
            float maxDistanceSq = maxDistance * maxDistance;

            for (ushort i = 1; i < net.m_nodes.m_buffer.Length; i++)
            {
                NetNode node = net.m_nodes.m_buffer[i];
                if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.None)
                {
                    continue;
                }
                if (!CanReuseNode(node.Info, info))
                {
                    continue;
                }

                Vector3 delta = node.m_position - position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= maxDistanceSq)
                {
                    return i;
                }
            }

            return 0;
        }

        private static bool CanReuseNode(NetInfo existing, NetInfo requested)
        {
            if (existing == null || requested == null || existing.m_class == null || requested.m_class == null)
            {
                return false;
            }

            if (existing == requested)
            {
                return true;
            }

            return existing.m_class.m_service == ItemClass.Service.Road &&
                requested.m_class.m_service == ItemClass.Service.Road;
        }

        private static Vector3 ReadPoint(string body, string name)
        {
            float x = JsonUtil.GetPointNumber(body, name, "x", 0f);
            float z = JsonUtil.GetPointNumber(body, name, "z", 0f);
            float y = JsonUtil.GetPointNumber(body, name, "y", 0f);
            return new Vector3(x, y, z);
        }
    }
}
