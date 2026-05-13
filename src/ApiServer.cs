using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public sealed class ApiServer
    {
        private readonly AgentBridge bridge;
        private readonly int port;
        private TcpListener listener;
        private Thread thread;
        private volatile bool running;

        public ApiServer(AgentBridge bridge, int port)
        {
            this.bridge = bridge;
            this.port = port;
        }

        public bool IsRunning
        {
            get { return running; }
        }

        public void Start()
        {
            if (running)
            {
                return;
            }

            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            running = true;

            thread = new Thread(AcceptLoop);
            thread.IsBackground = true;
            thread.Name = "Skylines Agent Bridge API";
            thread.Start();

            Debug.Log("[SkylinesAgentBridge] API server listening on http://127.0.0.1:" + port);
        }

        private void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (Exception ex)
                {
                    Debug.Log("[SkylinesAgentBridge] API accept failed: " + ex.Message);
                }
            }
        }

        private void HandleClient(object state)
        {
            TcpClient client = (TcpClient)state;

            try
            {
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                NetworkStream stream = client.GetStream();
                HttpRequest request = HttpRequest.Read(stream);
                HttpResponse response = Route(request);
                response.Write(stream);
            }
            catch (Exception ex)
            {
                try
                {
                    HttpResponse.Json(500, "{\"ok\":false,\"error\":\"" + JsonUtil.Escape(ex.Message) + "\"}").Write(client.GetStream());
                }
                catch
                {
                }
            }
            finally
            {
                client.Close();
            }
        }

        private HttpResponse Route(HttpRequest request)
        {
            if (request.Method == "OPTIONS")
            {
                return HttpResponse.Json(200, "{\"ok\":true}");
            }

            if (request.Method == "GET" && request.Path == "/health")
            {
                return HttpResponse.Json(200, "{\"ok\":true,\"mod\":\"Skylines Agent Bridge\",\"levelLoaded\":" + JsonUtil.Bool(bridge.LevelLoaded) + ",\"port\":" + port + "}");
            }

            if (request.Method == "GET" && request.Path == "/state/summary")
            {
                return RunOnGameThread(request, GameState.BuildSummaryJson);
            }

            if (request.Method == "GET" && request.Path == "/state/problems")
            {
                int limit = request.GetQueryInt("limit", 200);
                return RunOnGameThread(request, delegate { return GameState.BuildProblemsJson(limit); });
            }

            if (request.Method == "GET" && request.Path == "/state/economy")
            {
                return RunOnGameThread(request, GameState.BuildEconomyJson);
            }

            if (request.Method == "GET" && request.Path == "/state/facilities")
            {
                int limit = request.GetQueryInt("limit", 500);
                string service = request.GetQueryString("service", "");
                bool includeMapObjects = request.GetQueryString("includeMapObjects", "false") == "true";
                return RunOnGameThread(request, delegate { return GameState.BuildFacilitiesJson(limit, service, includeMapObjects); });
            }

            if (request.Method == "GET" && request.Path == "/state/networks")
            {
                int limit = request.GetQueryInt("limit", 500);
                string service = request.GetQueryString("service", "");
                return RunOnGameThread(request, delegate { return GameState.BuildNetworksJson(limit, service); });
            }

            if (request.Method == "GET" && request.Path == "/state/road-anomalies")
            {
                int limit = request.GetQueryInt("limit", 200);
                float nearMissDistance = request.GetQueryFloat("nearMissDistance", 16f);
                float shortSegmentLength = request.GetQueryFloat("shortSegmentLength", 28f);
                bool includeDeadEnds = request.GetQueryString("includeDeadEnds", "true") == "true";
                return RunOnGameThread(request, delegate { return GameState.BuildRoadAnomaliesJson(limit, nearMissDistance, shortSegmentLength, includeDeadEnds); });
            }

            if (request.Method == "GET" && request.Path == "/state/building-anomalies")
            {
                int limit = request.GetQueryInt("limit", 200);
                return RunOnGameThread(request, delegate { return GameState.BuildBuildingAnomaliesJson(limit); });
            }

            if (request.Method == "GET" && request.Path == "/state/saves")
            {
                return RunOnGameThread(request, SaveCommands.ListSaves);
            }

            if (request.Method == "GET" && request.Path == "/prefabs/roads")
            {
                return RunOnGameThread(request, GameState.BuildRoadPrefabsJson);
            }

            if (request.Method == "GET" && request.Path == "/prefabs/networks")
            {
                string service = request.GetQueryString("service", "");
                return RunOnGameThread(request, delegate { return GameState.BuildNetworkPrefabsJson(service); });
            }

            if (request.Method == "GET" && request.Path == "/prefabs/buildings")
            {
                string service = request.GetQueryString("service", "");
                return RunOnGameThread(request, delegate { return GameState.BuildBuildingPrefabsJson(service); });
            }

            if (request.Method == "POST" && request.Path == "/commands/build-road")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return RoadCommands.BuildRoad(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/build-network")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return RoadCommands.BuildRoad(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-zone")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return ZoneCommands.SetZone(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/place-building")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BuildingCommands.PlaceBuilding(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/move-building")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BuildingCommands.MoveBuilding(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-simulation-speed")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return SimulationCommands.SetSimulationSpeed(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-tax-rate")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return EconomyCommands.SetTaxRate(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/bulldoze")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BulldozeCommands.Bulldoze(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/save")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return SaveCommands.Save(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/batch")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BatchCommands.Execute(body); });
            }

            return HttpResponse.Json(404, "{\"ok\":false,\"error\":\"Not found\"}");
        }

        private HttpResponse RunOnGameThread(HttpRequest request, Func<CommandResult> action)
        {
            if (!bridge.LevelLoaded)
            {
                return HttpResponse.Json(409, "{\"ok\":false,\"error\":\"No city is loaded.\"}");
            }

            CommandResult result = bridge.Queue.RunSync(action, 10000);
            bridge.Queue.RunSync(delegate
            {
                AgentBridgeNotifier.Notify((result.Ok ? "API OK: " : "API FAIL: ") + DescribeRequest(request));
                return CommandResult.FromJson("{\"ok\":true}");
            }, 10000);
            return HttpResponse.Json(result.Ok ? 200 : 500, result.Json);
        }

        private static string DescribeRequest(HttpRequest request)
        {
            string body = request.Body == null ? "" : request.Body;

            if (request.Method == "GET")
            {
                if (request.Path == "/state/summary") return "Read city summary";
                if (request.Path == "/state/problems") return "Read city problems";
                if (request.Path == "/state/economy") return "Read economy state";
                if (request.Path == "/state/facilities") return "Read facilities";
                if (request.Path == "/state/networks") return "Read networks";
                if (request.Path == "/state/road-anomalies") return "Inspect road anomalies";
                if (request.Path == "/state/building-anomalies") return "Inspect building placement";
                if (request.Path == "/state/saves") return "List saves";
                if (request.Path == "/prefabs/roads") return "List road prefabs";
                if (request.Path == "/prefabs/networks") return "List network prefabs";
                if (request.Path == "/prefabs/buildings") return "List building prefabs";
                return "GET " + request.Path;
            }

            if (request.Path == "/commands/build-network" || request.Path == "/commands/build-road")
            {
                string prefab = JsonUtil.GetString(body, "roadPrefab", "network");
                return "Build network " + prefab;
            }

            if (request.Path == "/commands/set-zone")
            {
                return "Set zone " + JsonUtil.GetString(body, "zone", "");
            }

            if (request.Path == "/commands/place-building")
            {
                return "Place building " + JsonUtil.GetString(body, "buildingPrefab", "");
            }

            if (request.Path == "/commands/move-building")
            {
                return "Move building #" + ((int)JsonUtil.GetNumber(body, "id", 0f)).ToString();
            }

            if (request.Path == "/commands/bulldoze")
            {
                string entityType = JsonUtil.GetString(body, "entityType", "entity");
                return "Bulldoze " + entityType + " #" + ((int)JsonUtil.GetNumber(body, "id", 0f)).ToString();
            }

            if (request.Path == "/commands/save")
            {
                return "Save city " + JsonUtil.GetString(body, "name", "AgentAutoSave");
            }

            if (request.Path == "/commands/set-simulation-speed")
            {
                if (JsonUtil.GetBool(body, "paused", false))
                {
                    return "Pause simulation";
                }
                return "Set simulation speed " + ((int)JsonUtil.GetNumber(body, "speed", 0f)).ToString();
            }

            if (request.Path == "/commands/set-tax-rate")
            {
                return "Set tax rate " + ((int)JsonUtil.GetNumber(body, "rate", 0f)).ToString();
            }

            if (request.Path == "/commands/batch")
            {
                return "Run batch commands";
            }

            return request.Method + " " + request.Path;
        }

        private sealed class HttpRequest
        {
            public string Method;
            public string Path;
            public string Query;
            public string Body;

            public int GetQueryInt(string name, int defaultValue)
            {
                if (Query == null || Query.Length == 0)
                {
                    return defaultValue;
                }

                string[] pairs = Query.Split('&');
                for (int i = 0; i < pairs.Length; i++)
                {
                    string[] parts = pairs[i].Split('=');
                    if (parts.Length == 2 && parts[0] == name)
                    {
                        int value;
                        if (int.TryParse(parts[1], out value))
                        {
                            return value;
                        }
                    }
                }

                return defaultValue;
            }

            public float GetQueryFloat(string name, float defaultValue)
            {
                if (Query == null || Query.Length == 0)
                {
                    return defaultValue;
                }

                string[] pairs = Query.Split('&');
                for (int i = 0; i < pairs.Length; i++)
                {
                    string[] parts = pairs[i].Split('=');
                    if (parts.Length == 2 && parts[0] == name)
                    {
                        float value;
                        if (float.TryParse(parts[1], out value))
                        {
                            return value;
                        }
                    }
                }

                return defaultValue;
            }

            public string GetQueryString(string name, string defaultValue)
            {
                if (Query == null || Query.Length == 0)
                {
                    return defaultValue;
                }

                string[] pairs = Query.Split('&');
                for (int i = 0; i < pairs.Length; i++)
                {
                    string[] parts = pairs[i].Split('=');
                    if (parts.Length == 2 && parts[0] == name)
                    {
                        return Uri.UnescapeDataString(parts[1].Replace("+", " "));
                    }
                }

                return defaultValue;
            }

            public static HttpRequest Read(NetworkStream stream)
            {
                MemoryStream headerBytes = new MemoryStream();
                int matched = 0;

                while (true)
                {
                    int b = stream.ReadByte();
                    if (b < 0)
                    {
                        break;
                    }

                    headerBytes.WriteByte((byte)b);

                    if ((matched == 0 && b == '\r') ||
                        (matched == 1 && b == '\n') ||
                        (matched == 2 && b == '\r') ||
                        (matched == 3 && b == '\n'))
                    {
                        matched++;
                        if (matched == 4)
                        {
                            break;
                        }
                    }
                    else
                    {
                        matched = b == '\r' ? 1 : 0;
                    }

                    if (headerBytes.Length > 65536)
                    {
                        throw new InvalidOperationException("Request headers are too large.");
                    }
                }

                string headers = Encoding.UTF8.GetString(headerBytes.ToArray());
                string[] lines = headers.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                string[] first = lines[0].Split(' ');
                int contentLength = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int colon = line.IndexOf(':');
                    if (colon > 0 && string.Compare(line.Substring(0, colon), "Content-Length", true) == 0)
                    {
                        int.TryParse(line.Substring(colon + 1).Trim(), out contentLength);
                    }
                }

                byte[] bodyBytes = new byte[contentLength];
                int offset = 0;
                while (offset < contentLength)
                {
                    int read = stream.Read(bodyBytes, offset, contentLength - offset);
                    if (read <= 0)
                    {
                        break;
                    }
                    offset += read;
                }

                HttpRequest request = new HttpRequest();
                request.Method = first.Length > 0 ? first[0].ToUpperInvariant() : "";
                string target = first.Length > 1 ? first[1] : "/";
                int queryIndex = target.IndexOf('?');
                if (queryIndex >= 0)
                {
                    request.Path = target.Substring(0, queryIndex);
                    request.Query = target.Substring(queryIndex + 1);
                }
                else
                {
                    request.Path = target;
                    request.Query = "";
                }
                request.Body = Encoding.UTF8.GetString(bodyBytes, 0, offset);
                return request;
            }
        }

        private sealed class HttpResponse
        {
            private readonly int status;
            private readonly string body;

            private HttpResponse(int status, string body)
            {
                this.status = status;
                this.body = body;
            }

            public static HttpResponse Json(int status, string body)
            {
                return new HttpResponse(status, body);
            }

            public void Write(NetworkStream stream)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                string header = "HTTP/1.1 " + status + " " + Reason(status) + "\r\n" +
                    "Content-Type: application/json; charset=utf-8\r\n" +
                    "Content-Length: " + bodyBytes.Length + "\r\n" +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                    "Access-Control-Allow-Headers: Content-Type\r\n" +
                    "Connection: close\r\n\r\n";

                byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                stream.Write(headerBytes, 0, headerBytes.Length);
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            private static string Reason(int status)
            {
                if (status == 200) return "OK";
                if (status == 404) return "Not Found";
                if (status == 409) return "Conflict";
                return "Internal Server Error";
            }
        }
    }
}
