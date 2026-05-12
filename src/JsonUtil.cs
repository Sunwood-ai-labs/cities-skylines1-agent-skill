using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SkylinesAgentBridge
{
    public static class JsonUtil
    {
        public static string Escape(string value)
        {
            if (value == null)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\\') builder.Append("\\\\");
                else if (c == '"') builder.Append("\\\"");
                else if (c == '\n') builder.Append("\\n");
                else if (c == '\r') builder.Append("\\r");
                else if (c == '\t') builder.Append("\\t");
                else builder.Append(c);
            }

            return builder.ToString();
        }

        public static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        public static string Number(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public static string StringField(string name, string value)
        {
            return "\"" + name + "\":\"" + Escape(value) + "\"";
        }

        public static string GetString(string json, string name, string defaultValue)
        {
            Match match = Regex.Match(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*\"([^\"]*)\"");
            if (!match.Success)
            {
                return defaultValue;
            }

            return match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        public static bool GetBool(string json, string name, bool defaultValue)
        {
            Match match = Regex.Match(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return defaultValue;
            }

            return string.Compare(match.Groups[1].Value, "true", true) == 0;
        }

        public static float GetPointNumber(string json, string pointName, string axisName, float defaultValue)
        {
            Match point = Regex.Match(json, "\"" + Regex.Escape(pointName) + "\"\\s*:\\s*\\{([^}]*)\\}");
            if (!point.Success)
            {
                return defaultValue;
            }

            return GetNumber(point.Groups[1].Value, axisName, defaultValue);
        }

        public static float GetNumber(string json, string name, float defaultValue)
        {
            Match match = Regex.Match(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*(-?[0-9]+(?:\\.[0-9]+)?)");
            if (!match.Success)
            {
                return defaultValue;
            }

            float value;
            if (float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public static List<string> GetObjectArray(string json, string name)
        {
            List<string> objects = new List<string>();
            int property = json.IndexOf("\"" + name + "\"");
            if (property < 0)
            {
                return objects;
            }

            int arrayStart = json.IndexOf('[', property);
            if (arrayStart < 0)
            {
                return objects;
            }

            int depth = 0;
            int objectStart = -1;
            bool inString = false;
            bool escape = false;

            for (int i = arrayStart + 1; i < json.Length; i++)
            {
                char c = json[i];

                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                }
                else if (c == '{')
                {
                    if (depth == 0)
                    {
                        objectStart = i;
                    }
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && objectStart >= 0)
                    {
                        objects.Add(json.Substring(objectStart, i - objectStart + 1));
                        objectStart = -1;
                    }
                }
                else if (c == ']' && depth == 0)
                {
                    break;
                }
            }

            return objects;
        }
    }
}
