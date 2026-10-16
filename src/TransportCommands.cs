using System.Collections.Generic;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // Public transport lines: list, create (stops snapped onto the nearest road
    // segment, loop closed back to the first stop like the in-game tool), and
    // remove.
    public static class TransportCommands
    {
        public static CommandResult BuildLinesJson()
        {
            TransportManager transport = Singleton<TransportManager>.instance;
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true,\"lines\":[");
            bool first = true;
            for (ushort i = 1; i < transport.m_lines.m_buffer.Length; i++)
            {
                TransportLine line = transport.m_lines.m_buffer[i];
                if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
                {
                    continue;
                }
                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                sb.Append("{\"id\":").Append(i);
                sb.Append(",\"number\":").Append(line.m_lineNumber);
                sb.Append(",\"name\":\"").Append(JsonUtil.Escape(transport.GetLineName(i))).Append("\"");
                sb.Append(",\"type\":\"").Append(JsonUtil.Escape(line.Info != null ? line.Info.name : "")).Append("\"");
                sb.Append(",\"complete\":").Append(JsonUtil.Bool(line.Complete));
                sb.Append(",\"stops\":").Append(line.CountStops(i));
                sb.Append(",\"vehicles\":").Append(line.CountVehicles(i));
                sb.Append(",\"targetVehicles\":").Append(line.CalculateTargetVehicleCount());
                sb.Append(",\"budget\":").Append(line.m_budget);
                sb.Append(",\"ticketPrice\":").Append(line.m_ticketPrice);
                sb.Append(",\"passengers\":").Append(line.m_passengers.m_residentPassengers.m_averageCount + line.m_passengers.m_touristPassengers.m_averageCount);
                sb.Append(",\"stopPositions\":[");
                ushort stop = line.m_stops;
                for (int n = 0; stop != 0 && n < 64; n++)
                {
                    Vector3 sp = Singleton<NetManager>.instance.m_nodes.m_buffer[stop].m_position;
                    if (n > 0) sb.Append(",");
                    sb.Append("[").Append(sp.x.ToString("0")).Append(",").Append(sp.z.ToString("0")).Append("]");
                    stop = TransportLine.GetNextStop(stop);
                    if (stop == line.m_stops) break;
                }
                sb.Append("]");
                sb.Append("}");
            }
            sb.Append("]}");
            return CommandResult.FromJson(sb.ToString());
        }

        public static CommandResult CreateLine(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            string infoName = JsonUtil.GetString(body, "transportInfo", "Bus");
            List<string> stopObjects = JsonUtil.GetObjectArray(body, "stops");

            TransportInfo info = PrefabCollection<TransportInfo>.FindLoaded(infoName);
            if (info == null)
            {
                return CommandResult.Fail("Transport prefab was not found: " + infoName);
            }
            if (stopObjects.Count < 2)
            {
                return CommandResult.Fail("At least two stops are required.");
            }
            if (!CostHelpers.IsUnlocked(info))
            {
                return CostHelpers.Locked(infoName + " lines");
            }

            List<Vector3> stops = new List<Vector3>();
            StringBuilder snapped = new StringBuilder();
            for (int i = 0; i < stopObjects.Count; i++)
            {
                Vector3 requested = new Vector3(JsonUtil.GetNumber(stopObjects[i], "x", 0f), 0f, JsonUtil.GetNumber(stopObjects[i], "z", 0f));
                Vector3 position;
                ushort segment;
                if (!SnapToRoad(requested, 40f, out position, out segment))
                {
                    return CommandResult.Fail("Stop " + i + " is not within 40 m of a road.");
                }
                if (CostHelpers.OutOfArea(position))
                {
                    return CostHelpers.OutsideOwnedArea("Stop " + i);
                }
                stops.Add(position);
                if (i > 0)
                {
                    snapped.Append(",");
                }
                snapped.Append("{\"x\":").Append(JsonUtil.Number(position.x)).Append(",\"z\":").Append(JsonUtil.Number(position.z)).Append(",\"segment\":").Append(segment).Append("}");
            }

            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"stops\":[" + snapped + "]}");
            }

            SimulationManager simulation = Singleton<SimulationManager>.instance;
            TransportManager transport = Singleton<TransportManager>.instance;
            Randomizer randomizer = simulation.m_randomizer;
            ushort lineId;
            if (!transport.CreateLine(out lineId, ref randomizer, info, true))
            {
                return CommandResult.Fail("Failed to create transport line.");
            }
            simulation.m_randomizer = randomizer;

            int added = 0;
            for (int i = 0; i < stops.Count; i++)
            {
                if (transport.m_lines.m_buffer[lineId].AddStop(lineId, -1, stops[i], false))
                {
                    added++;
                }
            }

            // Close the loop the way the tool does: add the first stop's position again.
            bool closed = false;
            ushort firstStop = transport.m_lines.m_buffer[lineId].m_stops;
            if (firstStop != 0)
            {
                Vector3 firstPosition = Singleton<NetManager>.instance.m_nodes.m_buffer[firstStop].m_position;
                closed = transport.m_lines.m_buffer[lineId].AddStop(lineId, -1, firstPosition, false);
            }
            transport.UpdateLine(lineId);

            bool complete = transport.m_lines.m_buffer[lineId].Complete;
            Debug.Log("[SkylinesAgentBridge] Created transport line " + lineId + " with " + added + " stops, complete=" + complete);
            return CommandResult.FromJson("{\"ok\":true,\"dryRun\":false,\"lineId\":" + lineId +
                ",\"stopsAdded\":" + added + ",\"closed\":" + JsonUtil.Bool(closed) + ",\"complete\":" + JsonUtil.Bool(complete) +
                ",\"stops\":[" + snapped + "]}");
        }

        // Per-line budget percentage (the line panel's vehicle slider). The game
        // derives the line's target vehicle count from its length and this value.
        public static CommandResult SetLineBudget(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            int budget = (int)JsonUtil.GetNumber(body, "budget", -1f);
            TransportManager transport = Singleton<TransportManager>.instance;
            if (id == 0 || (transport.m_lines.m_buffer[id].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
            {
                return CommandResult.Fail("Transport line was not found: " + id);
            }
            if (budget < 1 || budget > 150)
            {
                return CommandResult.Fail("budget must be between 1 and 150.");
            }
            int before = transport.m_lines.m_buffer[id].CalculateTargetVehicleCount();
            transport.m_lines.m_buffer[id].m_budget = (ushort)budget;
            int after = transport.m_lines.m_buffer[id].CalculateTargetVehicleCount();
            return CommandResult.FromJson("{\"ok\":true,\"id\":" + id + ",\"budget\":" + budget +
                ",\"targetVehiclesBefore\":" + before + ",\"targetVehiclesAfter\":" + after + "}");
        }

        // Vehicle models a line can use, with passenger capacity, plus the model
        // currently selected for it.
        public static CommandResult BuildLineVehiclesJson(ushort id)
        {
            TransportManager transport = Singleton<TransportManager>.instance;
            if (id == 0 || (transport.m_lines.m_buffer[id].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
            {
                return CommandResult.Fail("Transport line was not found: " + id);
            }

            int selected;
            bool hasSelected = transport.TryGetSelectedLineVehicle(id, out selected);
            FastList<VehicleSelector.Item> list = transport.m_lines.m_buffer[id].GetCompatibleVehiclesList(id, new FastList<VehicleSelector.Item>());

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true,\"id\":").Append(id);
            sb.Append(",\"defaultVehicle\":\"").Append(JsonUtil.Escape(transport.GetDefaultLineVehicleName(id))).Append("\"");
            VehicleInfo current = hasSelected ? PrefabCollection<VehicleInfo>.GetPrefab((uint)selected) : null;
            sb.Append(",\"selectedVehicle\":\"").Append(JsonUtil.Escape(current != null ? current.name : "")).Append("\"");
            sb.Append(",\"vehicles\":[");
            bool first = true;
            for (int i = 0; list != null && i < list.m_size; i++)
            {
                VehicleInfo info = PrefabCollection<VehicleInfo>.GetPrefab(list.m_buffer[i].m_prefabIndex);
                if (info == null)
                {
                    continue;
                }
                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                sb.Append("{\"name\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
                sb.Append(",\"capacity\":").Append(PassengerCapacity(info));
                sb.Append(",\"selectable\":").Append(JsonUtil.Bool(list.m_buffer[i].m_selectable));
                sb.Append("}");
            }
            sb.Append("]}");
            return CommandResult.FromJson(sb.ToString());
        }

        public static CommandResult SetLineVehicle(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            string name = JsonUtil.GetString(body, "vehicle", "");
            TransportManager transport = Singleton<TransportManager>.instance;
            if (id == 0 || (transport.m_lines.m_buffer[id].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
            {
                return CommandResult.Fail("Transport line was not found: " + id);
            }
            VehicleInfo info = PrefabCollection<VehicleInfo>.FindLoaded(name);
            if (info == null)
            {
                return CommandResult.Fail("Vehicle prefab was not found: " + name);
            }
            if (!transport.m_lines.m_buffer[id].IsCompatibleVehicle(id, info))
            {
                return CommandResult.Fail("Vehicle is not compatible with this line: " + name);
            }
            transport.AssignSelectedLineVehicle(id, info.m_prefabDataIndex);
            return CommandResult.FromJson("{\"ok\":true,\"id\":" + id + ",\"vehicle\":\"" + JsonUtil.Escape(info.name) + "\",\"capacity\":" + PassengerCapacity(info) + "}");
        }

        private static int PassengerCapacity(VehicleInfo info)
        {
            if (info.m_vehicleAI == null)
            {
                return 0;
            }
            System.Reflection.FieldInfo field = info.m_vehicleAI.GetType().GetField("m_passengerCapacity");
            if (field == null)
            {
                return 0;
            }
            return (int)field.GetValue(info.m_vehicleAI);
        }

        public static CommandResult RemoveLine(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            TransportManager transport = Singleton<TransportManager>.instance;
            if (id == 0 || (transport.m_lines.m_buffer[id].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
            {
                return CommandResult.Fail("Transport line was not found: " + id);
            }
            transport.ReleaseLine(id);
            return CommandResult.FromJson("{\"ok\":true,\"removed\":" + id + "}");
        }

        // Closest point on any road segment (treated as a straight line between
        // its nodes) within maxDistance of the requested point.
        private static bool SnapToRoad(Vector3 requested, float maxDistance, out Vector3 position, out ushort segmentId)
        {
            NetManager net = Singleton<NetManager>.instance;
            float best = maxDistance * maxDistance;
            position = requested;
            segmentId = 0;
            foreach (ushort i in GridHelpers.SegmentsNear(requested, maxDistance))
            {
                NetSegment segment = net.m_segments.m_buffer[i];
                if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None || segment.Info == null)
                {
                    continue;
                }
                if (segment.Info.m_class.m_service != ItemClass.Service.Road || !(segment.Info.m_netAI is RoadAI))
                {
                    continue;
                }
                Vector3 a = net.m_nodes.m_buffer[segment.m_startNode].m_position;
                Vector3 b = net.m_nodes.m_buffer[segment.m_endNode].m_position;
                Vector3 ab = b - a;
                ab.y = 0f;
                Vector3 ap = requested - a;
                ap.y = 0f;
                float len = ab.sqrMagnitude;
                float t = len > 0f ? Mathf.Clamp(Vector3.Dot(ap, ab) / len, 0.1f, 0.9f) : 0f;
                Vector3 p = a + (b - a) * t;
                Vector3 d = p - requested;
                d.y = 0f;
                if (d.sqrMagnitude < best)
                {
                    best = d.sqrMagnitude;
                    position = p;
                    segmentId = i;
                }
            }
            return segmentId != 0;
        }
    }
}
