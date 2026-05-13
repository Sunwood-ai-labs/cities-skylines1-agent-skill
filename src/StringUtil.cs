namespace SkylinesAgentBridge
{
    public static class StringUtil
    {
        public static bool ContainsIgnoreCase(string text, string value)
        {
            return text != null && value != null && text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
