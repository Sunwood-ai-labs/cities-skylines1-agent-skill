using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // Trees: list the loaded tree prefabs and plant one at a point, paying the
    // game's own tree price like the in-game tree brush.
    public static class TreeCommands
    {
        public static CommandResult BuildTreesJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true,\"trees\":[");
            bool first = true;
            for (uint i = 0; i < PrefabCollection<TreeInfo>.LoadedCount(); i++)
            {
                TreeInfo info = PrefabCollection<TreeInfo>.GetLoaded(i);
                if (info == null) continue;
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{\"name\":\"").Append(JsonUtil.Escape(info.name)).Append("\",\"cost\":").Append(CostHelpers.Money(info.GetConstructionCost()))
                  .Append(",\"unlocked\":").Append(JsonUtil.Bool(CostHelpers.IsUnlocked(info))).Append("}");
            }
            sb.Append("]}");
            return CommandResult.FromJson(sb.ToString());
        }

        public static CommandResult PlaceTree(string body)
        {
            string name = JsonUtil.GetString(body, "treePrefab", "");
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            TreeInfo info = PrefabCollection<TreeInfo>.FindLoaded(name);
            if (info == null) return CommandResult.Fail("Tree prefab was not found: " + name);

            Vector3 position = new Vector3(JsonUtil.GetPointNumber(body, "position", "x", 0f), 0f, JsonUtil.GetPointNumber(body, "position", "z", 0f));
            if (CostHelpers.OutOfArea(position)) return CostHelpers.OutsideOwnedArea("This tree");
            position.y = Singleton<TerrainManager>.instance.SampleRawHeightSmooth(position);

            int cost = info.GetConstructionCost();
            if (!CostHelpers.IsUnlocked(info)) return CostHelpers.Locked(name);
            if (!CostHelpers.CanAfford(cost)) return CostHelpers.NotEnoughMoney(cost);
            if (dryRun) return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"cost\":" + CostHelpers.Money(cost) + "}");

            SimulationManager sim = Singleton<SimulationManager>.instance;
            Randomizer randomizer = sim.m_randomizer;
            uint tree;
            bool created = Singleton<TreeManager>.instance.CreateTree(out tree, ref randomizer, info, position, false);
            sim.m_randomizer = randomizer;
            if (!created) return CommandResult.Fail("The game could not plant a tree there.");
            CostHelpers.Charge(cost, info.m_class);
            return CommandResult.FromJson("{\"ok\":true,\"treeId\":" + tree + ",\"cost\":" + CostHelpers.Money(cost) + ",\"y\":" + position.y.ToString("0.0") + "}");
        }
    }
}
