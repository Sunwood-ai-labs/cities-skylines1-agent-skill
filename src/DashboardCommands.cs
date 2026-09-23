using System.Text;

namespace SkylinesAgentBridge
{
    public static class DashboardCommands
    {
        public static CommandResult BuildJson(bool includeDetails)
        {
            CommandResult summary = GameState.BuildSummaryJson();
            CommandResult economy = EconomyCommands.BuildEconomyJson();
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"summary\":").Append(summary.Json);
            json.Append(",\"economy\":").Append(economy.Json);
            json.Append(",\"detailsIncluded\":").Append(JsonUtil.Bool(includeDetails));

            if (includeDetails)
            {
                json.Append(",\"problems\":").Append(GameState.BuildProblemsJson(200).Json);
                json.Append(",\"zones\":").Append(GameState.BuildZonesJson().Json);
                json.Append(",\"facilities\":").Append(GameState.BuildFacilitiesJson(0, "", false).Json);
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
