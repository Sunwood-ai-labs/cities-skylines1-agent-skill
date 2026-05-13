namespace SkylinesAgentBridge
{
    public static class ZoneHelpers
    {
        public static int GetRowCount(ZoneBlock block)
        {
            int rows = block.RowCount;
            if (rows <= 0)
            {
                rows = 4;
            }
            if (rows > 8)
            {
                rows = 8;
            }
            return rows;
        }
    }
}
