using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class GameState
    {
        public static CommandResult BuildSummaryJson()
        {
            SimulationManager simulation = Singleton<SimulationManager>.instance;
            NetManager net = NetManager.instance;
            ZoneManager zones = ZoneManager.instance;
            CitizenManager citizens = CitizenManager.instance;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"gameTime\":\"").Append(JsonUtil.Escape(simulation.m_currentGameTime.ToString("s"))).Append("\"");
            json.Append(",\"buildIndex\":").Append(simulation.m_currentBuildIndex);
            json.Append(",\"simulation\":{\"paused\":").Append(JsonUtil.Bool(simulation.SimulationPaused));
            json.Append(",\"selectedSpeed\":").Append(simulation.SelectedSimulationSpeed);
            json.Append(",\"finalSpeed\":").Append(simulation.FinalSimulationSpeed).Append("}");
            json.Append(",\"network\":{\"nodes\":").Append(net.m_nodeCount);
            json.Append(",\"segments\":").Append(net.m_segmentCount);
            json.Append(",\"lanes\":").Append(net.m_laneCount).Append("}");
            json.Append(",\"citizens\":{\"count\":").Append(citizens.m_citizenCount).Append("}");
            json.Append(",\"demand\":{\"residential\":").Append(zones.m_residentialDemand);
            json.Append(",\"commercial\":").Append(zones.m_commercialDemand);
            json.Append(",\"workplace\":").Append(zones.m_workplaceDemand).Append("}");
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult BuildDemandJson()
        {
            ZoneManager zones = ZoneManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"residential\":").Append(zones.m_residentialDemand);
            json.Append(",\"commercial\":").Append(zones.m_commercialDemand);
            json.Append(",\"workplace\":").Append(zones.m_workplaceDemand);
            json.Append(",\"bars\":[");
            AppendDemandBar(json, "Residential", "residential", zones.m_residentialDemand, "#7fff00", true);
            AppendDemandBar(json, "Commercial", "commercial", zones.m_commercialDemand, "#30c8ff", false);
            AppendDemandBar(json, "Workplace", "workplace", zones.m_workplaceDemand, "#ffd21a", false);
            json.Append("]}");
            return CommandResult.FromJson(json.ToString());
        }

        private static void AppendDemandBar(StringBuilder json, string type, string key, int value, string color, bool first)
        {
            if (!first)
            {
                json.Append(",");
            }
            json.Append("{\"type\":\"").Append(type).Append("\"");
            json.Append(",\"key\":\"").Append(key).Append("\"");
            json.Append(",\"value\":").Append(value);
            json.Append(",\"max\":100");
            json.Append(",\"color\":\"").Append(color).Append("\"}");
        }

        public static CommandResult BuildRoadPrefabsJson()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"roads\":[");

            int count = PrefabCollection<NetInfo>.LoadedCount();
            bool first = true;
            for (int i = 0; i < count; i++)
            {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded((uint)i);
                if (info == null || info.m_class == null)
                {
                    continue;
                }

                if (info.m_class.m_service != ItemClass.Service.Road)
                {
                    continue;
                }

                if (!first)
                {
                    json.Append(",");
                }

                json.Append("{\"name\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
                json.Append(",\"displayName\":\"").Append(JsonUtil.Escape(info.GetUncheckedLocalizedTitle())).Append("\"}");
                first = false;
            }

            json.Append("]}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult BuildNetworkPrefabsJson(string serviceFilter)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"networks\":[");

            int count = PrefabCollection<NetInfo>.LoadedCount();
            bool first = true;
            for (int i = 0; i < count; i++)
            {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded((uint)i);
                if (info == null || info.m_class == null)
                {
                    continue;
                }
                string service = info.m_class.m_service.ToString();
                if (serviceFilter != null && serviceFilter.Length > 0 && service != serviceFilter)
                {
                    continue;
                }

                if (!first)
                {
                    json.Append(",");
                }

                json.Append("{\"name\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
                json.Append(",\"displayName\":\"").Append(JsonUtil.Escape(info.GetUncheckedLocalizedTitle())).Append("\"");
                json.Append(",\"service\":\"").Append(JsonUtil.Escape(service)).Append("\"");
                json.Append(",\"subService\":\"").Append(JsonUtil.Escape(info.m_class.m_subService.ToString())).Append("\"}");
                first = false;
            }

            json.Append("]}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult BuildBuildingPrefabsJson(string serviceFilter)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"buildings\":[");

            int count = PrefabCollection<BuildingInfo>.LoadedCount();
            bool first = true;
            for (int i = 0; i < count; i++)
            {
                BuildingInfo info = PrefabCollection<BuildingInfo>.GetLoaded((uint)i);
                if (info == null || info.m_class == null)
                {
                    continue;
                }
                if (AssetPolicy.IsBlockedBuildingPrefab(info))
                {
                    continue;
                }

                string service = info.m_class.m_service.ToString();
                if (serviceFilter != null && serviceFilter.Length > 0 && service != serviceFilter)
                {
                    continue;
                }

                if (!first)
                {
                    json.Append(",");
                }

                json.Append("{\"name\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
                json.Append(",\"displayName\":\"").Append(JsonUtil.Escape(info.GetUncheckedLocalizedTitle())).Append("\"");
                json.Append(",\"service\":\"").Append(JsonUtil.Escape(service)).Append("\"");
                json.Append(",\"subService\":\"").Append(JsonUtil.Escape(info.m_class.m_subService.ToString())).Append("\"");
                json.Append(",\"level\":\"").Append(JsonUtil.Escape(info.m_class.m_level.ToString())).Append("\"");
                json.Append(",\"width\":").Append(info.GetWidth());
                json.Append(",\"length\":").Append(info.GetLength()).Append("}");
                first = false;
            }

            json.Append("]}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult BuildProblemsJson(int limit)
        {
            if (limit < 0)
            {
                limit = 0;
            }
            if (limit > 1000)
            {
                limit = 1000;
            }

            ProblemCollector collector = new ProblemCollector(limit);
            collector.CollectBuildings();
            collector.CollectNetNodes();
            collector.CollectNetSegments();
            return CommandResult.FromJson(collector.ToJson());
        }

        public static CommandResult BuildEconomyJson()
        {
            return EconomyCommands.BuildEconomyJson();
        }

        public static CommandResult BuildFacilitiesJson(int limit, string serviceFilter, bool includeMapObjects)
        {
            if (limit < 0)
            {
                limit = 0;
            }
            if (limit > 5000)
            {
                limit = 5000;
            }

            BuildingManager manager = BuildingManager.instance;
            StringBuilder items = new StringBuilder();
            StringBuilder services = new StringBuilder();
            StringBuilder subServices = new StringBuilder();
            System.Collections.Generic.Dictionary<string, int> countByService = new System.Collections.Generic.Dictionary<string, int>();
            System.Collections.Generic.Dictionary<string, int> countBySubService = new System.Collections.Generic.Dictionary<string, int>();
            int total = 0;
            int emitted = 0;
            bool firstItem = true;

            for (ushort i = 1; i < manager.m_buildings.m_buffer.Length; i++)
            {
                Building building = manager.m_buildings.m_buffer[i];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    continue;
                }

                BuildingInfo info = building.Info;
                if (info == null || info.m_class == null)
                {
                    continue;
                }

                if (!includeMapObjects && IsInternalNetworkHelperBuilding(info))
                {
                    continue;
                }

                string service = info.m_class.m_service.ToString();
                string subService = info.m_class.m_subService.ToString();
                string subServiceKey = service + "/" + subService;

                if (!includeMapObjects && (serviceFilter == null || serviceFilter.Length == 0) && !IsCityFacilityService(service))
                {
                    continue;
                }

                if (serviceFilter != null && serviceFilter.Length > 0 && service != serviceFilter)
                {
                    continue;
                }

                if (countByService.ContainsKey(service))
                {
                    countByService[service]++;
                }
                else
                {
                    countByService[service] = 1;
                }

                if (countBySubService.ContainsKey(subServiceKey))
                {
                    countBySubService[subServiceKey]++;
                }
                else
                {
                    countBySubService[subServiceKey] = 1;
                }

                total++;
                if (emitted >= limit)
                {
                    continue;
                }

                if (!firstItem)
                {
                    items.Append(",");
                }

                string problems = building.m_problems.IsNone ? "" : building.m_problems.ToString();
                Vector3 position = building.m_position;
                items.Append("{\"id\":").Append(i);
                items.Append(",\"prefab\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
                items.Append(",\"displayName\":\"").Append(JsonUtil.Escape(info.GetUncheckedLocalizedTitle())).Append("\"");
                items.Append(",\"blockedAsset\":").Append(JsonUtil.Bool(AssetPolicy.IsBlockedBuildingPrefab(info)));
                items.Append(",\"active\":").Append(JsonUtil.Bool((building.m_flags & Building.Flags.Active) != Building.Flags.None));
                items.Append(",\"flags\":\"").Append(JsonUtil.Escape(building.m_flags.ToString())).Append("\"");
                items.Append(",\"service\":\"").Append(JsonUtil.Escape(service)).Append("\"");
                items.Append(",\"subService\":\"").Append(JsonUtil.Escape(subService)).Append("\"");
                items.Append(",\"level\":\"").Append(JsonUtil.Escape(info.m_class.m_level.ToString())).Append("\"");
                items.Append(",\"width\":").Append(info.GetWidth());
                items.Append(",\"length\":").Append(info.GetLength());
                items.Append(",\"angleDegrees\":").Append(JsonUtil.Number(building.m_angle * Mathf.Rad2Deg));
                items.Append(",\"problems\":\"").Append(JsonUtil.Escape(problems)).Append("\"");
                items.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(position.x));
                items.Append(",\"y\":").Append(JsonUtil.Number(position.y));
                items.Append(",\"z\":").Append(JsonUtil.Number(position.z)).Append("}}");
                firstItem = false;
                emitted++;
            }

            bool first = true;
            foreach (System.Collections.Generic.KeyValuePair<string, int> pair in countByService)
            {
                if (!first)
                {
                    services.Append(",");
                }
                services.Append("\"").Append(JsonUtil.Escape(pair.Key)).Append("\":").Append(pair.Value);
                first = false;
            }

            first = true;
            foreach (System.Collections.Generic.KeyValuePair<string, int> pair in countBySubService)
            {
                if (!first)
                {
                    subServices.Append(",");
                }
                subServices.Append("\"").Append(JsonUtil.Escape(pair.Key)).Append("\":").Append(pair.Value);
                first = false;
            }

            return CommandResult.FromJson("{\"ok\":true,\"total\":" + total +
                ",\"returned\":" + emitted +
                ",\"limit\":" + limit +
                ",\"serviceFilter\":\"" + JsonUtil.Escape(serviceFilter) + "\"" +
                ",\"includeMapObjects\":" + JsonUtil.Bool(includeMapObjects) +
                ",\"countsByService\":{" + services.ToString() + "}" +
                ",\"countsBySubService\":{" + subServices.ToString() + "}" +
                ",\"facilities\":[" + items.ToString() + "]}");
        }

        public static CommandResult BuildNetworksJson(int limit, string serviceFilter)
        {
            if (limit < 0)
            {
                limit = 0;
            }
            if (limit > 5000)
            {
                limit = 5000;
            }

            NetManager manager = NetManager.instance;
            StringBuilder items = new StringBuilder();
            StringBuilder services = new StringBuilder();
            System.Collections.Generic.Dictionary<string, int> countByService = new System.Collections.Generic.Dictionary<string, int>();
            int total = 0;
            int emitted = 0;
            bool firstItem = true;

            for (ushort i = 1; i < manager.m_segments.m_buffer.Length; i++)
            {
                NetSegment segment = manager.m_segments.m_buffer[i];
                if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    continue;
                }

                NetInfo info = segment.Info;
                if (info == null || info.m_class == null)
                {
                    continue;
                }

                string service = info.m_class.m_service.ToString();
                if (serviceFilter != null && serviceFilter.Length > 0 && service != serviceFilter)
                {
                    continue;
                }

                if (countByService.ContainsKey(service))
                {
                    countByService[service]++;
                }
                else
                {
                    countByService[service] = 1;
                }

                total++;
                if (emitted >= limit)
                {
                    continue;
                }

                ushort startNodeId = segment.m_startNode;
                ushort endNodeId = segment.m_endNode;
                Vector3 start = manager.m_nodes.m_buffer[startNodeId].m_position;
                Vector3 end = manager.m_nodes.m_buffer[endNodeId].m_position;
                Vector3 middle = segment.m_middlePosition;
                string problems = segment.m_problems.IsNone ? "" : segment.m_problems.ToString();
                string segmentName = manager.GetSegmentName(i);

                if (!firstItem)
                {
                    items.Append(",");
                }

                items.Append("{\"id\":").Append(i);
                items.Append(",\"prefab\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
                items.Append(",\"name\":\"").Append(JsonUtil.Escape(segmentName)).Append("\"");
                items.Append(",\"displayName\":\"").Append(JsonUtil.Escape(info.GetUncheckedLocalizedTitle())).Append("\"");
                items.Append(",\"service\":\"").Append(JsonUtil.Escape(service)).Append("\"");
                items.Append(",\"subService\":\"").Append(JsonUtil.Escape(info.m_class.m_subService.ToString())).Append("\"");
                items.Append(",\"problems\":\"").Append(JsonUtil.Escape(problems)).Append("\"");
                items.Append(",\"startNodeId\":").Append(startNodeId);
                items.Append(",\"endNodeId\":").Append(endNodeId);
                items.Append(",\"start\":{\"x\":").Append(JsonUtil.Number(start.x));
                items.Append(",\"y\":").Append(JsonUtil.Number(start.y));
                items.Append(",\"z\":").Append(JsonUtil.Number(start.z)).Append("}");
                items.Append(",\"end\":{\"x\":").Append(JsonUtil.Number(end.x));
                items.Append(",\"y\":").Append(JsonUtil.Number(end.y));
                items.Append(",\"z\":").Append(JsonUtil.Number(end.z)).Append("}");
                items.Append(",\"middle\":{\"x\":").Append(JsonUtil.Number(middle.x));
                items.Append(",\"y\":").Append(JsonUtil.Number(middle.y));
                items.Append(",\"z\":").Append(JsonUtil.Number(middle.z)).Append("}}");
                firstItem = false;
                emitted++;
            }

            bool first = true;
            foreach (System.Collections.Generic.KeyValuePair<string, int> pair in countByService)
            {
                if (!first)
                {
                    services.Append(",");
                }
                services.Append("\"").Append(JsonUtil.Escape(pair.Key)).Append("\":").Append(pair.Value);
                first = false;
            }

            return CommandResult.FromJson("{\"ok\":true,\"total\":" + total +
                ",\"returned\":" + emitted +
                ",\"limit\":" + limit +
                ",\"serviceFilter\":\"" + JsonUtil.Escape(serviceFilter) + "\"" +
                ",\"countsByService\":{" + services.ToString() + "}" +
                ",\"segments\":[" + items.ToString() + "]}");
        }

        public static CommandResult BuildRoadAnomaliesJson(int limit, float nearMissDistance, float shortSegmentLength, bool includeDeadEnds)
        {
            if (limit < 0)
            {
                limit = 0;
            }
            if (limit > 1000)
            {
                limit = 1000;
            }
            if (nearMissDistance < 1f)
            {
                nearMissDistance = 1f;
            }
            if (shortSegmentLength < 1f)
            {
                shortSegmentLength = 1f;
            }

            RoadAnomalyCollector collector = new RoadAnomalyCollector(limit, nearMissDistance, shortSegmentLength, includeDeadEnds);
            collector.Collect();
            return CommandResult.FromJson(collector.ToJson());
        }

        public static CommandResult BuildBuildingAnomaliesJson(int limit)
        {
            if (limit < 0)
            {
                limit = 0;
            }
            if (limit > 1000)
            {
                limit = 1000;
            }

            BuildingAnomalyCollector collector = new BuildingAnomalyCollector(limit);
            collector.Collect();
            return CommandResult.FromJson(collector.ToJson());
        }

        private static bool IsCityFacilityService(string service)
        {
            return service == "Water" ||
                service == "Electricity" ||
                service == "Garbage" ||
                service == "HealthCare" ||
                service == "PoliceDepartment" ||
                service == "FireDepartment" ||
                service == "Education" ||
                service == "Disaster";
        }

        private static bool IsInternalNetworkHelperBuilding(BuildingInfo info)
        {
            return info != null &&
                (info.name == "Water Pipe Junction" || info.name == "Heating Pipe Junction");
        }

        private sealed class BuildingAnomalyCollector
        {
            private readonly int limit;
            private readonly StringBuilder items = new StringBuilder();
            private int total;
            private int emitted;
            private bool firstItem = true;

            public BuildingAnomalyCollector(int limit)
            {
                this.limit = limit;
            }

            public void Collect()
            {
                BuildingManager buildings = BuildingManager.instance;
                NetManager net = NetManager.instance;

                for (ushort buildingId = 1; buildingId < buildings.m_buildings.m_buffer.Length; buildingId++)
                {
                    Building building = buildings.m_buildings.m_buffer[buildingId];
                    if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
                    {
                        continue;
                    }

                    BuildingInfo buildingInfo = building.Info;
                    if (buildingInfo == null || buildingInfo.m_class == null)
                    {
                        continue;
                    }

                    if (IsInternalNetworkHelperBuilding(buildingInfo))
                    {
                        continue;
                    }

                    if (!IsCityFacilityService(buildingInfo.m_class.m_service.ToString()))
                    {
                        continue;
                    }

                    for (ushort segmentId = 1; segmentId < net.m_segments.m_buffer.Length; segmentId++)
                    {
                        NetSegment segment = net.m_segments.m_buffer[segmentId];
                        if (!IsCreatedRoadSegment(segment))
                        {
                            continue;
                        }

                        Vector3 start = net.m_nodes.m_buffer[segment.m_startNode].m_position;
                        Vector3 end = net.m_nodes.m_buffer[segment.m_endNode].m_position;
                        if (RoadCrossesBuildingFootprint(building, buildingInfo, start, end))
                        {
                            AddRoadOverlap(buildingId, building, buildingInfo, segmentId);
                            break;
                        }
                    }
                }
            }

            public string ToJson()
            {
                return "{\"ok\":true,\"total\":" + total +
                    ",\"returned\":" + emitted +
                    ",\"limit\":" + limit +
                    ",\"counts\":{\"buildingRoadOverlap\":" + total + "}" +
                    ",\"anomalies\":[" + items.ToString() + "]}";
            }

            private void AddRoadOverlap(ushort buildingId, Building building, BuildingInfo info, ushort segmentId)
            {
                total++;
                if (emitted >= limit)
                {
                    return;
                }

                if (!firstItem)
                {
                    items.Append(",");
                }

                Vector3 position = building.m_position;
                items.Append("{\"type\":\"buildingRoadOverlap\"");
                items.Append(",\"buildingId\":").Append(buildingId);
                items.Append(",\"segmentId\":").Append(segmentId);
                items.Append(",\"prefab\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
                items.Append(",\"displayName\":\"").Append(JsonUtil.Escape(info.GetUncheckedLocalizedTitle())).Append("\"");
                items.Append(",\"service\":\"").Append(JsonUtil.Escape(info.m_class.m_service.ToString())).Append("\"");
                items.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(position.x));
                items.Append(",\"y\":").Append(JsonUtil.Number(position.y));
                items.Append(",\"z\":").Append(JsonUtil.Number(position.z)).Append("}}");
                emitted++;
                firstItem = false;
            }

            private static bool RoadCrossesBuildingFootprint(Building building, BuildingInfo info, Vector3 roadStart, Vector3 roadEnd)
            {
                float halfWidth = Mathf.Max(1f, info.GetWidth() * 4f - 1.5f);
                float halfLength = Mathf.Max(1f, info.GetLength() * 4f - 1.5f);
                Vector2 start = ToLocal(building, roadStart);
                Vector2 end = ToLocal(building, roadEnd);

                return SegmentIntersectsAabb(start, end, -halfWidth, halfWidth, -halfLength, halfLength);
            }

            private static Vector2 ToLocal(Building building, Vector3 point)
            {
                float dx = point.x - building.m_position.x;
                float dz = point.z - building.m_position.z;
                float cos = Mathf.Cos(-building.m_angle);
                float sin = Mathf.Sin(-building.m_angle);
                return new Vector2(dx * cos - dz * sin, dx * sin + dz * cos);
            }

            private static bool SegmentIntersectsAabb(Vector2 start, Vector2 end, float minX, float maxX, float minY, float maxY)
            {
                float t0 = 0f;
                float t1 = 1f;
                float dx = end.x - start.x;
                float dy = end.y - start.y;

                return Clip(-dx, start.x - minX, ref t0, ref t1) &&
                    Clip(dx, maxX - start.x, ref t0, ref t1) &&
                    Clip(-dy, start.y - minY, ref t0, ref t1) &&
                    Clip(dy, maxY - start.y, ref t0, ref t1);
            }

            private static bool Clip(float p, float q, ref float t0, ref float t1)
            {
                if (Mathf.Abs(p) < 0.0001f)
                {
                    return q >= 0f;
                }

                float r = q / p;
                if (p < 0f)
                {
                    if (r > t1) return false;
                    if (r > t0) t0 = r;
                }
                else
                {
                    if (r < t0) return false;
                    if (r < t1) t1 = r;
                }
                return true;
            }

            private static bool IsCreatedRoadSegment(NetSegment segment)
            {
                return (segment.m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None &&
                    segment.Info != null &&
                    segment.Info.m_class != null &&
                    segment.Info.m_class.m_service == ItemClass.Service.Road;
            }
        }

        private sealed class RoadAnomalyCollector
        {
            private const float RoadOverlapDistance = 3f;
            private const float RoadCrossingHeightTolerance = 5f;
            private const float RoadOverlapMinLength = 18f;
            private const float RoadTerrainSideOffset = 22f;
            private const float RoadTerrainHeightTolerance = 18f;
            private const float RoadTerrainSideDeltaTolerance = 28f;
            private const float RoadTerrainAdjacentDeltaTolerance = 14f;
            private const float RoadTerrainBoundsMargin = 700f;
            private readonly int limit;
            private readonly float nearMissDistance;
            private readonly float shortSegmentLength;
            private readonly bool includeDeadEnds;
            private readonly StringBuilder items = new StringBuilder();
            private readonly StringBuilder counts = new StringBuilder();
            private readonly System.Collections.Generic.Dictionary<string, int> countByType = new System.Collections.Generic.Dictionary<string, int>();
            private int total;
            private int emitted;
            private bool firstItem = true;

            public RoadAnomalyCollector(int limit, float nearMissDistance, float shortSegmentLength, bool includeDeadEnds)
            {
                this.limit = limit;
                this.nearMissDistance = nearMissDistance;
                this.shortSegmentLength = shortSegmentLength;
                this.includeDeadEnds = includeDeadEnds;
            }

            public void Collect()
            {
                NetManager manager = NetManager.instance;

                for (ushort i = 1; i < manager.m_segments.m_buffer.Length; i++)
                {
                    NetSegment segment = manager.m_segments.m_buffer[i];
                    if (!IsCreatedRoadSegment(segment))
                    {
                        continue;
                    }

                    Vector3 start = manager.m_nodes.m_buffer[segment.m_startNode].m_position;
                    Vector3 end = manager.m_nodes.m_buffer[segment.m_endNode].m_position;
                    float length = FlatDistance(start, end);
                    int startConnections = CountRoadSegments(manager, segment.m_startNode);
                    int endConnections = CountRoadSegments(manager, segment.m_endNode);

                    if (length <= shortSegmentLength && (startConnections <= 1 || endConnections <= 1))
                    {
                        AddShortSegment(i, segment, start, end, length, startConnections, endConnections);
                    }
                }

                CollectRoadOverlapAnomalies(manager);
                CollectRoadTerrainAnomalies(manager);

                for (ushort i = 1; i < manager.m_nodes.m_buffer.Length; i++)
                {
                    NetNode node = manager.m_nodes.m_buffer[i];
                    if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.None || !IsRoadInfo(node.Info))
                    {
                        continue;
                    }

                    int connections = CountRoadSegments(manager, i);
                    if (connections != 1)
                    {
                        continue;
                    }

                    ushort ownSegment = FirstRoadSegment(manager, i);
                    ushort nearestSegment;
                    float distance;
                    FindNearestRoadSegment(manager, i, node.m_position, ownSegment, out nearestSegment, out distance);
                    if (nearestSegment != 0 && distance <= nearMissDistance)
                    {
                        AddNearMissNode(i, ownSegment, nearestSegment, node.m_position, distance);
                    }
                    else if (includeDeadEnds)
                    {
                        AddDeadEndNode(i, ownSegment, node.m_position);
                    }
                }
            }

            public string ToJson()
            {
                bool first = true;
                foreach (System.Collections.Generic.KeyValuePair<string, int> pair in countByType)
                {
                    if (!first)
                    {
                        counts.Append(",");
                    }
                    counts.Append("\"").Append(JsonUtil.Escape(pair.Key)).Append("\":").Append(pair.Value);
                    first = false;
                }

                return "{\"ok\":true,\"total\":" + total +
                    ",\"returned\":" + emitted +
                    ",\"limit\":" + limit +
                    ",\"nearMissDistance\":" + JsonUtil.Number(nearMissDistance) +
                    ",\"shortSegmentLength\":" + JsonUtil.Number(shortSegmentLength) +
                    ",\"includeDeadEnds\":" + JsonUtil.Bool(includeDeadEnds) +
                    ",\"counts\":{" + counts.ToString() + "}" +
                    ",\"anomalies\":[" + items.ToString() + "]}";
            }

            private void AddShortSegment(ushort segmentId, NetSegment segment, Vector3 start, Vector3 end, float length, int startConnections, int endConnections)
            {
                if (Begin("shortRoadStub"))
                {
                    items.Append(",\"segmentId\":").Append(segmentId);
                    items.Append(",\"prefab\":\"").Append(JsonUtil.Escape(segment.Info.name)).Append("\"");
                    items.Append(",\"length\":").Append(JsonUtil.Number(length));
                    items.Append(",\"startConnections\":").Append(startConnections);
                    items.Append(",\"endConnections\":").Append(endConnections);
                    AppendPoint("start", start);
                    AppendPoint("end", end);
                    items.Append("}");
                }
            }

            private void AddNearMissNode(ushort nodeId, ushort ownSegment, ushort nearestSegment, Vector3 position, float distance)
            {
                if (Begin("deadEndNearRoad"))
                {
                    items.Append(",\"nodeId\":").Append(nodeId);
                    items.Append(",\"ownSegmentId\":").Append(ownSegment);
                    items.Append(",\"nearestSegmentId\":").Append(nearestSegment);
                    items.Append(",\"distance\":").Append(JsonUtil.Number(distance));
                    AppendPoint("position", position);
                    items.Append("}");
                }
            }

            private void AddDeadEndNode(ushort nodeId, ushort ownSegment, Vector3 position)
            {
                if (Begin("deadEndRoad"))
                {
                    items.Append(",\"nodeId\":").Append(nodeId);
                    items.Append(",\"ownSegmentId\":").Append(ownSegment);
                    AppendPoint("position", position);
                    items.Append("}");
                }
            }

            private void AddOverlappingRoadSegments(ushort segmentAId, ushort segmentBId, NetSegment segmentA, NetSegment segmentB, float distance, float overlapLength)
            {
                if (Begin("overlappingRoadSegments"))
                {
                    items.Append(",\"segmentAId\":").Append(segmentAId);
                    items.Append(",\"segmentBId\":").Append(segmentBId);
                    items.Append(",\"prefabA\":\"").Append(JsonUtil.Escape(segmentA.Info.name)).Append("\"");
                    items.Append(",\"prefabB\":\"").Append(JsonUtil.Escape(segmentB.Info.name)).Append("\"");
                    items.Append(",\"distance\":").Append(JsonUtil.Number(distance));
                    items.Append(",\"overlapLength\":").Append(JsonUtil.Number(overlapLength));
                    AppendSegment("segmentA", segmentA);
                    AppendSegment("segmentB", segmentB);
                    items.Append("}");
                }
            }

            private void AddCrossingRoadWithoutNode(ushort segmentAId, ushort segmentBId, NetSegment segmentA, NetSegment segmentB, Vector3 position, float heightDifference)
            {
                if (Begin("roadCrossingWithoutNode"))
                {
                    items.Append(",\"segmentAId\":").Append(segmentAId);
                    items.Append(",\"segmentBId\":").Append(segmentBId);
                    items.Append(",\"prefabA\":\"").Append(JsonUtil.Escape(segmentA.Info.name)).Append("\"");
                    items.Append(",\"prefabB\":\"").Append(JsonUtil.Escape(segmentB.Info.name)).Append("\"");
                    items.Append(",\"heightDifference\":").Append(JsonUtil.Number(heightDifference));
                    AppendPoint("position", position);
                    AppendSegment("segmentA", segmentA);
                    AppendSegment("segmentB", segmentB);
                    items.Append("}");
                }
            }

            private void AddDuplicateRoadSegments(ushort segmentAId, ushort segmentBId, NetSegment segmentA, NetSegment segmentB)
            {
                if (Begin("duplicateRoadSegments"))
                {
                    items.Append(",\"segmentAId\":").Append(segmentAId);
                    items.Append(",\"segmentBId\":").Append(segmentBId);
                    items.Append(",\"prefabA\":\"").Append(JsonUtil.Escape(segmentA.Info.name)).Append("\"");
                    items.Append(",\"prefabB\":\"").Append(JsonUtil.Escape(segmentB.Info.name)).Append("\"");
                    AppendSegment("segmentA", segmentA);
                    AppendSegment("segmentB", segmentB);
                    items.Append("}");
                }
            }

            private void AddRoadTerrainCliff(ushort segmentId, NetSegment segment, float maxRoadToTerrainDelta, float maxSideToSideDelta, float maxAdjacentTerrainDelta, Vector3 samplePosition)
            {
                if (Begin("roadTerrainCliff"))
                {
                    items.Append(",\"segmentId\":").Append(segmentId);
                    items.Append(",\"name\":\"").Append(JsonUtil.Escape(NetManager.instance.GetSegmentName(segmentId))).Append("\"");
                    items.Append(",\"prefab\":\"").Append(JsonUtil.Escape(segment.Info.name)).Append("\"");
                    items.Append(",\"maxRoadToTerrainDelta\":").Append(JsonUtil.Number(maxRoadToTerrainDelta));
                    items.Append(",\"maxSideToSideDelta\":").Append(JsonUtil.Number(maxSideToSideDelta));
                    items.Append(",\"maxAdjacentTerrainDelta\":").Append(JsonUtil.Number(maxAdjacentTerrainDelta));
                    AppendPoint("samplePosition", samplePosition);
                    AppendSegment("segment", segment);
                    items.Append("}");
                }
            }

            private bool Begin(string type)
            {
                total++;
                if (countByType.ContainsKey(type))
                {
                    countByType[type]++;
                }
                else
                {
                    countByType[type] = 1;
                }

                if (emitted >= limit)
                {
                    return false;
                }

                if (!firstItem)
                {
                    items.Append(",");
                }
                items.Append("{\"type\":\"").Append(type).Append("\"");
                emitted++;
                firstItem = false;
                return true;
            }

            private void AppendPoint(string name, Vector3 point)
            {
                items.Append(",\"").Append(name).Append("\":{\"x\":").Append(JsonUtil.Number(point.x));
                items.Append(",\"y\":").Append(JsonUtil.Number(point.y));
                items.Append(",\"z\":").Append(JsonUtil.Number(point.z)).Append("}");
            }

            private void AppendSegment(string name, NetSegment segment)
            {
                NetManager manager = NetManager.instance;
                Vector3 start = manager.m_nodes.m_buffer[segment.m_startNode].m_position;
                Vector3 end = manager.m_nodes.m_buffer[segment.m_endNode].m_position;
                items.Append(",\"").Append(name).Append("\":{");
                items.Append("\"startNodeId\":").Append(segment.m_startNode);
                items.Append(",\"endNodeId\":").Append(segment.m_endNode);
                AppendPoint("start", start);
                AppendPoint("end", end);
                items.Append("}");
            }

            private void CollectRoadOverlapAnomalies(NetManager manager)
            {
                for (ushort a = 1; a < manager.m_segments.m_buffer.Length; a++)
                {
                    NetSegment segmentA = manager.m_segments.m_buffer[a];
                    if (!IsCreatedRoadSegment(segmentA))
                    {
                        continue;
                    }

                    Vector3 aStart = manager.m_nodes.m_buffer[segmentA.m_startNode].m_position;
                    Vector3 aEnd = manager.m_nodes.m_buffer[segmentA.m_endNode].m_position;

                    for (ushort b = (ushort)(a + 1); b < manager.m_segments.m_buffer.Length; b++)
                    {
                        NetSegment segmentB = manager.m_segments.m_buffer[b];
                        if (!IsCreatedRoadSegment(segmentB))
                        {
                            continue;
                        }

                        if (SameEndpoints(segmentA, segmentB))
                        {
                            AddDuplicateRoadSegments(a, b, segmentA, segmentB);
                            continue;
                        }

                        if (SharesNode(segmentA, segmentB))
                        {
                            continue;
                        }

                        Vector3 bStart = manager.m_nodes.m_buffer[segmentB.m_startNode].m_position;
                        Vector3 bEnd = manager.m_nodes.m_buffer[segmentB.m_endNode].m_position;

                        Vector3 crossing;
                        float heightDifference;
                        if (SegmentsCrossWithoutNode(aStart, aEnd, bStart, bEnd, out crossing, out heightDifference))
                        {
                            AddCrossingRoadWithoutNode(a, b, segmentA, segmentB, crossing, heightDifference);
                            continue;
                        }

                        float distance;
                        float overlapLength;
                        if (SegmentsOverlap(aStart, aEnd, bStart, bEnd, out distance, out overlapLength))
                        {
                            AddOverlappingRoadSegments(a, b, segmentA, segmentB, distance, overlapLength);
                        }
                    }
                }
            }

            private void CollectRoadTerrainAnomalies(NetManager manager)
            {
                TerrainManager terrain = TerrainManager.instance;
                if (terrain == null)
                {
                    return;
                }

                float minX;
                float maxX;
                float minZ;
                float maxZ;
                if (!TryGetTerrainCheckBounds(manager, out minX, out maxX, out minZ, out maxZ))
                {
                    return;
                }

                for (ushort i = 1; i < manager.m_segments.m_buffer.Length; i++)
                {
                    NetSegment segment = manager.m_segments.m_buffer[i];
                    if (!IsCreatedRoadSegment(segment) || SkipTerrainAnomalyCheck(segment.Info))
                    {
                        continue;
                    }

                    Vector3 start = manager.m_nodes.m_buffer[segment.m_startNode].m_position;
                    Vector3 end = manager.m_nodes.m_buffer[segment.m_endNode].m_position;
                    if (!IsAgentNamedRoad(i) && !SegmentTouchesBounds(start, end, minX, maxX, minZ, maxZ))
                    {
                        continue;
                    }

                    Vector3 direction = end - start;
                    direction.y = 0f;
                    float length = Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z);
                    if (length < 24f)
                    {
                        continue;
                    }

                    direction.x /= length;
                    direction.z /= length;
                    Vector3 side = new Vector3(-direction.z, 0f, direction.x);

                    float maxRoadToTerrainDelta = 0f;
                    float maxSideToSideDelta = 0f;
                    float maxAdjacentTerrainDelta = 0f;
                    Vector3 worstPoint = segment.m_middlePosition;

                    for (int sample = 1; sample <= 3; sample++)
                    {
                        float t = sample * 0.25f;
                        Vector3 center = Vector3.Lerp(start, end, t);
                        float roadY = Mathf.Lerp(start.y, end.y, t);
                        float leftY = SampleRoadSideTerrain(terrain, center, side, 1f, roadY, ref maxRoadToTerrainDelta, ref maxAdjacentTerrainDelta);
                        float rightY = SampleRoadSideTerrain(terrain, center, side, -1f, roadY, ref maxRoadToTerrainDelta, ref maxAdjacentTerrainDelta);
                        float sideDelta = Mathf.Abs(leftY - rightY);

                        if (sideDelta > maxSideToSideDelta)
                        {
                            maxSideToSideDelta = sideDelta;
                            worstPoint = center;
                        }
                    }

                    if (IsLongGroundOutsideConnector(i, segment, length))
                    {
                        maxRoadToTerrainDelta = Mathf.Max(maxRoadToTerrainDelta, RoadTerrainHeightTolerance);
                    }

                    if (maxRoadToTerrainDelta >= RoadTerrainHeightTolerance ||
                        maxSideToSideDelta >= RoadTerrainSideDeltaTolerance ||
                        maxAdjacentTerrainDelta >= RoadTerrainAdjacentDeltaTolerance)
                    {
                        AddRoadTerrainCliff(i, segment, maxRoadToTerrainDelta, maxSideToSideDelta, maxAdjacentTerrainDelta, worstPoint);
                    }
                }
            }

            private static float SampleRoadSideTerrain(TerrainManager terrain, Vector3 center, Vector3 side, float sideSign, float roadY, ref float maxRoadToTerrainDelta, ref float maxAdjacentTerrainDelta)
            {
                float previousY = roadY;
                float lastY = roadY;
                float[] offsets = new float[] { 8f, RoadTerrainSideOffset, 44f, 66f, 96f };
                for (int i = 0; i < offsets.Length; i++)
                {
                    Vector3 sample = center + side * (offsets[i] * sideSign);
                    float terrainY = terrain.SampleRawHeightSmoothWithWater(sample, false, 0f);
                    maxRoadToTerrainDelta = Mathf.Max(maxRoadToTerrainDelta, Mathf.Abs(roadY - terrainY));
                    maxAdjacentTerrainDelta = Mathf.Max(maxAdjacentTerrainDelta, Mathf.Abs(previousY - terrainY));
                    previousY = terrainY;
                    lastY = terrainY;
                }
                return lastY;
            }

            private static bool TryGetTerrainCheckBounds(NetManager manager, out float minX, out float maxX, out float minZ, out float maxZ)
            {
                minX = float.MaxValue;
                maxX = float.MinValue;
                minZ = float.MaxValue;
                maxZ = float.MinValue;
                bool found = false;

                for (ushort i = 1; i < manager.m_segments.m_buffer.Length; i++)
                {
                    NetSegment segment = manager.m_segments.m_buffer[i];
                    if (!IsCreatedRoadSegment(segment) || !ContributesToTerrainCheckBounds(i, segment))
                    {
                        continue;
                    }

                    Vector3 start = manager.m_nodes.m_buffer[segment.m_startNode].m_position;
                    Vector3 end = manager.m_nodes.m_buffer[segment.m_endNode].m_position;
                    ExpandBounds(start, ref minX, ref maxX, ref minZ, ref maxZ);
                    ExpandBounds(end, ref minX, ref maxX, ref minZ, ref maxZ);
                    found = true;
                }

                if (!found)
                {
                    return false;
                }

                minX -= RoadTerrainBoundsMargin;
                maxX += RoadTerrainBoundsMargin;
                minZ -= RoadTerrainBoundsMargin;
                maxZ += RoadTerrainBoundsMargin;
                return true;
            }

            private static void ExpandBounds(Vector3 point, ref float minX, ref float maxX, ref float minZ, ref float maxZ)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            private static bool SegmentTouchesBounds(Vector3 start, Vector3 end, float minX, float maxX, float minZ, float maxZ)
            {
                float segmentMinX = Mathf.Min(start.x, end.x);
                float segmentMaxX = Mathf.Max(start.x, end.x);
                float segmentMinZ = Mathf.Min(start.z, end.z);
                float segmentMaxZ = Mathf.Max(start.z, end.z);
                return segmentMaxX >= minX && segmentMinX <= maxX && segmentMaxZ >= minZ && segmentMinZ <= maxZ;
            }

            private static bool ContributesToTerrainCheckBounds(ushort segmentId, NetSegment segment)
            {
                if (IsAgentNamedRoad(segmentId))
                {
                    return true;
                }

                string prefab = segment.Info == null || segment.Info.name == null ? "" : segment.Info.name;
                return !ContainsIgnoreCase(prefab, "Highway");
            }

            private static bool IsAgentNamedRoad(ushort segmentId)
            {
                string name = NetManager.instance.GetSegmentName(segmentId);
                return ContainsIgnoreCase(name, "Agent");
            }

            private static bool IsLongGroundOutsideConnector(ushort segmentId, NetSegment segment, float length)
            {
                if (length < 140f || segment.Info == null)
                {
                    return false;
                }

                string prefab = segment.Info.name == null ? "" : segment.Info.name;
                if (ContainsIgnoreCase(prefab, "Elevated") || ContainsIgnoreCase(prefab, "Bridge") || ContainsIgnoreCase(prefab, "Tunnel") || ContainsIgnoreCase(prefab, "Slope"))
                {
                    return false;
                }

                string name = NetManager.instance.GetSegmentName(segmentId);
                return ContainsIgnoreCase(name, "Agent") && ContainsIgnoreCase(name, "Outside");
            }

            private static void FindNearestRoadSegment(NetManager manager, ushort nodeId, Vector3 point, ushort ownSegment, out ushort nearestSegment, out float distance)
            {
                nearestSegment = 0;
                distance = float.MaxValue;

                for (ushort i = 1; i < manager.m_segments.m_buffer.Length; i++)
                {
                    if (i == ownSegment)
                    {
                        continue;
                    }

                    NetSegment segment = manager.m_segments.m_buffer[i];
                    if (!IsCreatedRoadSegment(segment) || segment.m_startNode == nodeId || segment.m_endNode == nodeId)
                    {
                        continue;
                    }

                    Vector3 start = manager.m_nodes.m_buffer[segment.m_startNode].m_position;
                    Vector3 end = manager.m_nodes.m_buffer[segment.m_endNode].m_position;
                    float candidate = DistancePointToSegment(point, start, end);
                    if (candidate < distance)
                    {
                        distance = candidate;
                        nearestSegment = i;
                    }
                }
            }

            private static float DistancePointToSegment(Vector3 point, Vector3 start, Vector3 end)
            {
                float dx = end.x - start.x;
                float dz = end.z - start.z;
                float lengthSq = dx * dx + dz * dz;
                if (lengthSq < 0.001f)
                {
                    return FlatDistance(point, start);
                }

                float t = ((point.x - start.x) * dx + (point.z - start.z) * dz) / lengthSq;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;

                Vector3 nearest = new Vector3(start.x + t * dx, 0f, start.z + t * dz);
                return FlatDistance(point, nearest);
            }

            private static bool SegmentsCrossWithoutNode(Vector3 aStart, Vector3 aEnd, Vector3 bStart, Vector3 bEnd, out Vector3 crossing, out float heightDifference)
            {
                crossing = Vector3.zero;
                heightDifference = 0f;

                float ax = aEnd.x - aStart.x;
                float az = aEnd.z - aStart.z;
                float bx = bEnd.x - bStart.x;
                float bz = bEnd.z - bStart.z;
                float denom = ax * bz - az * bx;
                if (Mathf.Abs(denom) < 0.0001f)
                {
                    return false;
                }

                float cx = bStart.x - aStart.x;
                float cz = bStart.z - aStart.z;
                float t = (cx * bz - cz * bx) / denom;
                float u = (cx * az - cz * ax) / denom;
                if (t <= 0.04f || t >= 0.96f || u <= 0.04f || u >= 0.96f)
                {
                    return false;
                }

                float aY = Mathf.Lerp(aStart.y, aEnd.y, t);
                float bY = Mathf.Lerp(bStart.y, bEnd.y, u);
                heightDifference = Mathf.Abs(aY - bY);
                if (heightDifference > RoadCrossingHeightTolerance)
                {
                    return false;
                }

                crossing = new Vector3(aStart.x + ax * t, (aY + bY) * 0.5f, aStart.z + az * t);
                return true;
            }

            private static bool SegmentsOverlap(Vector3 aStart, Vector3 aEnd, Vector3 bStart, Vector3 bEnd, out float distance, out float overlapLength)
            {
                distance = Mathf.Min(
                    Mathf.Min(DistancePointToSegment(aStart, bStart, bEnd), DistancePointToSegment(aEnd, bStart, bEnd)),
                    Mathf.Min(DistancePointToSegment(bStart, aStart, aEnd), DistancePointToSegment(bEnd, aStart, aEnd)));
                overlapLength = 0f;

                if (distance > RoadOverlapDistance)
                {
                    return false;
                }

                float aDx = aEnd.x - aStart.x;
                float aDz = aEnd.z - aStart.z;
                float bDx = bEnd.x - bStart.x;
                float bDz = bEnd.z - bStart.z;
                float aLength = Mathf.Sqrt(aDx * aDx + aDz * aDz);
                float bLength = Mathf.Sqrt(bDx * bDx + bDz * bDz);
                if (aLength < 0.001f || bLength < 0.001f)
                {
                    return false;
                }

                float dot = Mathf.Abs((aDx * bDx + aDz * bDz) / (aLength * bLength));
                if (dot < 0.985f)
                {
                    return false;
                }

                float b0 = ProjectAlong(aStart, aEnd, bStart);
                float b1 = ProjectAlong(aStart, aEnd, bEnd);
                if (b0 > b1)
                {
                    float swap = b0;
                    b0 = b1;
                    b1 = swap;
                }

                float overlapStart = Mathf.Max(0f, b0);
                float overlapEnd = Mathf.Min(aLength, b1);
                overlapLength = overlapEnd - overlapStart;
                if (overlapLength < RoadOverlapMinLength)
                {
                    return false;
                }

                float maxHeightDifference = Mathf.Max(
                    Mathf.Abs(aStart.y - bStart.y),
                    Mathf.Abs(aEnd.y - bEnd.y));
                return maxHeightDifference <= RoadCrossingHeightTolerance;
            }

            private static float ProjectAlong(Vector3 start, Vector3 end, Vector3 point)
            {
                float dx = end.x - start.x;
                float dz = end.z - start.z;
                float length = Mathf.Sqrt(dx * dx + dz * dz);
                if (length < 0.001f)
                {
                    return 0f;
                }
                return ((point.x - start.x) * dx + (point.z - start.z) * dz) / length;
            }

            private static float FlatDistance(Vector3 a, Vector3 b)
            {
                float dx = a.x - b.x;
                float dz = a.z - b.z;
                return Mathf.Sqrt(dx * dx + dz * dz);
            }

            private static int CountRoadSegments(NetManager manager, ushort nodeId)
            {
                int count = 0;
                for (int i = 0; i < 8; i++)
                {
                    ushort segmentId = GetSegmentId(manager.m_nodes.m_buffer[nodeId], i);
                    if (segmentId == 0)
                    {
                        continue;
                    }
                    if (IsCreatedRoadSegment(manager.m_segments.m_buffer[segmentId]))
                    {
                        count++;
                    }
                }
                return count;
            }

            private static ushort FirstRoadSegment(NetManager manager, ushort nodeId)
            {
                for (int i = 0; i < 8; i++)
                {
                    ushort segmentId = GetSegmentId(manager.m_nodes.m_buffer[nodeId], i);
                    if (segmentId != 0 && IsCreatedRoadSegment(manager.m_segments.m_buffer[segmentId]))
                    {
                        return segmentId;
                    }
                }
                return 0;
            }

            private static bool SharesNode(NetSegment a, NetSegment b)
            {
                return a.m_startNode == b.m_startNode ||
                    a.m_startNode == b.m_endNode ||
                    a.m_endNode == b.m_startNode ||
                    a.m_endNode == b.m_endNode;
            }

            private static bool SameEndpoints(NetSegment a, NetSegment b)
            {
                return (a.m_startNode == b.m_startNode && a.m_endNode == b.m_endNode) ||
                    (a.m_startNode == b.m_endNode && a.m_endNode == b.m_startNode);
            }

            private static bool SkipTerrainAnomalyCheck(NetInfo info)
            {
                if (info == null)
                {
                    return true;
                }

                string name = info.name == null ? "" : info.name;
                string aiName = info.m_netAI == null ? "" : info.m_netAI.GetType().Name;
                return ContainsIgnoreCase(name, "Elevated") ||
                    ContainsIgnoreCase(name, "Bridge") ||
                    ContainsIgnoreCase(name, "Tunnel") ||
                    ContainsIgnoreCase(name, "Slope") ||
                    ContainsIgnoreCase(aiName, "Elevated") ||
                    ContainsIgnoreCase(aiName, "Bridge") ||
                    ContainsIgnoreCase(aiName, "Tunnel");
            }

            private static bool ContainsIgnoreCase(string text, string value)
            {
                return text != null && text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static ushort GetSegmentId(NetNode node, int index)
            {
                if (index == 0) return node.m_segment0;
                if (index == 1) return node.m_segment1;
                if (index == 2) return node.m_segment2;
                if (index == 3) return node.m_segment3;
                if (index == 4) return node.m_segment4;
                if (index == 5) return node.m_segment5;
                if (index == 6) return node.m_segment6;
                return node.m_segment7;
            }

            private static bool IsCreatedRoadSegment(NetSegment segment)
            {
                return (segment.m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None && IsRoadInfo(segment.Info);
            }

            private static bool IsRoadInfo(NetInfo info)
            {
                return info != null && info.m_class != null && info.m_class.m_service == ItemClass.Service.Road;
            }
        }

        private sealed class ProblemCollector
        {
            private readonly int limit;
            private readonly StringBuilder items = new StringBuilder();
            private readonly StringBuilder counts = new StringBuilder();
            private readonly StringBuilder nameCounts = new StringBuilder();
            private int emitted;
            private int total;
            private bool firstItem = true;
            private bool firstCount = true;
            private bool firstNameCount = true;
            private readonly System.Collections.Generic.Dictionary<string, int> countByProblem = new System.Collections.Generic.Dictionary<string, int>();
            private readonly System.Collections.Generic.Dictionary<string, int> countByProblemName = new System.Collections.Generic.Dictionary<string, int>();

            public ProblemCollector(int limit)
            {
                this.limit = limit;
            }

            public void CollectBuildings()
            {
                BuildingManager manager = BuildingManager.instance;
                for (ushort i = 1; i < manager.m_buildings.m_buffer.Length; i++)
                {
                    Building building = manager.m_buildings.m_buffer[i];
                    if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
                    {
                        continue;
                    }
                    Add("building", i, building.m_position, building.m_problems, building.m_flags, true);
                }
            }

            public void CollectNetNodes()
            {
                NetManager manager = NetManager.instance;
                for (ushort i = 1; i < manager.m_nodes.m_buffer.Length; i++)
                {
                    NetNode node = manager.m_nodes.m_buffer[i];
                    if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.None)
                    {
                        continue;
                    }
                    Add("netNode", i, node.m_position, node.m_problems, Building.Flags.None, false);
                }
            }

            public void CollectNetSegments()
            {
                NetManager manager = NetManager.instance;
                for (ushort i = 1; i < manager.m_segments.m_buffer.Length; i++)
                {
                    NetSegment segment = manager.m_segments.m_buffer[i];
                    if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                    {
                        continue;
                    }
                    Add("netSegment", i, segment.m_middlePosition, segment.m_problems, Building.Flags.None, false);
                }
            }

            public string ToJson()
            {
                foreach (System.Collections.Generic.KeyValuePair<string, int> pair in countByProblem)
                {
                    if (!firstCount)
                    {
                        counts.Append(",");
                    }
                    counts.Append("\"").Append(JsonUtil.Escape(pair.Key)).Append("\":").Append(pair.Value);
                    firstCount = false;
                }

                foreach (System.Collections.Generic.KeyValuePair<string, int> pair in countByProblemName)
                {
                    if (!firstNameCount)
                    {
                        nameCounts.Append(",");
                    }
                    nameCounts.Append("\"").Append(JsonUtil.Escape(pair.Key)).Append("\":").Append(pair.Value);
                    firstNameCount = false;
                }

                return "{\"ok\":true,\"total\":" + total +
                    ",\"returned\":" + emitted +
                    ",\"limit\":" + limit +
                    ",\"counts\":{" + counts.ToString() + "}" +
                    ",\"countsByProblem\":{" + nameCounts.ToString() + "}" +
                    ",\"problems\":[" + items.ToString() + "]}";
            }

            private void Add(string entityType, ushort id, Vector3 position, Notification.ProblemStruct problems, Building.Flags buildingFlags, bool includeBuildingFlags)
            {
                bool hasBuildingFlagAlert = includeBuildingFlags && HasBuildingFlagAlerts(buildingFlags);
                if (problems.IsNone && !hasBuildingFlagAlert)
                {
                    return;
                }

                string problemText = BuildProblemText(problems, buildingFlags, hasBuildingFlagAlert);
                string problemNamesJson = BuildProblemNamesJson(problems, buildingFlags, hasBuildingFlagAlert);
                total++;

                if (countByProblem.ContainsKey(problemText))
                {
                    countByProblem[problemText]++;
                }
                else
                {
                    countByProblem[problemText] = 1;
                }

                CountProblemNames(problems);
                CountBuildingFlagNames(buildingFlags, hasBuildingFlagAlert);

                if (emitted >= limit)
                {
                    return;
                }

                if (!firstItem)
                {
                    items.Append(",");
                }

                items.Append("{\"entityType\":\"").Append(entityType).Append("\"");
                items.Append(",\"id\":").Append(id);
                items.Append(",\"problems\":\"").Append(JsonUtil.Escape(problemText)).Append("\"");
                items.Append(",\"problemNames\":").Append(problemNamesJson);
                items.Append(",\"problem1Raw\":").Append(((ulong)problems.m_Problems1).ToString());
                items.Append(",\"problem2Raw\":").Append(((ulong)problems.m_Problems2).ToString());
                if (includeBuildingFlags)
                {
                    items.Append(",\"buildingFlags\":\"").Append(JsonUtil.Escape(BuildingFlagsText(buildingFlags))).Append("\"");
                    items.Append(",\"buildingFlagsRaw\":").Append(((ulong)buildingFlags).ToString());
                }
                items.Append(",\"isMajor\":").Append(JsonUtil.Bool(problems.IsMajor));
                items.Append(",\"isFatal\":").Append(JsonUtil.Bool(problems.IsFatal));
                items.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(position.x));
                items.Append(",\"y\":").Append(JsonUtil.Number(position.y));
                items.Append(",\"z\":").Append(JsonUtil.Number(position.z)).Append("}}");

                emitted++;
                firstItem = false;
            }

            private string BuildProblemText(Notification.ProblemStruct problems, Building.Flags buildingFlags, bool hasBuildingFlagAlert)
            {
                string text = problems.IsNone ? "" : problems.ToString();
                if (!hasBuildingFlagAlert)
                {
                    return text;
                }

                string flagsText = BuildingFlagAlertsText(buildingFlags);
                if (text.Length == 0)
                {
                    return flagsText;
                }
                return text + ", " + flagsText;
            }

            private void CountProblemNames(Notification.ProblemStruct problems)
            {
                AddProblemNameCounts(typeof(Notification.Problem1), (ulong)problems.m_Problems1);
                AddProblemNameCounts(typeof(Notification.Problem2), (ulong)problems.m_Problems2);
            }

            private void AddProblemNameCounts(System.Type enumType, ulong flags)
            {
                System.Array values = System.Enum.GetValues(enumType);
                for (int i = 0; i < values.Length; i++)
                {
                    object value = values.GetValue(i);
                    string name = value.ToString();
                    if (name == "None" || name == "All")
                    {
                        continue;
                    }

                    ulong bit = System.Convert.ToUInt64(value);
                    if ((flags & bit) == 0)
                    {
                        continue;
                    }

                    if (countByProblemName.ContainsKey(name))
                    {
                        countByProblemName[name]++;
                    }
                    else
                    {
                        countByProblemName[name] = 1;
                    }
                }
            }

            private string BuildProblemNamesJson(Notification.ProblemStruct problems, Building.Flags buildingFlags, bool hasBuildingFlagAlert)
            {
                StringBuilder names = new StringBuilder();
                bool first = true;
                AppendProblemNames(names, typeof(Notification.Problem1), (ulong)problems.m_Problems1, ref first);
                AppendProblemNames(names, typeof(Notification.Problem2), (ulong)problems.m_Problems2, ref first);
                if (hasBuildingFlagAlert)
                {
                    AppendBuildingFlagNames(names, buildingFlags, ref first);
                }
                return "[" + names.ToString() + "]";
            }

            private void AppendProblemNames(StringBuilder names, System.Type enumType, ulong flags, ref bool first)
            {
                System.Array values = System.Enum.GetValues(enumType);
                for (int i = 0; i < values.Length; i++)
                {
                    object value = values.GetValue(i);
                    string name = value.ToString();
                    if (name == "None" || name == "All")
                    {
                        continue;
                    }

                    ulong bit = System.Convert.ToUInt64(value);
                    if ((flags & bit) == 0)
                    {
                        continue;
                    }

                    if (!first)
                    {
                        names.Append(",");
                    }
                    names.Append("\"").Append(JsonUtil.Escape(name)).Append("\"");
                    first = false;
                }
            }

            private bool HasBuildingFlagAlerts(Building.Flags flags)
            {
                return (flags & Building.Flags.Abandoned) != Building.Flags.None ||
                    (flags & Building.Flags.BurnedDown) != Building.Flags.None ||
                    (flags & Building.Flags.Collapsed) != Building.Flags.None ||
                    (flags & Building.Flags.Flooded) != Building.Flags.None ||
                    (flags & Building.Flags.RoadAccessFailed) != Building.Flags.None;
            }

            private void CountBuildingFlagNames(Building.Flags flags, bool hasBuildingFlagAlert)
            {
                if (!hasBuildingFlagAlert)
                {
                    return;
                }

                CountBuildingFlagName(flags, Building.Flags.Abandoned, "Abandoned");
                CountBuildingFlagName(flags, Building.Flags.BurnedDown, "BurnedDown");
                CountBuildingFlagName(flags, Building.Flags.Collapsed, "Collapsed");
                CountBuildingFlagName(flags, Building.Flags.Flooded, "Flooded");
                CountBuildingFlagName(flags, Building.Flags.RoadAccessFailed, "RoadAccessFailed");
            }

            private void CountBuildingFlagName(Building.Flags flags, Building.Flags flag, string name)
            {
                if ((flags & flag) == Building.Flags.None)
                {
                    return;
                }

                if (countByProblemName.ContainsKey(name))
                {
                    countByProblemName[name]++;
                }
                else
                {
                    countByProblemName[name] = 1;
                }
            }

            private void AppendBuildingFlagNames(StringBuilder names, Building.Flags flags, ref bool first)
            {
                AppendBuildingFlagName(names, flags, Building.Flags.Abandoned, "Abandoned", ref first);
                AppendBuildingFlagName(names, flags, Building.Flags.BurnedDown, "BurnedDown", ref first);
                AppendBuildingFlagName(names, flags, Building.Flags.Collapsed, "Collapsed", ref first);
                AppendBuildingFlagName(names, flags, Building.Flags.Flooded, "Flooded", ref first);
                AppendBuildingFlagName(names, flags, Building.Flags.RoadAccessFailed, "RoadAccessFailed", ref first);
            }

            private void AppendBuildingFlagName(StringBuilder names, Building.Flags flags, Building.Flags flag, string name, ref bool first)
            {
                if ((flags & flag) == Building.Flags.None)
                {
                    return;
                }

                if (!first)
                {
                    names.Append(",");
                }
                names.Append("\"").Append(JsonUtil.Escape(name)).Append("\"");
                first = false;
            }

            private string BuildingFlagAlertsText(Building.Flags flags)
            {
                StringBuilder names = new StringBuilder();
                bool first = true;
                AppendBuildingFlagText(names, flags, Building.Flags.Abandoned, "Abandoned", ref first);
                AppendBuildingFlagText(names, flags, Building.Flags.BurnedDown, "BurnedDown", ref first);
                AppendBuildingFlagText(names, flags, Building.Flags.Collapsed, "Collapsed", ref first);
                AppendBuildingFlagText(names, flags, Building.Flags.Flooded, "Flooded", ref first);
                AppendBuildingFlagText(names, flags, Building.Flags.RoadAccessFailed, "RoadAccessFailed", ref first);
                return names.ToString();
            }

            private string BuildingFlagsText(Building.Flags flags)
            {
                return flags.ToString();
            }

            private void AppendBuildingFlagText(StringBuilder names, Building.Flags flags, Building.Flags flag, string name, ref bool first)
            {
                if ((flags & flag) == Building.Flags.None)
                {
                    return;
                }

                if (!first)
                {
                    names.Append(", ");
                }
                names.Append(name);
                first = false;
            }
        }
    }
}
