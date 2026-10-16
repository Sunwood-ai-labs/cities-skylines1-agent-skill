using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    // Map tiles: which are owned, which can be bought next, and their prices,
    // plus a purchase command that charges the real land price from the
    // treasury the same way the in-game areas panel does.
    public static class AreaCommands
    {
        public static CommandResult BuildAreasJson()
        {
            GameAreaManager areas = Singleton<GameAreaManager>.instance;
            EconomyManager economy = Singleton<EconomyManager>.instance;
            UnlockManager unlocks = Singleton<UnlockManager>.instance;
            int res = GameAreaManager.AREAGRID_RESOLUTION;

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true");
            sb.Append(",\"owned\":").Append(areas.m_areaCount);
            sb.Append(",\"maxAllowed\":").Append(areas.m_maxAreaCount);
            sb.Append(",\"gridResolution\":").Append(res);
            sb.Append(",\"cash\":").Append((economy.LastCashAmount / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(",\"zonesUnlocked\":{");
            sb.Append("\"ResidentialHigh\":").Append(JsonUtil.Bool(unlocks.Unlocked(ItemClass.Zone.ResidentialHigh)));
            sb.Append(",\"CommercialHigh\":").Append(JsonUtil.Bool(unlocks.Unlocked(ItemClass.Zone.CommercialHigh)));
            sb.Append(",\"Office\":").Append(JsonUtil.Bool(unlocks.Unlocked(ItemClass.Zone.Office)));
            sb.Append(",\"Industrial\":").Append(JsonUtil.Bool(unlocks.Unlocked(ItemClass.Zone.Industrial)));
            sb.Append("}");
            sb.Append(",\"tiles\":[");
            bool first = true;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    bool unlocked = areas.IsUnlocked(x, z);
                    bool canUnlock = !unlocked && areas.CanUnlock(x, z);
                    if (!unlocked && !canUnlock)
                    {
                        continue;
                    }
                    float minX, minZ, maxX, maxZ;
                    areas.GetAreaBounds(x, z, out minX, out minZ, out maxX, out maxZ);
                    if (!first)
                    {
                        sb.Append(",");
                    }
                    first = false;
                    sb.Append("{\"x\":").Append(x).Append(",\"z\":").Append(z);
                    sb.Append(",\"owned\":").Append(JsonUtil.Bool(unlocked));
                    sb.Append(",\"canBuy\":").Append(JsonUtil.Bool(canUnlock));
                    if (canUnlock)
                    {
                        sb.Append(",\"price\":").Append((areas.CalculateTilePrice(z * res + x) / 100.0).ToString("0", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    sb.Append(",\"bounds\":{\"minX\":").Append(JsonUtil.Number(minX)).Append(",\"minZ\":").Append(JsonUtil.Number(minZ));
                    sb.Append(",\"maxX\":").Append(JsonUtil.Number(maxX)).Append(",\"maxZ\":").Append(JsonUtil.Number(maxZ)).Append("}");
                    sb.Append("}");
                }
            }
            sb.Append("]}");
            return CommandResult.FromJson(sb.ToString());
        }

        public static CommandResult BuyArea(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            int x = (int)JsonUtil.GetNumber(body, "x", -1f);
            int z = (int)JsonUtil.GetNumber(body, "z", -1f);
            GameAreaManager areas = Singleton<GameAreaManager>.instance;
            EconomyManager economy = Singleton<EconomyManager>.instance;
            int res = GameAreaManager.AREAGRID_RESOLUTION;

            if (x < 0 || z < 0 || x >= res || z >= res)
            {
                return CommandResult.Fail("Tile is outside the map grid.");
            }
            if (areas.IsUnlocked(x, z))
            {
                return CommandResult.Fail("Tile is already owned.");
            }
            if (!areas.CanUnlock(x, z))
            {
                return CommandResult.Fail("Tile cannot be bought yet (not adjacent, or no milestone allowance left).");
            }
            if (areas.m_areaCount >= areas.m_maxAreaCount)
            {
                return CommandResult.Fail("No tile purchases allowed until the next milestone.");
            }

            int index = z * res + x;
            int price = areas.CalculateTilePrice(index);
            if (economy.PeekResource(EconomyManager.Resource.LandPrice, price) != price)
            {
                return CommandResult.Fail("Not enough money for this tile.");
            }
            string priceText = (price / 100.0).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"x\":" + x + ",\"z\":" + z + ",\"price\":" + priceText + "}");
            }

            economy.FetchResource(EconomyManager.Resource.LandPrice, price, ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Level.None);
            bool bought = areas.UnlockArea(index);
            return CommandResult.FromJson("{\"ok\":" + JsonUtil.Bool(bought) + ",\"x\":" + x + ",\"z\":" + z + ",\"price\":" + priceText +
                ",\"ownedNow\":" + JsonUtil.Bool(areas.IsUnlocked(x, z)) + ",\"owned\":" + areas.m_areaCount + ",\"maxAllowed\":" + areas.m_maxAreaCount + "}");
        }
    }
}
