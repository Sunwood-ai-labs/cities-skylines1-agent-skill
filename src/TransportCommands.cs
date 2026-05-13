using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class TransportCommands
    {
        public static CommandResult BuildTransportLinesJson(int limit)
        {
            if (limit < 0) limit = 0;
            if (limit > 512) limit = 512;

            TransportManager manager = TransportManager.instance;
            StringBuilder items = new StringBuilder();
            int total = 0;
            int emitted = 0;
            bool first = true;

            for (ushort lineId = 1; lineId < manager.m_lines.m_buffer.Length; lineId++)
            {
                TransportLine line = manager.m_lines.m_buffer[lineId];
                if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
                {
                    continue;
                }

                total++;
                if (emitted >= limit)
                {
                    continue;
                }

                if (!first)
                {
                    items.Append(",");
                }

                TransportInfo info = line.Info;
                VehicleInfo vehicleInfo = null;
                try { vehicleInfo = line.GetLineVehicle(lineId); } catch { }
                string name = "";
                try { name = manager.GetLineName(lineId); } catch { }
                string defaultVehicleName = "";
                int defaultVehicleIndex = -1;
                try { defaultVehicleName = manager.GetDefaultLineVehicleName(lineId); } catch { }
                try { defaultVehicleIndex = manager.GetDefaultLineVehicleIndex(lineId); } catch { }

                items.Append("{\"id\":").Append(lineId);
                items.Append(",\"name\":\"").Append(JsonUtil.Escape(name)).Append("\"");
                items.Append(",\"transportType\":\"").Append(JsonUtil.Escape(info == null ? "" : info.m_transportType.ToString())).Append("\"");
                items.Append(",\"vehicleType\":\"").Append(JsonUtil.Escape(info == null ? "" : info.m_vehicleType.ToString())).Append("\"");
                items.Append(",\"vehicleReason\":\"").Append(JsonUtil.Escape(info == null ? "" : info.m_vehicleReason.ToString())).Append("\"");
                items.Append(",\"citizenReason\":\"").Append(JsonUtil.Escape(info == null ? "" : info.m_citizenReason.ToString())).Append("\"");
                items.Append(",\"defaultVehicleDistance\":").Append(JsonUtil.Number(info == null ? 0f : info.m_defaultVehicleDistance));
                items.Append(",\"defaultVehicleIndex\":").Append(defaultVehicleIndex);
                items.Append(",\"defaultVehicleName\":\"").Append(JsonUtil.Escape(defaultVehicleName)).Append("\"");
                items.Append(",\"lineVehiclePrefab\":\"").Append(JsonUtil.Escape(vehicleInfo == null ? "" : vehicleInfo.name)).Append("\"");
                items.Append(",\"flags\":\"").Append(JsonUtil.Escape(line.m_flags.ToString())).Append("\"");
                items.Append(",\"budget\":").Append(line.m_budget);
                items.Append(",\"ticketPrice\":").Append(line.m_ticketPrice);
                items.Append(",\"averageInterval\":").Append(line.m_averageInterval);
                items.Append(",\"buildingId\":").Append(line.m_building);
                items.Append(",\"firstVehicleId\":").Append(line.m_vehicles);
                items.Append(",\"targetVehicles\":").Append(line.CalculateTargetVehicleCount());
                items.Append(",\"stops\":").Append(line.CountStops(lineId));
                items.Append(",\"vehicles\":").Append(CountVehicles(line.m_vehicles));
                items.Append(",\"passengers\":").Append(line.CalculatePassengerCount(lineId));
                items.Append(",\"totalLength\":").Append(JsonUtil.Number(line.m_totalLength));
                items.Append(",\"color\":\"#").Append(line.m_color.r.ToString("X2")).Append(line.m_color.g.ToString("X2")).Append(line.m_color.b.ToString("X2")).Append("\"");
                items.Append(",\"stopNodes\":[");
                AppendStops(items, lineId, line);
                items.Append("]}");

                first = false;
                emitted++;
            }

            return CommandResult.FromJson("{\"ok\":true,\"total\":" + total +
                ",\"returned\":" + emitted +
                ",\"limit\":" + limit +
                ",\"lines\":[" + items.ToString() + "]}");
        }

        public static CommandResult CreateTransportLine(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            string typeName = JsonUtil.GetString(body, "transportType", "Bus");
            string lineName = JsonUtil.GetString(body, "name", "");
            bool fixedPlatform = JsonUtil.GetBool(body, "fixedPlatform", false);
            List<string> stopObjects = JsonUtil.GetObjectArray(body, "stops");

            TransportInfo.TransportType transportType;
            if (!TryParseTransportType(typeName, out transportType))
            {
                return CommandResult.Fail("Unsupported transportType: " + typeName);
            }

            if (stopObjects.Count < 2)
            {
                return CommandResult.Fail("At least two stops are required.");
            }

            TransportManager manager = TransportManager.instance;
            TransportInfo info = FindTransportInfo(transportType);
            if (info == null)
            {
                return CommandResult.Fail("Transport info is not loaded for: " + transportType.ToString());
            }

            List<Vector3> stops = new List<Vector3>();
            for (int i = 0; i < stopObjects.Count; i++)
            {
                Vector3 position = ReadPosition(stopObjects[i]);
                position.y = TerrainManager.instance.SampleRawHeightSmoothWithWater(position, false, 0f);
                stops.Add(position);
            }

            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"message\":\"Create-transport-line validation passed.\",\"transportType\":\"" +
                    JsonUtil.Escape(transportType.ToString()) + "\",\"stops\":" + stops.Count + "}");
            }

            SimulationManager simulation = Singleton<SimulationManager>.instance;
            Randomizer randomizer = simulation.m_randomizer;
            ushort lineId;
            bool created = manager.CreateLine(out lineId, ref randomizer, info, false);
            simulation.m_randomizer = randomizer;

            if (!created || lineId == 0)
            {
                return CommandResult.Fail("Failed to create transport line.");
            }

            manager.m_lines.m_buffer[lineId].m_budget = 100;
            manager.m_lines.m_buffer[lineId].m_ticketPrice = (ushort)Mathf.Max(0, info.m_ticketPrice);
            manager.m_lines.m_buffer[lineId].m_building = FindDepotBuilding(info);
            TransportLine.Flags beforeFlags = manager.m_lines.m_buffer[lineId].m_flags;
            for (int i = 0; i < stops.Count; i++)
            {
                bool added = manager.m_lines.m_buffer[lineId].AddStop(lineId, i, stops[i], fixedPlatform);
                if (!added)
                {
                    manager.ReleaseLine(lineId);
                    return CommandResult.Fail("Failed to add stop " + i + " near x=" + JsonUtil.Number(stops[i].x) + ", z=" + JsonUtil.Number(stops[i].z) + ".");
                }
            }

            manager.m_lines.m_buffer[lineId].m_flags |= TransportLine.Flags.Complete | TransportLine.Flags.CompleteSet;
            ApplyColor(body, lineId);
            ApplyLineName(manager, lineId, lineName);
            manager.m_lines.m_buffer[lineId].UpdatePaths(lineId);
            manager.m_lines.m_buffer[lineId].UpdateMeshData(lineId);
            manager.UpdateLine(lineId);
            manager.UpdateLinesNow();
            manager.CheckTransportLineVehicles();

            TransportLine line = manager.m_lines.m_buffer[lineId];
            string json = "{\"ok\":true,\"dryRun\":false,\"lineId\":" + lineId +
                ",\"name\":\"" + JsonUtil.Escape(lineName) + "\"" +
                ",\"transportType\":\"" + JsonUtil.Escape(transportType.ToString()) + "\"" +
                ",\"buildingId\":" + line.m_building +
                ",\"stops\":" + line.CountStops(lineId) +
                ",\"beforeFlags\":\"" + JsonUtil.Escape(beforeFlags.ToString()) + "\"" +
                ",\"afterFlags\":\"" + JsonUtil.Escape(line.m_flags.ToString()) + "\"}";

            Debug.Log("[SkylinesAgentBridge] Created transport line " + lineId + " type " + transportType.ToString() + " with " + stops.Count + " stops");
            return CommandResult.FromJson(json);
        }

        public static CommandResult AssignTransportLineDepot(string body)
        {
            ushort lineId = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            ushort buildingId = (ushort)JsonUtil.GetNumber(body, "buildingId", 0f);

            if (lineId == 0)
            {
                return CommandResult.Fail("id is required.");
            }

            TransportManager manager = TransportManager.instance;
            TransportLine line = manager.m_lines.m_buffer[lineId];
            if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
            {
                return CommandResult.Fail("Transport line was not found: " + lineId);
            }

            TransportInfo info = line.Info;
            if (info == null)
            {
                return CommandResult.Fail("Transport info was not found for line: " + lineId);
            }

            if (buildingId == 0)
            {
                buildingId = FindDepotBuilding(info);
            }

            if (buildingId == 0)
            {
                return CommandResult.Fail("No active matching depot building was found for line: " + lineId);
            }

            BuildingManager buildings = BuildingManager.instance;
            Building building = buildings.m_buildings.m_buffer[buildingId];
            if (!IsUsableDepot(building, info))
            {
                return CommandResult.Fail("Building is not a usable depot for this line: " + buildingId);
            }

            manager.m_lines.m_buffer[lineId].m_building = buildingId;
            manager.UpdateLine(lineId);
            manager.UpdateLinesNow();
            manager.CheckTransportLineVehicles();

            return CommandResult.FromJson("{\"ok\":true,\"lineId\":" + lineId +
                ",\"buildingId\":" + buildingId + "}");
        }

        public static CommandResult ReleaseTransportLine(string body)
        {
            ushort lineId = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);

            if (lineId == 0)
            {
                return CommandResult.Fail("id is required.");
            }

            TransportManager manager = TransportManager.instance;
            TransportLine line = manager.m_lines.m_buffer[lineId];
            if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
            {
                return CommandResult.Fail("Transport line was not found: " + lineId);
            }

            if (!dryRun)
            {
                ReleaseLine(manager, lineId);
            }

            return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) +
                ",\"lineId\":" + lineId + "}");
        }

        private static void AppendStops(StringBuilder json, ushort lineId, TransportLine line)
        {
            NetManager net = NetManager.instance;
            ushort stop = line.m_stops;
            bool first = true;
            int guard = 0;

            while (stop != 0 && guard < 256)
            {
                if (!first)
                {
                    json.Append(",");
                }

                Vector3 position = net.m_nodes.m_buffer[stop].m_position;
                json.Append("{\"nodeId\":").Append(stop);
                json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(position.x));
                json.Append(",\"y\":").Append(JsonUtil.Number(position.y));
                json.Append(",\"z\":").Append(JsonUtil.Number(position.z)).Append("}}");

                stop = TransportLine.GetNextStop(stop);
                first = false;
                guard++;

                if (stop == line.m_stops)
                {
                    break;
                }
            }
        }

        private static int CountVehicles(ushort firstVehicle)
        {
            VehicleManager vehicles = VehicleManager.instance;
            int count = 0;
            ushort vehicle = firstVehicle;
            int guard = 0;

            while (vehicle != 0 && guard < 16384)
            {
                count++;
                vehicle = vehicles.m_vehicles.m_buffer[vehicle].m_nextLineVehicle;
                guard++;
            }

            return count;
        }

        private static bool TryParseTransportType(string value, out TransportInfo.TransportType transportType)
        {
            string normalized = value == null ? "" : value.Trim();
            string[] names = Enum.GetNames(typeof(TransportInfo.TransportType));
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Compare(names[i], normalized, true) == 0)
                {
                    transportType = (TransportInfo.TransportType)Enum.Parse(typeof(TransportInfo.TransportType), names[i]);
                    return true;
                }
            }

            transportType = TransportInfo.TransportType.Bus;
            return false;
        }

        private static TransportInfo FindTransportInfo(TransportInfo.TransportType transportType)
        {
            TransportInfo fallback = TransportManager.instance.GetTransportInfo(transportType);

            int count = PrefabCollection<TransportInfo>.LoadedCount();
            for (int i = 0; i < count; i++)
            {
                TransportInfo info = PrefabCollection<TransportInfo>.GetLoaded((uint)i);
                if (info == null)
                {
                    continue;
                }
                if (info.m_transportType != transportType)
                {
                    continue;
                }

                if (transportType == TransportInfo.TransportType.Bus &&
                    info.m_vehicleReason == TransferManager.TransferReason.Bus)
                {
                    return info;
                }

                if (fallback == null)
                {
                    fallback = info;
                }
            }

            return fallback;
        }

        private static ushort FindDepotBuilding(TransportInfo transportInfo)
        {
            if (transportInfo == null)
            {
                return 0;
            }

            BuildingManager buildings = BuildingManager.instance;
            for (ushort i = 1; i < buildings.m_buildings.m_buffer.Length; i++)
            {
                Building building = buildings.m_buildings.m_buffer[i];
                if (IsUsableDepot(building, transportInfo))
                {
                    return i;
                }
            }

            return 0;
        }

        private static bool IsUsableDepot(Building building, TransportInfo transportInfo)
        {
            if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
            {
                return false;
            }
            if ((building.m_flags & Building.Flags.Deleted) != Building.Flags.None)
            {
                return false;
            }
            if ((building.m_flags & Building.Flags.Active) == Building.Flags.None)
            {
                return false;
            }
            if ((building.m_flags & Building.Flags.RoadAccessFailed) != Building.Flags.None)
            {
                return false;
            }
            if (!building.m_problems.IsNone)
            {
                return false;
            }

            BuildingInfo buildingInfo = building.Info;
            if (buildingInfo == null || buildingInfo.m_class == null)
            {
                return false;
            }
            if (buildingInfo.m_class.m_service != ItemClass.Service.PublicTransport)
            {
                return false;
            }

            ItemClass.SubService stationSubService = RequiredDepotSubService(transportInfo);
            if (stationSubService != ItemClass.SubService.None &&
                buildingInfo.m_class.m_subService != stationSubService)
            {
                return false;
            }

            return true;
        }

        private static ItemClass.SubService RequiredDepotSubService(TransportInfo transportInfo)
        {
            if (transportInfo == null)
            {
                return ItemClass.SubService.None;
            }

            if (transportInfo.m_transportType == TransportInfo.TransportType.Bus)
            {
                return ItemClass.SubService.PublicTransportBus;
            }

            return transportInfo.m_stationSubService;
        }

        private static Vector3 ReadPosition(string json)
        {
            float x = JsonUtil.GetPointNumber(json, "position", "x", JsonUtil.GetNumber(json, "x", 0f));
            float y = JsonUtil.GetPointNumber(json, "position", "y", JsonUtil.GetNumber(json, "y", 0f));
            float z = JsonUtil.GetPointNumber(json, "position", "z", JsonUtil.GetNumber(json, "z", 0f));
            return new Vector3(x, y, z);
        }

        private static void ApplyLineName(TransportManager manager, ushort lineId, string name)
        {
            if (name == null || name.Length == 0)
            {
                return;
            }

            try
            {
                System.Collections.Generic.IEnumerator<bool> enumerator = manager.SetLineName(lineId, name);
                while (enumerator.MoveNext())
                {
                }
            }
            catch
            {
            }
        }

        private static void ApplyColor(string body, ushort lineId)
        {
            string color = JsonUtil.GetString(body, "color", "");
            if (color == null || color.Length == 0)
            {
                return;
            }

            if (color.StartsWith("#"))
            {
                color = color.Substring(1);
            }

            if (color.Length != 6)
            {
                return;
            }

            try
            {
                byte r = Convert.ToByte(color.Substring(0, 2), 16);
                byte g = Convert.ToByte(color.Substring(2, 2), 16);
                byte b = Convert.ToByte(color.Substring(4, 2), 16);
                TransportManager.instance.m_lines.m_buffer[lineId].m_color = new Color32(r, g, b, 255);
            }
            catch
            {
            }
        }

        private static void ReleaseLine(TransportManager manager, ushort lineId)
        {
            try
            {
                manager.ReleaseLine(lineId);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == null || ex.Message.IndexOf("Already in the same thread") < 0)
                {
                    throw;
                }

                MethodInfo method = typeof(TransportManager).GetMethod(
                    "ReleaseLineImplementation",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(ushort), typeof(TransportLine).MakeByRefType() },
                    null);

                if (method == null)
                {
                    throw;
                }

                TransportLine line = manager.m_lines.m_buffer[lineId];
                object[] args = new object[] { lineId, line };
                method.Invoke(manager, args);
                manager.m_lines.m_buffer[lineId] = (TransportLine)args[1];
            }
        }
    }
}
