using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class BuildingCommands
    {
        public static CommandResult PlaceBuilding(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            string prefabName = JsonUtil.GetString(body, "buildingPrefab", "");
            float angleDegrees = JsonUtil.GetNumber(body, "angleDegrees", 0f);
            Vector3 position = ReadPoint(body, "position");

            if (prefabName == null || prefabName.Length == 0)
            {
                return CommandResult.Fail("buildingPrefab is required.");
            }

            BuildingInfo info = PrefabCollection<BuildingInfo>.FindLoaded(prefabName);
            if (info == null)
            {
                return CommandResult.Fail("Building prefab was not found: " + prefabName);
            }
            if (AssetPolicy.IsBlockedBuildingPrefab(info))
            {
                return CommandResult.Fail("Building prefab is blocked and must not be used: " + prefabName + " (" + AssetPolicy.BlockReason(prefabName) + ")");
            }

            TerrainManager terrain = TerrainManager.instance;
            position.y = terrain.SampleRawHeightSmoothWithWater(position, false, 0f);

            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"message\":\"Place-building validation passed.\",\"buildingPrefab\":\"" + JsonUtil.Escape(prefabName) + "\"}");
            }

            SimulationManager simulation = Singleton<SimulationManager>.instance;
            BuildingManager buildings = BuildingManager.instance;
            Randomizer randomizer = simulation.m_randomizer;
            ushort buildingId;
            float angle = angleDegrees * Mathf.Deg2Rad;

            bool created = buildings.CreateBuilding(
                out buildingId,
                ref randomizer,
                info,
                position,
                angle,
                info.GetLength(),
                simulation.m_currentBuildIndex);

            simulation.m_randomizer = randomizer;

            if (!created)
            {
                return CommandResult.Fail("Failed to create building.");
            }

            simulation.m_currentBuildIndex += 1u;

            string json = "{\"ok\":true,\"dryRun\":false,\"buildingId\":" + buildingId +
                ",\"buildingPrefab\":\"" + JsonUtil.Escape(prefabName) + "\"" +
                ",\"service\":\"" + JsonUtil.Escape(info.m_class.m_service.ToString()) + "\"" +
                ",\"subService\":\"" + JsonUtil.Escape(info.m_class.m_subService.ToString()) + "\"}";

            Debug.Log("[SkylinesAgentBridge] Placed building " + buildingId + " with prefab " + prefabName);
            return CommandResult.FromJson(json);
        }

        public static CommandResult MoveBuilding(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);

            if (id == 0)
            {
                return CommandResult.Fail("id is required.");
            }

            BuildingManager buildings = BuildingManager.instance;
            if ((buildings.m_buildings.m_buffer[id].m_flags & Building.Flags.Created) == Building.Flags.None)
            {
                return CommandResult.Fail("Building was not found: " + id);
            }

            Building oldBuilding = buildings.m_buildings.m_buffer[id];
            BuildingInfo info = oldBuilding.Info;
            if (info == null)
            {
                return CommandResult.Fail("Building prefab info was not found for building: " + id);
            }
            if (AssetPolicy.IsBlockedBuildingPrefab(info))
            {
                return CommandResult.Fail("Building prefab is blocked and must not be used for move/recreate: " + info.name + " (" + AssetPolicy.BlockReason(info.name) + ")");
            }

            Vector3 position = ReadPoint(body, "position");
            TerrainManager terrain = TerrainManager.instance;
            position.y = terrain.SampleRawHeightSmoothWithWater(position, false, 0f);
            float angleDegrees = JsonUtil.GetNumber(body, "angleDegrees", oldBuilding.m_angle * Mathf.Rad2Deg);
            float angle = angleDegrees * Mathf.Deg2Rad;

            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"message\":\"Move-building validation passed.\",\"id\":" + id +
                    ",\"buildingPrefab\":\"" + JsonUtil.Escape(info.name) + "\"}");
            }

            SimulationManager simulation = Singleton<SimulationManager>.instance;
            Randomizer randomizer = simulation.m_randomizer;
            ushort newBuildingId;
            bool created = buildings.CreateBuilding(
                out newBuildingId,
                ref randomizer,
                info,
                position,
                angle,
                info.GetLength(),
                simulation.m_currentBuildIndex);

            simulation.m_randomizer = randomizer;
            if (!created)
            {
                return CommandResult.Fail("Failed to create moved building.");
            }

            simulation.m_currentBuildIndex += 1u;
            buildings.ReleaseBuilding(id);

            string json = "{\"ok\":true,\"dryRun\":false,\"oldBuildingId\":" + id +
                ",\"newBuildingId\":" + newBuildingId +
                ",\"buildingPrefab\":\"" + JsonUtil.Escape(info.name) + "\"" +
                ",\"position\":{\"x\":" + JsonUtil.Number(position.x) +
                ",\"y\":" + JsonUtil.Number(position.y) +
                ",\"z\":" + JsonUtil.Number(position.z) + "}}";

            Debug.Log("[SkylinesAgentBridge] Moved building " + id + " to " + newBuildingId + " with prefab " + info.name);
            return CommandResult.FromJson(json);
        }

        public static CommandResult SetBuildingActive(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            bool active = JsonUtil.GetBool(body, "active", true);

            if (id == 0)
            {
                return CommandResult.Fail("id is required.");
            }

            BuildingManager buildings = BuildingManager.instance;
            Building building = buildings.m_buildings.m_buffer[id];
            if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
            {
                return CommandResult.Fail("Building was not found: " + id);
            }

            Building.Flags before = building.m_flags;
            BuildingInfo info = building.Info;
            if (info != null && info.m_buildingAI != null)
            {
                InvokeManualActivation(info.m_buildingAI, id, ref building, active);
            }

            if (active)
            {
                building.m_flags |= Building.Flags.Active;
            }
            else
            {
                building.m_flags &= ~Building.Flags.Active;
            }
            buildings.m_buildings.m_buffer[id] = building;

            string prefab = info == null ? "" : info.name;
            string json = "{\"ok\":true,\"id\":" + id +
                ",\"active\":" + JsonUtil.Bool(active) +
                ",\"prefab\":\"" + JsonUtil.Escape(prefab) + "\"" +
                ",\"beforeFlags\":\"" + JsonUtil.Escape(before.ToString()) + "\"" +
                ",\"afterFlags\":\"" + JsonUtil.Escape(building.m_flags.ToString()) + "\"}";
            Debug.Log("[SkylinesAgentBridge] Set building " + id + " active=" + active);
            return CommandResult.FromJson(json);
        }

        private static void InvokeManualActivation(BuildingAI ai, ushort id, ref Building building, bool active)
        {
            string methodName = active ? "ManualActivation" : "ManualDeactivation";
            MethodInfo method = ai.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                method = typeof(BuildingAI).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (method == null)
            {
                return;
            }

            object[] args = new object[] { id, building };
            method.Invoke(ai, args);
            building = (Building)args[1];
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
