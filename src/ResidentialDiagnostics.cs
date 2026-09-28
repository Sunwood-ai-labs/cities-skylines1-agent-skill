using System;
using System.Text;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Read-only data for diagnosing residential growables and residential zone cells.
    /// </summary>
    public static class ResidentialDiagnostics
    {
        public static CommandResult BuildJson()
        {
            BuildingManager buildings = BuildingManager.instance;
            CitizenManager citizens = CitizenManager.instance;
            ZoneManager zones = ZoneManager.instance;
            NetManager networks = NetManager.instance;

            if (buildings == null || citizens == null || zones == null || networks == null)
            {
                return CommandResult.Fail("Residential diagnostic managers are not ready.");
            }

            StringBuilder buildingItems = new StringBuilder();
            StringBuilder zoneBlockItems = new StringBuilder();
            int residentialBuildings = 0;
            int homeUnits = 0;
            int occupiedHomeUnits = 0;
            int residentSlotsUsed = 0;
            int residentSlotsCapacity = 0;
            int emittedBuildings = 0;
            bool firstBuilding = true;

            for (ushort id = 1; id < buildings.m_buildings.m_buffer.Length; id++)
            {
                Building building = buildings.m_buildings.m_buffer[id];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    continue;
                }

                BuildingInfo info = building.Info;
                if (info == null || info.m_class == null ||
                    info.m_class.m_service != ItemClass.Service.Residential)
                {
                    continue;
                }

                int buildingHomeUnits;
                int buildingOccupiedHomeUnits;
                int buildingResidentSlots;
                int buildingHomeSlotCapacity;
                int unitsVisited;
                CountHomeUnits(citizens, building, out buildingHomeUnits,
                    out buildingOccupiedHomeUnits, out buildingResidentSlots,
                    out buildingHomeSlotCapacity, out unitsVisited);

                residentialBuildings++;
                homeUnits += buildingHomeUnits;
                occupiedHomeUnits += buildingOccupiedHomeUnits;
                residentSlotsUsed += buildingResidentSlots;
                residentSlotsCapacity += buildingHomeSlotCapacity;

                if (!firstBuilding)
                {
                    buildingItems.Append(",");
                }

                Vector3 position = building.m_position;
                string problems = building.m_problems.IsNone ? "" : building.m_problems.ToString();
                buildingItems.Append("{\"id\":").Append(id);
                buildingItems.Append(",\"prefab\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
                buildingItems.Append(",\"subService\":\"").Append(JsonUtil.Escape(info.m_class.m_subService.ToString())).Append("\"");
                buildingItems.Append(",\"level\":\"").Append(JsonUtil.Escape(info.m_class.m_level.ToString())).Append("\"");
                buildingItems.Append(",\"width\":").Append(info.GetWidth());
                buildingItems.Append(",\"length\":").Append(info.GetLength());
                buildingItems.Append(",\"flags\":\"").Append(JsonUtil.Escape(building.m_flags.ToString())).Append("\"");
                buildingItems.Append(",\"problems\":\"").Append(JsonUtil.Escape(problems)).Append("\"");
                buildingItems.Append(",\"reportedCitizenCount\":").Append(building.m_citizenCount);
                buildingItems.Append(",\"homeUnits\":").Append(buildingHomeUnits);
                buildingItems.Append(",\"occupiedHomeUnits\":").Append(buildingOccupiedHomeUnits);
                buildingItems.Append(",\"residentSlotsUsed\":").Append(buildingResidentSlots);
                buildingItems.Append(",\"residentSlotsCapacity\":").Append(buildingHomeSlotCapacity);
                buildingItems.Append(",\"citizenUnitsVisited\":").Append(unitsVisited);
                buildingItems.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(position.x));
                buildingItems.Append(",\"y\":").Append(JsonUtil.Number(position.y));
                buildingItems.Append(",\"z\":").Append(JsonUtil.Number(position.z)).Append("}}");
                firstBuilding = false;
                emittedBuildings++;
            }

            int residentialZoneBlocks = 0;
            int residentialZoneCells = 0;
            int validResidentialCells = 0;
            int unoccupiedValidResidentialCells = 0;
            int emittedZoneBlocks = 0;
            bool firstZoneBlock = true;

            for (ushort id = 1; id < zones.m_blocks.m_buffer.Length; id++)
            {
                ZoneBlock block = zones.m_blocks.m_buffer[id];
                if ((block.m_flags & ZoneBlock.FLAG_CREATED) == 0)
                {
                    continue;
                }

                int rows = ZoneHelpers.GetRowCount(block);
                int residentialCellsInBlock = 0;
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < ZoneBlock.COLUMN_COUNT; column++)
                    {
                        if (IsResidentialZone(block.GetZone(column, row)))
                        {
                            residentialCellsInBlock++;
                        }
                    }
                }

                if (residentialCellsInBlock == 0)
                {
                    continue;
                }

                residentialZoneBlocks++;
                residentialZoneCells += residentialCellsInBlock;

                if (!firstZoneBlock)
                {
                    zoneBlockItems.Append(",");
                }

                Vector3 blockPosition = block.m_position;
                zoneBlockItems.Append("{\"id\":").Append(id);
                zoneBlockItems.Append(",\"rows\":").Append(rows);
                zoneBlockItems.Append(",\"segmentId\":").Append(block.m_segment);
                zoneBlockItems.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(blockPosition.x));
                zoneBlockItems.Append(",\"y\":").Append(JsonUtil.Number(blockPosition.y));
                zoneBlockItems.Append(",\"z\":").Append(JsonUtil.Number(blockPosition.z)).Append("}");
                zoneBlockItems.Append(",\"angleDegrees\":").Append(JsonUtil.Number(block.m_angle * Mathf.Rad2Deg));
                AppendRoadConnection(zoneBlockItems, networks, block.m_segment);
                zoneBlockItems.Append(",\"cells\":[");

                bool firstCell = true;
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < ZoneBlock.COLUMN_COUNT; column++)
                    {
                        ItemClass.Zone zone = block.GetZone(column, row);
                        if (!IsResidentialZone(zone))
                        {
                            continue;
                        }

                        int bitIndex = row * (int)ZoneBlock.COLUMN_COUNT + column;
                        ulong cellMask = 1UL << bitIndex;
                        bool valid = (block.m_valid & cellMask) != 0;
                        bool occupied1 = block.IsOccupied1(column, row);
                        bool occupied2 = block.IsOccupied2(column, row);
                        bool shared = (block.m_shared & cellMask) != 0;
                        if (valid)
                        {
                            validResidentialCells++;
                            if (!occupied1 && !occupied2)
                            {
                                unoccupiedValidResidentialCells++;
                            }
                        }

                        if (!firstCell)
                        {
                            zoneBlockItems.Append(",");
                        }
                        zoneBlockItems.Append("{\"column\":").Append(column);
                        zoneBlockItems.Append(",\"row\":").Append(row);
                        zoneBlockItems.Append(",\"zone\":\"").Append(zone.ToString()).Append("\"");
                        zoneBlockItems.Append(",\"valid\":").Append(JsonUtil.Bool(valid));
                        zoneBlockItems.Append(",\"occupied1\":").Append(JsonUtil.Bool(occupied1));
                        zoneBlockItems.Append(",\"occupied2\":").Append(JsonUtil.Bool(occupied2));
                        zoneBlockItems.Append(",\"shared\":").Append(JsonUtil.Bool(shared)).Append("}");
                        firstCell = false;
                    }
                }

                zoneBlockItems.Append("]}");
                firstZoneBlock = false;
                emittedZoneBlocks++;
            }

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"summary\":{\"residentialBuildings\":").Append(residentialBuildings);
            json.Append(",\"homeUnits\":").Append(homeUnits);
            json.Append(",\"occupiedHomeUnits\":").Append(occupiedHomeUnits);
            json.Append(",\"residentSlotsUsed\":").Append(residentSlotsUsed);
            json.Append(",\"residentSlotsCapacity\":").Append(residentSlotsCapacity);
            json.Append(",\"unoccupiedResidentSlots\":").Append(Math.Max(0, residentSlotsCapacity - residentSlotsUsed));
            json.Append(",\"residentialZoneBlocks\":").Append(residentialZoneBlocks);
            json.Append(",\"residentialZoneCells\":").Append(residentialZoneCells);
            json.Append(",\"validResidentialCells\":").Append(validResidentialCells);
            json.Append(",\"unoccupiedValidResidentialCells\":").Append(unoccupiedValidResidentialCells).Append("}");
            json.Append(",\"returnedBuildings\":").Append(emittedBuildings);
            json.Append(",\"buildings\":[").Append(buildingItems.ToString()).Append("]");
            json.Append(",\"returnedZoneBlocks\":").Append(emittedZoneBlocks);
            json.Append(",\"zoneBlocks\":[").Append(zoneBlockItems.ToString()).Append("]}");
            return CommandResult.FromJson(json.ToString());
        }

        private static bool IsResidentialZone(ItemClass.Zone zone)
        {
            string name = zone.ToString();
            return name.StartsWith("Residential", StringComparison.Ordinal);
        }

        private static void CountHomeUnits(CitizenManager manager, Building building,
            out int homeUnits, out int occupiedHomeUnits, out int residentSlotsUsed,
            out int residentSlotsCapacity, out int unitsVisited)
        {
            homeUnits = 0;
            occupiedHomeUnits = 0;
            residentSlotsUsed = 0;
            residentSlotsCapacity = 0;
            unitsVisited = 0;

            uint unitId = building.m_citizenUnits;
            uint firstUnitId = unitId;
            int maxUnits = manager.m_units.m_buffer.Length;
            while (unitId != 0 && unitId < maxUnits && unitsVisited < 4096)
            {
                CitizenUnit unit = manager.m_units.m_buffer[unitId];
                if ((unit.m_flags & CitizenUnit.Flags.Created) == CitizenUnit.Flags.None)
                {
                    break;
                }
                unitsVisited++;

                if ((unit.m_flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None)
                {
                    homeUnits++;
                    int unitResidents = 0;
                    for (int slot = 0; slot < 5; slot++)
                    {
                        if (unit.GetCitizen(slot) != 0)
                        {
                            unitResidents++;
                        }
                    }

                    residentSlotsCapacity += 5;
                    residentSlotsUsed += unitResidents;
                    if (unitResidents > 0)
                    {
                        occupiedHomeUnits++;
                    }
                }

                uint nextUnit = unit.m_nextUnit;
                if (nextUnit == unitId || nextUnit == firstUnitId)
                {
                    break;
                }
                unitId = nextUnit;
            }
        }

        private static void AppendRoadConnection(StringBuilder json, NetManager manager, ushort segmentId)
        {
            json.Append(",\"road\":");
            if (segmentId == 0 || segmentId >= manager.m_segments.m_buffer.Length)
            {
                json.Append("null");
                return;
            }

            NetSegment segment = manager.m_segments.m_buffer[segmentId];
            NetInfo info = segment.Info;
            if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None || info == null)
            {
                json.Append("null");
                return;
            }

            json.Append("{\"id\":").Append(segmentId);
            json.Append(",\"prefab\":\"").Append(JsonUtil.Escape(info.name)).Append("\"");
            json.Append(",\"startNodeId\":").Append(segment.m_startNode);
            json.Append(",\"endNodeId\":").Append(segment.m_endNode).Append("}");
        }
    }
}
