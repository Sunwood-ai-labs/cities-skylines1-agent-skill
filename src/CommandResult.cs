namespace SkylinesAgentBridge
{
    public sealed class CommandResult
    {
        public bool Ok;
        public string Json;
        public string Error;

        public static CommandResult FromJson(string json)
        {
            CommandResult result = new CommandResult();
            result.Ok = true;
            result.Json = json;
            return result;
        }

        public static CommandResult Fail(string error)
        {
            CommandResult result = new CommandResult();
            result.Ok = false;
            result.Error = error;
            result.Json = "{\"ok\":false,\"error\":\"" + JsonUtil.Escape(error) + "\"}";
            return result;
        }
    }
}
