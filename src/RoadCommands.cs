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
            if (CostHelpers.OutOfArea(start) || CostHelpers.OutOfArea(end) || CostHelpers.OutOfArea((start + end) * 0.5f))
            {
                return CostHelpers.OutsideOwnedArea("Part of this network");
            }

            // An explicit "y" on a point (e.g. a bridge deck) is kept as an
            // absolute height; otherwise the node sits on the ground/water.
            TerrainManager terrain = TerrainManager.instance;
            if (!HasPointY(body, "start"))
            {
                start.y = terrain.SampleRawHeightSmoothWithWater(start, false, 0f);
            }
            if (!HasPointY(body, "end"))
            {
                end.y = terrain.SampleRawHeightSmoothWithWater(end, false, 0f);
            }

            int cost = CostHelpers.NetCost(info, start, end);
            bool unlocked = CostHelpers.IsUnlocked(info);
            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"message\":\"Build-road validation passed.\",\"roadPrefab\":\"" + JsonUtil.Escape(prefabName) + "\",\"cost\":" + CostHelpers.Money(cost) + ",\"affordable\":" + JsonUtil.Bool(CostHelpers.CanAfford(cost)) + ",\"unlocked\":" + JsonUtil.Bool(unlocked) + "}");
            }
            if (!unlocked)
            {
                return CostHelpers.Locked(prefabName);
            }
            if (!CostHelpers.CanAfford(cost))
            {
                return CostHelpers.NotEnoughMoney(cost);
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

            // Optional "control" point bends the segment into a smooth curve, the
            // way the game's curved road tool does (start/end tangents aim at it).
            Vector3 startDirection = (end - start).normalized;
            Vector3 endDirection = -startDirection;
            if (!float.IsNaN(JsonUtil.GetPointNumber(body, "control", "x", float.NaN)))
            {
                Vector3 control = ReadPoint(body, "control");
                control.y = (start.y + end.y) * 0.5f;
                Vector3 toControl = control - start;
                Vector3 fromControl = control - end;
                toControl.y = 0f;
                fromControl.y = 0f;
                if (toControl.sqrMagnitude > 1f && fromControl.sqrMagnitude > 1f)
                {
                    startDirection = toControl.normalized;
                    endDirection = fromControl.normalized;
                }
            }
            ushort segment;
            bool created = net.CreateSegment(
                out segment,
                ref randomizer,
                info,
                startNode,
                endNode,
                startDirection,
                endDirection,
                simulation.m_currentBuildIndex,
                simulation.m_currentBuildIndex,
                false);

            simulation.m_randomizer = randomizer;

            if (!created)
            {
                return CommandResult.Fail("Failed to create road segment.");
            }

            simulation.m_currentBuildIndex += 2u;
            CostHelpers.Charge(cost, info.m_class);

            if (name != null && name.Length > 0)
            {
                net.SetSegmentNameImpl(segment, name);
            }

            string json = "{\"ok\":true,\"dryRun\":false,\"segmentId\":" + segment +
                ",\"startNodeId\":" + startNode +
                ",\"endNodeId\":" + endNode +
                ",\"cost\":" + CostHelpers.Money(cost) +
                ",\"roadPrefab\":\"" + JsonUtil.Escape(prefabName) + "\"}";

            Debug.Log("[SkylinesAgentBridge] Built road segment " + segment + " with prefab " + prefabName);
            return CommandResult.FromJson(json);
        }

        private static ushort FindNearbyNode(Vector3 position, float maxDistance, NetInfo info)
        {
            // Grid lookup instead of scanning all nodes; returns the nearest match.
            NetManager net = NetManager.instance;
            float bestSq = maxDistance * maxDistance;
            ushort best = 0;

            foreach (ushort i in GridHelpers.NodesNear(position, maxDistance))
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
                if (delta.sqrMagnitude <= bestSq)
                {
                    bestSq = delta.sqrMagnitude;
                    best = i;
                }
            }

            return best;
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

        private static bool HasPointY(string body, string name)
        {
            return !float.IsNaN(JsonUtil.GetPointNumber(body, name, "y", float.NaN));
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
