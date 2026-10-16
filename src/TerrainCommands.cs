using System;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // Landscaping on the raw height grid (16 m cells, 1/64 m height steps),
    // with the game's dirt economy: digging fills the dirt buffer, raising
    // empties it, and only dirt beyond the buffer is bought or sold at
    // DIRT_BUYSELL_PRICE per unit, charged as Landscaping.
    public static class TerrainCommands
    {
        private const float HeightScale = 65536f / 1024f;   // raw units per metre (TERRAIN_HEIGHT = 1024)
        private const long DirtBuySellPrice = 5;              // TerrainManager.DIRT_BUYSELL_PRICE (not public)

        // Sloped drainage trench: lowers ground along start->end to a bed that
        // runs linearly from startY to endY, width metres wide. Never raises ground.
        public static CommandResult Channel(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            Vector2 a = new Vector2(JsonUtil.GetPointNumber(body, "start", "x", 0f), JsonUtil.GetPointNumber(body, "start", "z", 0f));
            Vector2 b = new Vector2(JsonUtil.GetPointNumber(body, "end", "x", 0f), JsonUtil.GetPointNumber(body, "end", "z", 0f));
            float startY = JsonUtil.GetNumber(body, "startY", float.NaN);
            float endY = JsonUtil.GetNumber(body, "endY", float.NaN);
            float width = Mathf.Clamp(JsonUtil.GetNumber(body, "width", 24f), 8f, 200f);
            if (float.IsNaN(startY) || float.IsNaN(endY))
            {
                return CommandResult.Fail("startY and endY (bed heights in metres) are required.");
            }
            float length = Vector2.Distance(a, b);
            if (length < 16f)
            {
                return CommandResult.Fail("Channel is too short.");
            }
            return Apply(dryRun, Mathf.Min(a.x, b.x) - width, Mathf.Min(a.y, b.y) - width, Mathf.Max(a.x, b.x) + width, Mathf.Max(a.y, b.y) + width,
                delegate(Vector2 p, float current, out float target)
                {
                    Vector2 ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    float d = Vector2.Distance(p, a + ab * t);
                    target = current;
                    if (d > width * 0.5f)
                    {
                        return false;
                    }
                    float bed = Mathf.Lerp(startY, endY, t);
                    target = Mathf.Min(current, bed);
                    return target < current;
                });
        }

        // Level a circle to a height. mode: "lower" (only cut), "raise" (only fill) or "set".
        public static CommandResult Level(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            Vector2 c = new Vector2(JsonUtil.GetPointNumber(body, "center", "x", 0f), JsonUtil.GetPointNumber(body, "center", "z", 0f));
            float radius = Mathf.Clamp(JsonUtil.GetNumber(body, "radius", 40f), 8f, 400f);
            float height = JsonUtil.GetNumber(body, "height", float.NaN);
            string mode = JsonUtil.GetString(body, "mode", "set");
            if (float.IsNaN(height))
            {
                return CommandResult.Fail("height (metres) is required.");
            }
            return Apply(dryRun, c.x - radius, c.y - radius, c.x + radius, c.y + radius,
                delegate(Vector2 p, float current, out float target)
                {
                    target = current;
                    if (Vector2.Distance(p, c) > radius)
                    {
                        return false;
                    }
                    if (mode == "lower") { target = Mathf.Min(current, height); }
                    else if (mode == "raise") { target = Mathf.Max(current, height); }
                    else { target = height; }
                    return Mathf.Abs(target - current) > 0.01f;
                });
        }

        private delegate bool Shape(Vector2 world, float current, out float target);

        private static CommandResult Apply(bool dryRun, float minWX, float minWZ, float maxWX, float maxWZ, Shape shape)
        {
            if (!CostHelpers.FeatureUnlocked(UnlockManager.Feature.Landscaping))
            {
                return CostHelpers.FeatureLocked("Landscaping");
            }
            TerrainManager terrain = Singleton<TerrainManager>.instance;
            ushort[] heights = terrain.RawHeights;
            int res = TerrainManager.RAW_RESOLUTION;
            float cell = TerrainManager.RAW_CELL_SIZE;
            int minX = Mathf.Clamp(Mathf.FloorToInt(minWX / cell + res * 0.5f), 0, res);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(maxWX / cell + res * 0.5f), 0, res);
            int minZ = Mathf.Clamp(Mathf.FloorToInt(minWZ / cell + res * 0.5f), 0, res);
            int maxZ = Mathf.Clamp(Mathf.CeilToInt(maxWZ / cell + res * 0.5f), 0, res);

            long dirtDelta = 0;      // + = dirt removed from the ground (dug)
            int cells = 0;
            float lowest = float.MaxValue;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 world = new Vector2((x - res * 0.5f) * cell, (z - res * 0.5f) * cell);
                    if (CostHelpers.OutOfArea(new Vector3(world.x, 0f, world.y)))
                    {
                        continue;   // like the in-game tool, only land you own can be reshaped
                    }
                    int index = z * (res + 1) + x;
                    float current = heights[index] / HeightScale;
                    float target;
                    if (!shape(world, current, out target))
                    {
                        continue;
                    }
                    ushort raw = (ushort)Mathf.Clamp(Mathf.RoundToInt(target * HeightScale), 0, 65535);
                    dirtDelta += heights[index] - raw;
                    cells++;
                    lowest = Mathf.Min(lowest, target);
                    if (!dryRun)
                    {
                        heights[index] = raw;
                    }
                }
            }

            // Dirt economy: buffer absorbs what it can; the rest is bought or sold.
            long buffer = terrain.RawDirtBuffer;
            long after = buffer + dirtDelta;
            long traded = 0;
            if (after < 0) { traded = -after; after = 0; }
            else if (after > TerrainManager.DIRT_BUFFER_SIZE) { traded = after - TerrainManager.DIRT_BUFFER_SIZE; after = TerrainManager.DIRT_BUFFER_SIZE; }
            long costLong = traded * DirtBuySellPrice;
            int cost = (int)Math.Min(costLong, int.MaxValue);
            double cubicMetres = dirtDelta * (cell * cell) / HeightScale;

            string summary = ",\"cellsChanged\":" + cells +
                ",\"dirtMovedCubicMetres\":" + cubicMetres.ToString("0", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"dirtBufferBefore\":" + buffer + ",\"dirtBufferAfter\":" + after +
                ",\"cost\":" + CostHelpers.Money(cost) +
                ",\"lowestHeight\":" + JsonUtil.Number(cells > 0 ? lowest : 0f);
            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"affordable\":" + JsonUtil.Bool(CostHelpers.CanAffordLandscaping(cost)) + summary + "}");
            }
            if (!CostHelpers.CanAffordLandscaping(cost))
            {
                return CostHelpers.NotEnoughMoney(cost);
            }

            System.Reflection.FieldInfo dirtField = typeof(TerrainManager).GetField("m_dirtBuffer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (dirtField != null)
            {
                dirtField.SetValue(terrain, (int)after);
            }
            CostHelpers.ChargeLandscaping(cost);
            if (cells > 0)
            {
                TerrainModify.UpdateArea(minX - 1, minZ - 1, maxX + 1, maxZ + 1, true, true, false);
            }
            return CommandResult.FromJson("{\"ok\":true,\"dryRun\":false" + summary + "}");
        }
    }
}
