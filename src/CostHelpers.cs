using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // Construction charging, mirroring the in-game tools: check the treasury
    // with PeekResource before building, then FetchResource once it succeeded.
    // Amounts are in the game's internal units (cents).
    public static class CostHelpers
    {
        public static int BuildingCost(BuildingInfo info)
        {
            PlayerBuildingAI ai = info == null ? null : info.m_buildingAI as PlayerBuildingAI;
            return ai == null ? 0 : ai.GetConstructionCost();
        }

        public static int RelocationCost(BuildingInfo info)
        {
            return info == null || info.m_buildingAI == null ? 0 : info.m_buildingAI.GetRelocationCost();
        }

        public static int NetCost(NetInfo info, Vector3 start, Vector3 end)
        {
            PlayerNetAI ai = info == null ? null : info.m_netAI as PlayerNetAI;
            if (ai == null)
            {
                return 0;
            }
            TerrainManager terrain = TerrainManager.instance;
            float startHeight = Mathf.Max(0f, start.y - terrain.SampleRawHeightSmooth(start));
            float endHeight = Mathf.Max(0f, end.y - terrain.SampleRawHeightSmooth(end));
            return ai.GetConstructionCost(start, end, startHeight, endHeight);
        }

        public static bool CanAfford(int cost)
        {
            return cost <= 0 || Singleton<EconomyManager>.instance.PeekResource(EconomyManager.Resource.Construction, cost) == cost;
        }

        public static void Charge(int cost, ItemClass itemClass)
        {
            if (cost > 0)
            {
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Construction, cost, itemClass);
            }
        }

        // Milestone/service unlock check, so the bridge only builds what the
        // player has actually unlocked in this game.
        public static bool IsUnlocked(PrefabInfo info)
        {
            if (info == null)
            {
                return false;
            }
            UnlockManager unlocks = Singleton<UnlockManager>.instance;
            MilestoneInfo milestone = info.GetUnlockMilestone();
            if (milestone != null && !unlocks.Unlocked(milestone))
            {
                return false;
            }
            ItemClass.Service service = info.GetService();
            if (service != ItemClass.Service.None && !unlocks.Unlocked(service))
            {
                return false;
            }
            return true;
        }

        public static bool FeatureUnlocked(UnlockManager.Feature feature)
        {
            return Singleton<UnlockManager>.instance.Unlocked(feature);
        }

        public static bool ZoneUnlocked(ItemClass.Zone zone)
        {
            return Singleton<UnlockManager>.instance.Unlocked(zone);
        }

        // Outside the tiles the player owns (the game's own placement rule).
        public static bool OutOfArea(Vector3 position)
        {
            return Singleton<GameAreaManager>.instance.PointOutOfArea(position);
        }

        public static CommandResult OutsideOwnedArea(string what)
        {
            return CommandResult.Fail(what + " is outside the map tiles you own.");
        }

        public static CommandResult FeatureLocked(string feature)
        {
            return CommandResult.Fail(feature + " is not unlocked yet in this game.");
        }

        public static CommandResult Locked(string name)
        {
            return CommandResult.Fail("Not unlocked yet in this game: " + name + ".");
        }

        public static bool CanAffordLandscaping(int cost)
        {
            return cost <= 0 || Singleton<EconomyManager>.instance.PeekResource(EconomyManager.Resource.Landscaping, cost) == cost;
        }

        public static void ChargeLandscaping(int cost)
        {
            if (cost > 0)
            {
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Landscaping, cost, ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Level.None);
            }
        }

        public static string Money(int cost)
        {
            return (cost / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static CommandResult NotEnoughMoney(int cost)
        {
            return CommandResult.Fail("Not enough money: this costs " + Money(cost) + ".");
        }
    }
}
