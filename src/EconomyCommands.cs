using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class EconomyCommands
    {
        private static readonly ItemClass.Level[] TaxLevels = new ItemClass.Level[]
        {
            ItemClass.Level.Level1,
            ItemClass.Level.Level2,
            ItemClass.Level.Level3,
            ItemClass.Level.Level4,
            ItemClass.Level.Level5
        };

        private static readonly TaxTarget[] TaxTargets = new TaxTarget[]
        {
            new TaxTarget(ItemClass.Service.Residential, ItemClass.SubService.ResidentialLow),
            new TaxTarget(ItemClass.Service.Residential, ItemClass.SubService.ResidentialHigh),
            new TaxTarget(ItemClass.Service.Residential, ItemClass.SubService.ResidentialLowEco),
            new TaxTarget(ItemClass.Service.Residential, ItemClass.SubService.ResidentialHighEco),
            new TaxTarget(ItemClass.Service.Residential, ItemClass.SubService.ResidentialWallToWall),
            new TaxTarget(ItemClass.Service.Commercial, ItemClass.SubService.CommercialLow),
            new TaxTarget(ItemClass.Service.Commercial, ItemClass.SubService.CommercialHigh),
            new TaxTarget(ItemClass.Service.Commercial, ItemClass.SubService.CommercialLeisure),
            new TaxTarget(ItemClass.Service.Commercial, ItemClass.SubService.CommercialTourist),
            new TaxTarget(ItemClass.Service.Commercial, ItemClass.SubService.CommercialEco),
            new TaxTarget(ItemClass.Service.Commercial, ItemClass.SubService.CommercialWallToWall),
            new TaxTarget(ItemClass.Service.Industrial, ItemClass.SubService.IndustrialGeneric),
            new TaxTarget(ItemClass.Service.Industrial, ItemClass.SubService.IndustrialForestry),
            new TaxTarget(ItemClass.Service.Industrial, ItemClass.SubService.IndustrialFarming),
            new TaxTarget(ItemClass.Service.Industrial, ItemClass.SubService.IndustrialOil),
            new TaxTarget(ItemClass.Service.Industrial, ItemClass.SubService.IndustrialOre),
            new TaxTarget(ItemClass.Service.Office, ItemClass.SubService.OfficeGeneric),
            new TaxTarget(ItemClass.Service.Office, ItemClass.SubService.OfficeHightech),
            new TaxTarget(ItemClass.Service.Office, ItemClass.SubService.OfficeWallToWall),
            new TaxTarget(ItemClass.Service.Office, ItemClass.SubService.OfficeFinancial)
        };

        public static CommandResult BuildEconomyJson()
        {
            EconomyManager manager = Singleton<EconomyManager>.instance;
            StringBuilder rates = new StringBuilder();
            StringBuilder aggregateRates = new StringBuilder();
            bool first = true;
            bool firstAggregate = true;

            for (int i = 0; i < TaxTargets.Length; i++)
            {
                TaxTarget target = TaxTargets[i];
                if (!firstAggregate)
                {
                    aggregateRates.Append(",");
                }
                ItemClass aggregateClass = BuildItemClass(target.Service, target.SubService, ItemClass.Level.None);
                aggregateRates.Append("{\"service\":\"").Append(target.Service.ToString()).Append("\"");
                aggregateRates.Append(",\"subService\":\"").Append(target.SubService.ToString()).Append("\"");
                aggregateRates.Append(",\"rate\":").Append(manager.GetTaxRate(aggregateClass)).Append("}");
                firstAggregate = false;

                for (int j = 0; j < TaxLevels.Length; j++)
                {
                    ItemClass.Level level = TaxLevels[j];
                    if (!first)
                    {
                        rates.Append(",");
                    }

                    rates.Append("{\"service\":\"").Append(target.Service.ToString()).Append("\"");
                    rates.Append(",\"subService\":\"").Append(target.SubService.ToString()).Append("\"");
                    rates.Append(",\"level\":\"").Append(level.ToString()).Append("\"");
                    rates.Append(",\"rate\":").Append(manager.GetTaxRate(target.Service, target.SubService, level)).Append("}");
                    first = false;
                }
            }

            return CommandResult.FromJson("{\"ok\":true,\"aggregateTaxRates\":[" + aggregateRates.ToString() + "],\"taxRates\":[" + rates.ToString() + "]}");
        }

        public static CommandResult SetTaxRate(string body)
        {
            int rate = (int)JsonUtil.GetNumber(body, "rate", -1f);
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            string serviceText = JsonUtil.GetString(body, "service", "");
            string subServiceText = JsonUtil.GetString(body, "subService", "");
            string levelText = JsonUtil.GetString(body, "level", "");

            if (rate < 0 || rate > 29)
            {
                return CommandResult.Fail("rate must be between 0 and 29.");
            }

            ItemClass.Service serviceFilter;
            bool hasServiceFilter = TryParseService(serviceText, out serviceFilter);

            ItemClass.SubService subServiceFilter;
            bool hasSubServiceFilter = TryParseSubService(subServiceText, out subServiceFilter);

            ItemClass.Level levelFilter;
            bool hasLevelFilter = TryParseLevel(levelText, out levelFilter);

            EconomyManager manager = Singleton<EconomyManager>.instance;
            StringBuilder changed = new StringBuilder();
            bool first = true;
            int changedCount = 0;

            for (int i = 0; i < TaxTargets.Length; i++)
            {
                TaxTarget target = TaxTargets[i];
                if (hasServiceFilter && target.Service != serviceFilter)
                {
                    continue;
                }
                if (hasSubServiceFilter && target.SubService != subServiceFilter)
                {
                    continue;
                }

                if (!hasLevelFilter)
                {
                    ItemClass aggregateClass = BuildItemClass(target.Service, target.SubService, ItemClass.Level.None);
                    int aggregateBefore = manager.GetTaxRate(aggregateClass);
                    if (!dryRun)
                    {
                        manager.SetTaxRate(aggregateClass, rate);
                    }
                    int aggregateAfter = dryRun ? aggregateBefore : manager.GetTaxRate(aggregateClass);
                    AppendChangedRate(changed, ref first, target, ItemClass.Level.None, "aggregate", aggregateBefore, aggregateAfter);
                    changedCount++;
                }

                for (int j = 0; j < TaxLevels.Length; j++)
                {
                    ItemClass.Level level = TaxLevels[j];
                    if (hasLevelFilter && level != levelFilter)
                    {
                        continue;
                    }

                    int before = manager.GetTaxRate(target.Service, target.SubService, level);
                    if (!dryRun)
                    {
                        manager.SetTaxRate(target.Service, target.SubService, level, rate);
                    }
                    int after = dryRun ? before : manager.GetTaxRate(target.Service, target.SubService, level);
                    AppendChangedRate(changed, ref first, target, level, "level", before, after);
                    changedCount++;
                }
            }

            if (changedCount == 0)
            {
                return CommandResult.Fail("No matching taxable service, subService, or level was found.");
            }

            return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) +
                ",\"rate\":" + rate +
                ",\"changed\":" + changedCount +
                ",\"taxRates\":[" + changed.ToString() + "]}");
        }

        private static bool TryParseService(string value, out ItemClass.Service service)
        {
            service = ItemClass.Service.None;
            if (value == null || value.Length == 0 || value == "All" || value == "Zoned")
            {
                return false;
            }
            return TryParseEnum<ItemClass.Service>(value, out service);
        }

        private static ItemClass BuildItemClass(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
        {
            ItemClass itemClass = new ItemClass();
            itemClass.m_service = service;
            itemClass.m_subService = subService;
            itemClass.m_level = level;
            return itemClass;
        }

        private static void AppendChangedRate(StringBuilder changed, ref bool first, TaxTarget target, ItemClass.Level level, string scope, int before, int after)
        {
            if (!first)
            {
                changed.Append(",");
            }
            changed.Append("{\"service\":\"").Append(target.Service.ToString()).Append("\"");
            changed.Append(",\"subService\":\"").Append(target.SubService.ToString()).Append("\"");
            changed.Append(",\"level\":\"").Append(level.ToString()).Append("\"");
            changed.Append(",\"scope\":\"").Append(scope).Append("\"");
            changed.Append(",\"before\":").Append(before);
            changed.Append(",\"after\":").Append(after).Append("}");
            first = false;
        }

        private static bool TryParseSubService(string value, out ItemClass.SubService subService)
        {
            subService = ItemClass.SubService.None;
            if (value == null || value.Length == 0 || value == "All")
            {
                return false;
            }
            return TryParseEnum<ItemClass.SubService>(value, out subService);
        }

        private static bool TryParseLevel(string value, out ItemClass.Level level)
        {
            level = ItemClass.Level.None;
            if (value == null || value.Length == 0 || value == "All")
            {
                return false;
            }
            return TryParseEnum<ItemClass.Level>(value, out level);
        }

        private static bool TryParseEnum<T>(string value, out T result)
        {
            try
            {
                result = (T)Enum.Parse(typeof(T), value, true);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        private struct TaxTarget
        {
            public readonly ItemClass.Service Service;
            public readonly ItemClass.SubService SubService;

            public TaxTarget(ItemClass.Service service, ItemClass.SubService subService)
            {
                Service = service;
                SubService = subService;
            }
        }
    }
}
