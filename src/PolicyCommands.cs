using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    // City-wide policies: list which are loaded (DLC present), unlocked
    // (milestone reached) and enabled, and switch unlocked ones on or off.
    public static class PolicyCommands
    {
        public static CommandResult BuildPoliciesJson()
        {
            DistrictManager districts = Singleton<DistrictManager>.instance;
            UnlockManager unlocks = Singleton<UnlockManager>.instance;
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true,\"policies\":[");
            bool first = true;
            foreach (DistrictPolicies.Policies policy in Enum.GetValues(typeof(DistrictPolicies.Policies)))
            {
                if (policy == DistrictPolicies.Policies.None || !districts.IsPolicyLoaded(policy))
                {
                    continue;
                }
                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                sb.Append("{\"name\":\"").Append(policy.ToString()).Append("\"");
                sb.Append(",\"unlocked\":").Append(JsonUtil.Bool(unlocks.Unlocked(policy)));
                sb.Append(",\"enabled\":").Append(JsonUtil.Bool(districts.IsCityPolicySet(policy)));
                sb.Append("}");
            }
            sb.Append("]}");
            return CommandResult.FromJson(sb.ToString());
        }

        public static CommandResult SetPolicy(string body)
        {
            string name = JsonUtil.GetString(body, "policy", "");
            bool enabled = JsonUtil.GetBool(body, "enabled", true);
            DistrictPolicies.Policies policy;
            try
            {
                policy = (DistrictPolicies.Policies)Enum.Parse(typeof(DistrictPolicies.Policies), name, true);
            }
            catch (ArgumentException)
            {
                return CommandResult.Fail("Unknown policy: " + name);
            }

            DistrictManager districts = Singleton<DistrictManager>.instance;
            if (!districts.IsPolicyLoaded(policy))
            {
                return CommandResult.Fail("Policy is not available in this game: " + name);
            }
            if (enabled && !Singleton<UnlockManager>.instance.Unlocked(policy))
            {
                return CommandResult.Fail("Policy is not unlocked yet: " + name);
            }

            if (enabled)
            {
                districts.SetCityPolicy(policy);
            }
            else
            {
                districts.UnsetCityPolicy(policy);
            }
            return CommandResult.FromJson("{\"ok\":true,\"policy\":\"" + policy + "\",\"enabled\":" + JsonUtil.Bool(districts.IsCityPolicySet(policy)) + "}");
        }
    }
}
