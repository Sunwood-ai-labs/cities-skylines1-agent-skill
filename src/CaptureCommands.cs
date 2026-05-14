using System;
using System.IO;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class CaptureCommands
    {
        private const int MaxSuperSize = 4;

        public static CommandResult CaptureView(string body)
        {
            string preset = JsonUtil.GetString(body, "preset", "overview");
            string name = JsonUtil.GetString(body, "name", "");
            int superSize = Clamp((int)JsonUtil.GetNumber(body, "superSize", 1f), 1, MaxSuperSize);
            bool setCamera = JsonUtil.GetBool(body, "setCamera", true);

            CapturePreset capturePreset = CapturePreset.FromName(preset);
            if (setCamera)
            {
                ApplyCamera(body, capturePreset);
            }
            ApplyInfoMode(capturePreset);

            string directory = GetCaptureDirectory();
            Directory.CreateDirectory(directory);

            string fileName = BuildFileName(name, capturePreset.Name);
            string path = Path.Combine(directory, fileName);
            Application.CaptureScreenshot(path, superSize);

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"pending\":true");
            json.Append(",\"message\":\"Capture requested. Unity writes the PNG after the next rendered frame.\"");
            json.Append(",\"preset\":\"").Append(JsonUtil.Escape(capturePreset.Name)).Append("\"");
            json.Append(",\"infoMode\":\"").Append(JsonUtil.Escape(capturePreset.InfoMode.ToString())).Append("\"");
            json.Append(",\"subInfoMode\":\"").Append(JsonUtil.Escape(capturePreset.SubInfoMode.ToString())).Append("\"");
            json.Append(",\"superSize\":").Append(superSize);
            json.Append(",\"screen\":{\"width\":").Append(Screen.width).Append(",\"height\":").Append(Screen.height).Append("}");
            json.Append(",\"path\":\"").Append(JsonUtil.Escape(path)).Append("\"}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult ListCaptures()
        {
            string directory = GetCaptureDirectory();
            Directory.CreateDirectory(directory);

            string[] files = Directory.GetFiles(directory, "*.png");
            Array.Sort(files);
            Array.Reverse(files);

            StringBuilder items = new StringBuilder();
            bool first = true;
            int emitted = 0;
            for (int i = 0; i < files.Length && emitted < 200; i++)
            {
                FileInfo file = new FileInfo(files[i]);
                if (!first)
                {
                    items.Append(",");
                }

                items.Append("{\"file\":\"").Append(JsonUtil.Escape(file.Name)).Append("\"");
                items.Append(",\"path\":\"").Append(JsonUtil.Escape(file.FullName)).Append("\"");
                items.Append(",\"sizeBytes\":").Append(file.Length);
                items.Append(",\"lastWriteTime\":\"").Append(JsonUtil.Escape(file.LastWriteTime.ToString("s"))).Append("\"}");
                first = false;
                emitted++;
            }

            return CommandResult.FromJson("{\"ok\":true,\"directory\":\"" + JsonUtil.Escape(directory) +
                "\",\"total\":" + files.Length +
                ",\"returned\":" + emitted +
                ",\"captures\":[" + items.ToString() + "]}");
        }

        private static void ApplyCamera(string body, CapturePreset preset)
        {
            CameraController camera = ToolsModifierControl.cameraController;
            if (camera == null)
            {
                return;
            }

            Vector3 center = FindCityCenter();
            center.x = JsonUtil.GetPointNumber(body, "center", "x", JsonUtil.GetNumber(body, "x", center.x));
            center.y = JsonUtil.GetPointNumber(body, "center", "y", JsonUtil.GetNumber(body, "y", center.y));
            center.z = JsonUtil.GetPointNumber(body, "center", "z", JsonUtil.GetNumber(body, "z", center.z));

            float zoom = JsonUtil.GetNumber(body, "zoom", preset.Zoom);
            float height = JsonUtil.GetNumber(body, "height", preset.Height);
            float angleX = JsonUtil.GetNumber(body, "angleX", preset.AngleX);
            float angleY = JsonUtil.GetNumber(body, "angleY", preset.AngleY);

            camera.ClearTarget();
            camera.SetOverrideModeOff();
            camera.DisableFocusEffects();
            camera.m_freeCamera = true;
            camera.m_unlimitedCamera = true;
            camera.m_targetPosition = center;
            camera.m_currentPosition = center;
            camera.m_targetSize = zoom;
            camera.m_currentSize = zoom;
            camera.m_targetHeight = height;
            camera.m_currentHeight = height;
            camera.m_targetAngle = new Vector2(angleX, angleY);
            camera.m_currentAngle = new Vector2(angleX, angleY);
        }

        private static void ApplyInfoMode(CapturePreset preset)
        {
            InfoManager manager = Singleton<InfoManager>.instance;
            if (manager == null)
            {
                return;
            }

            manager.SetCurrentMode(preset.InfoMode, preset.SubInfoMode);
            manager.UpdateInfoMode();
        }

        private static Vector3 FindCityCenter()
        {
            NetManager manager = NetManager.instance;
            BuildingManager buildings = BuildingManager.instance;
            Bounds2D bounds = new Bounds2D();
            int count = 0;

            for (ushort i = 1; i < manager.m_nodes.m_buffer.Length; i++)
            {
                NetNode node = manager.m_nodes.m_buffer[i];
                if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.None)
                {
                    continue;
                }

                if (AddPoint(bounds, node.m_position))
                {
                    count++;
                }
            }

            for (ushort i = 1; i < buildings.m_buildings.m_buffer.Length; i++)
            {
                Building building = buildings.m_buildings.m_buffer[i];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None ||
                    (building.m_flags & Building.Flags.Deleted) != Building.Flags.None)
                {
                    continue;
                }

                if (AddPoint(bounds, building.m_position))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return Vector3.zero;
            }

            return new Vector3((bounds.MinX + bounds.MaxX) * 0.5f, 0f, (bounds.MinZ + bounds.MaxZ) * 0.5f);
        }

        private static bool AddPoint(Bounds2D bounds, Vector3 position)
        {
            if (Mathf.Abs(position.x) > 8600f || Mathf.Abs(position.z) > 8600f)
            {
                return false;
            }

            if (!bounds.HasValue)
            {
                bounds.MinX = position.x;
                bounds.MaxX = position.x;
                bounds.MinZ = position.z;
                bounds.MaxZ = position.z;
                bounds.HasValue = true;
                return true;
            }

            if (position.x < bounds.MinX) bounds.MinX = position.x;
            if (position.x > bounds.MaxX) bounds.MaxX = position.x;
            if (position.z < bounds.MinZ) bounds.MinZ = position.z;
            if (position.z > bounds.MaxZ) bounds.MaxZ = position.z;
            return true;
        }

        private static string GetCaptureDirectory()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Colossal Order\\Cities_Skylines\\Addons\\Mods\\SkylinesAgentBridge\\Captures");
        }

        private static string BuildFileName(string requestedName, string preset)
        {
            string stem = requestedName == null || requestedName.Trim().Length == 0
                ? "city-" + preset + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")
                : requestedName.Trim();

            stem = SanitizeFileName(stem);
            if (!stem.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                stem += ".png";
            }

            return stem;
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool bad = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j])
                    {
                        bad = true;
                        break;
                    }
                }

                builder.Append(bad ? '-' : c);
            }

            string result = builder.ToString().Trim();
            return result.Length == 0 ? "city-capture" : result;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private sealed class CapturePreset
        {
            public string Name;
            public InfoManager.InfoMode InfoMode;
            public InfoManager.SubInfoMode SubInfoMode;
            public float Zoom;
            public float Height;
            public float AngleX;
            public float AngleY;

            public static CapturePreset FromName(string value)
            {
                string normalized = value == null ? "" : value.Trim().ToLowerInvariant();
                if (normalized == "transport" || normalized == "transit" || normalized == "route-map" || normalized == "routes")
                {
                    return new CapturePreset
                    {
                        Name = "transport",
                        InfoMode = InfoManager.InfoMode.Transport,
                        SubInfoMode = InfoManager.SubInfoMode.NormalTransport,
                        Zoom = 4600f,
                        Height = 2800f,
                        AngleX = 0f,
                        AngleY = 90f
                    };
                }

                if (normalized == "underground" || normalized == "metro" || normalized == "subway" || normalized == "tunnels")
                {
                    return new CapturePreset
                    {
                        Name = "underground",
                        InfoMode = InfoManager.InfoMode.Underground,
                        SubInfoMode = InfoManager.SubInfoMode.UndergroundTunnels,
                        Zoom = 4600f,
                        Height = 2800f,
                        AngleX = 0f,
                        AngleY = 90f
                    };
                }

                return new CapturePreset
                {
                    Name = "overview",
                    InfoMode = InfoManager.InfoMode.None,
                    SubInfoMode = InfoManager.SubInfoMode.Default,
                    Zoom = 4600f,
                    Height = 2800f,
                    AngleX = 0f,
                    AngleY = 90f
                };
            }
        }

        private sealed class Bounds2D
        {
            public bool HasValue;
            public float MinX;
            public float MaxX;
            public float MinZ;
            public float MaxZ;
        }
    }
}
