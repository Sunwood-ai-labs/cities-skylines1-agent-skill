using System;
using System.Collections.Generic;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // Read-only budget snapshot: cash, per-service income/expenses and budget
    // sliders, plus the most expensive player buildings and network upkeep by
    // prefab. Money values are converted from CS1's internal cents to the
    // currency shown in the budget panel.
    public static class BudgetCommands
    {
        public static CommandResult BuildBudgetJson(int topBuildings)
        {
            EconomyManager economy = Singleton<EconomyManager>.instance;
            StringBuilder sb = new StringBuilder();

            sb.Append("{\"ok\":true");
            sb.Append(",\"cash\":").Append(Money(economy.LastCashAmount));
            sb.Append(",\"lastCashDelta\":").Append(Money(economy.LastCashDelta));
            sb.Append(",\"internalCash\":").Append(Money(economy.InternalCashAmount));
            sb.Append(",\"loanCount\":").Append(economy.CountLoans());
            sb.Append(",\"loanExpenses\":").Append(Money(economy.GetLoanExpenses()));
            sb.Append(",\"policyExpenses\":").Append(Money(economy.GetPolicyExpenses()));

            long totalIncome = 0;
            long totalExpenses = 0;
            sb.Append(",\"services\":[");
            bool first = true;
            foreach (ItemClass.Service service in Enum.GetValues(typeof(ItemClass.Service)))
            {
                if (service == ItemClass.Service.None)
                {
                    continue;
                }

                long income;
                long expenses;
                economy.GetIncomeAndExpenses(service, ItemClass.SubService.None, ItemClass.Level.None, out income, out expenses);
                if (income == 0 && expenses == 0)
                {
                    continue;
                }

                totalIncome += income;
                totalExpenses += expenses;

                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                sb.Append("{\"service\":\"").Append(JsonUtil.Escape(service.ToString())).Append("\"");
                sb.Append(",\"income\":").Append(Money(income));
                sb.Append(",\"expenses\":").Append(Money(expenses));
                sb.Append(",\"net\":").Append(Money(income - expenses));
                sb.Append(",\"budgetDay\":").Append(economy.GetBudget(service, ItemClass.SubService.None, false));
                sb.Append(",\"budgetNight\":").Append(economy.GetBudget(service, ItemClass.SubService.None, true));
                sb.Append("}");
            }
            sb.Append("]");
            sb.Append(",\"totalIncome\":").Append(Money(totalIncome));
            sb.Append(",\"totalExpenses\":").Append(Money(totalExpenses));
            sb.Append(",\"totalNet\":").Append(Money(totalIncome - totalExpenses));

            AppendBuildings(sb, topBuildings);
            AppendNetworks(sb);

            sb.Append("}");
            return CommandResult.FromJson(sb.ToString());
        }

        private static void AppendBuildings(StringBuilder sb, int topBuildings)
        {
            BuildingManager buildings = Singleton<BuildingManager>.instance;
            List<KeyValuePair<ushort, int>> costs = new List<KeyValuePair<ushort, int>>();

            for (int i = 1; i < buildings.m_buildings.m_buffer.Length; i++)
            {
                Building building = buildings.m_buildings.m_buffer[i];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None || building.Info == null)
                {
                    continue;
                }
                PlayerBuildingAI ai = building.Info.m_buildingAI as PlayerBuildingAI;
                if (ai == null)
                {
                    continue;
                }
                int cost = ai.GetMaintenanceCost();
                if (cost > 0)
                {
                    costs.Add(new KeyValuePair<ushort, int>((ushort)i, cost));
                }
            }

            costs.Sort(delegate(KeyValuePair<ushort, int> a, KeyValuePair<ushort, int> b) { return b.Value.CompareTo(a.Value); });

            sb.Append(",\"buildingMaintenance\":[");
            for (int i = 0; i < costs.Count && i < topBuildings; i++)
            {
                Building building = buildings.m_buildings.m_buffer[costs[i].Key];
                if (i > 0)
                {
                    sb.Append(",");
                }
                sb.Append("{\"id\":").Append(costs[i].Key);
                sb.Append(",\"prefab\":\"").Append(JsonUtil.Escape(building.Info.name)).Append("\"");
                sb.Append(",\"service\":\"").Append(JsonUtil.Escape(building.Info.m_class.m_service.ToString())).Append("\"");
                sb.Append(",\"maintenance\":").Append(Money(costs[i].Value));
                sb.Append("}");
            }
            sb.Append("]");
            sb.Append(",\"playerBuildingsWithUpkeep\":").Append(costs.Count);
        }

        private static void AppendNetworks(StringBuilder sb)
        {
            NetManager net = Singleton<NetManager>.instance;
            Dictionary<string, long> costByPrefab = new Dictionary<string, long>();
            Dictionary<string, int> countByPrefab = new Dictionary<string, int>();
            Dictionary<string, float> lengthByPrefab = new Dictionary<string, float>();

            for (int i = 1; i < net.m_segments.m_buffer.Length; i++)
            {
                NetSegment segment = net.m_segments.m_buffer[i];
                if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None || segment.Info == null)
                {
                    continue;
                }
                PlayerNetAI ai = segment.Info.m_netAI as PlayerNetAI;
                if (ai == null)
                {
                    continue;
                }
                Vector3 start = net.m_nodes.m_buffer[segment.m_startNode].m_position;
                Vector3 end = net.m_nodes.m_buffer[segment.m_endNode].m_position;
                int cost = ai.GetMaintenanceCost(start, end);
                string name = segment.Info.name;

                long total;
                costByPrefab.TryGetValue(name, out total);
                costByPrefab[name] = total + cost;
                int count;
                countByPrefab.TryGetValue(name, out count);
                countByPrefab[name] = count + 1;
                float length;
                lengthByPrefab.TryGetValue(name, out length);
                lengthByPrefab[name] = length + Vector3.Distance(start, end);
            }

            sb.Append(",\"networkMaintenance\":[");
            bool first = true;
            foreach (KeyValuePair<string, long> entry in costByPrefab)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                sb.Append("{\"prefab\":\"").Append(JsonUtil.Escape(entry.Key)).Append("\"");
                sb.Append(",\"segments\":").Append(countByPrefab[entry.Key]);
                sb.Append(",\"lengthMeters\":").Append(JsonUtil.Number(lengthByPrefab[entry.Key]));
                sb.Append(",\"maintenance\":").Append(Money(entry.Value));
                sb.Append("}");
            }
            sb.Append("]");
        }

        private static string Money(long cents)
        {
            return (cents / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
