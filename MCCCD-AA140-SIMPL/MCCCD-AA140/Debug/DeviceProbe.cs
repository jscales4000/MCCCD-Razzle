// DeviceProbe — one-shot reachability probe for the debug panel's Ping button.
// TCP devices: attempt a bounded TCP connect to host:port (proves the control
// port is open — more meaningful than ICMP). REST devices: HTTP GET the root;
// any HTTP response (even 401/404) means the box answered. Returns a small
// result the DebugServer serializes to JSON. Never throws.
using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharp.Net.Http;

namespace MCCCD_AA140.Debug
{
    public static class DeviceProbe
    {
        public class Result
        {
            public bool Reachable;
            public string Detail;   // "connected" | "http <code>" | "timeout" | "refused" | "no host"
        }

        private const int TIMEOUT_MS = 1500;

        // Control ports for the TCP-spoken devices.
        private static readonly Dictionary<string, int> TcpPorts = new Dictionary<string, int> {
            { "p300",    2202 },
            { "mxa-a",   2202 },
            { "mxa-b",   2202 },
            { "sony-1",  53595 },
            { "sony-2",  53595 },
            { "newline", 6688 },
        };

        // REST devices probed over HTTP.
        private static readonly HashSet<string> HttpKeys = new HashSet<string> {
            "cam-1", "cam-2", "airmedia",
        };

        public static Result Probe(string key, string host)
        {
            if (string.IsNullOrEmpty(host) || host == "0.0.0.0")
                return new Result { Reachable = false, Detail = "no host" };
            if (TcpPorts.TryGetValue(key, out int port))
                return ProbeTcp(host, port);
            if (HttpKeys.Contains(key))
                return ProbeHttp(host);
            // Unknown key (e.g. IPID devices) — no IP-based probe available.
            return new Result { Reachable = false, Detail = "no probe for key" };
        }

        private static Result ProbeTcp(string host, int port)
        {
            TCPClient client = null;
            var done = new CEvent(false, false);
            bool connected = false;
            try {
                client = new TCPClient(host, port, 256);
                client.ConnectToServerAsync(c => {
                    connected = c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED;
                    try { done.Set(); } catch { }
                });
                bool signaled = done.Wait(TIMEOUT_MS);
                if (!signaled) return new Result { Reachable = false, Detail = "timeout" };
                return new Result { Reachable = connected, Detail = connected ? "connected" : "refused" };
            } catch (Exception ex) {
                return new Result { Reachable = false, Detail = "error: " + ex.Message };
            } finally {
                try { client?.DisconnectFromServer(); client?.Dispose(); } catch { }
                try { done.Close(); } catch { }
            }
        }

        private static Result ProbeHttp(string host)
        {
            try {
                using (var c = new HttpClient()) {
                    c.TimeoutEnabled = true;
                    c.Timeout = 2; // seconds
                    var req = new HttpClientRequest {
                        Url = new UrlParser("http://" + host + "/"),
                        RequestType = RequestType.Get,
                    };
                    var resp = c.Dispatch(req);
                    return new Result { Reachable = true, Detail = "http " + resp.Code };
                }
            } catch (Exception ex) {
                return new Result { Reachable = false, Detail = "error: " + ex.Message };
            }
        }
    }
}
