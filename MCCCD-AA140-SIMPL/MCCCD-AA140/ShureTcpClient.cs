using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

namespace MCCCD_AA140
{
    /// <summary>
    /// Reusable thin TCP client for the Shure ASCII control protocol used by the
    /// P300-IMX, MXA-series ceiling arrays, and other Shure IntelliMix gear.
    ///
    /// Wire format: ASCII commands wrapped in `&lt; ... &gt;` — the brackets are
    /// part of every frame. The server pushes REP notifications when any
    /// parameter changes (no polling needed) and SAMPLE_IN/OUT/PROC meter frames
    /// when subscribed via METER_RATE_*. Frames are delimited by '&gt;'.
    ///
    /// Owners (P300 service, MXA service, etc.) attach to OnConnected to send
    /// initial state-sync commands, and to OnFrame to handle inbound REP/SAMPLE
    /// frames. The client owns reconnect on socket drop.
    /// </summary>
    public class ShureTcpClient
    {
        // Reconnect backoff: 5s for the first N tries, then 60s steady-state.
        // Avoids flooding the err log when stub IPs (placeholder devices not
        // yet on the network) reject TCP connects. Once a successful connect
        // happens, the fast cadence resumes for the next outage.
        private const int RECONNECT_FAST_MS       = 5000;
        private const int RECONNECT_SLOW_MS       = 60000;
        private const int FAST_RECONNECT_ATTEMPTS = 3;
        private const int RX_BUFFER_SIZE          = 4096;

        private readonly string _host;
        private readonly int _port;
        private readonly string _name;
        private TCPClient _client;
        private readonly StringBuilder _rxBuf = new StringBuilder();
        private CTimer _reconnectTimer;
        private int _failedAttempts; // resets on successful connect

        public System.Action<ShureTcpClient> OnConnected;
        public System.Action<string> OnFrame;

        public ShureTcpClient(string host, int port, string name)
        {
            _host = host;
            _port = port;
            _name = name;
        }

        public bool IsConnected
        {
            get { return _client != null && _client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
        }

        public void Start() { Connect(); }

        public void Send(string command)
        {
            if (!IsConnected) {
                ErrorLog.Notice("Shure {0}: drop (not connected): {1}", _name, command);
                return;
            }
            var bytes = Encoding.ASCII.GetBytes(command);
            _client.SendDataAsync(bytes, bytes.Length, null);
        }

        private void Connect()
        {
            try {
                _client = new TCPClient(_host, _port, RX_BUFFER_SIZE);
                _client.SocketStatusChange += OnSocketStatusChange;
                _client.ConnectToServerAsync(OnConnectComplete);
            } catch (System.Exception ex) {
                ErrorLog.Error("Shure {0}: connect setup failed: {1}", _name, ex.Message);
                ScheduleReconnect();
            }
        }

        private void OnConnectComplete(TCPClient c)
        {
            if (c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) {
                ErrorLog.Notice("Shure {0}: connected to {1}:{2}", _name, _host, _port);
                _failedAttempts = 0; // resume fast reconnect on next drop
                c.ReceiveDataAsync(OnDataReceived);
                OnConnected?.Invoke(this);
            } else {
                _failedAttempts++;
                // Log every fast-attempt failure; once we switch to slow,
                // log every 10th retry so the operator still sees periodic
                // status without the log being overwhelmed.
                if (_failedAttempts <= FAST_RECONNECT_ATTEMPTS || _failedAttempts % 10 == 0) {
                    ErrorLog.Warn("Shure {0}: connect returned status {1} (attempt {2})",
                        _name, c.ClientStatus, _failedAttempts);
                }
                ScheduleReconnect();
            }
        }

        private void OnSocketStatusChange(TCPClient c, SocketStatus status)
        {
            if (status != SocketStatus.SOCKET_STATUS_CONNECTED) {
                if (_failedAttempts <= FAST_RECONNECT_ATTEMPTS) {
                    ErrorLog.Notice("Shure {0}: socket {1}, reconnecting", _name, status);
                }
                ScheduleReconnect();
            }
        }

        private void ScheduleReconnect()
        {
            _reconnectTimer?.Dispose();
            int delay = _failedAttempts < FAST_RECONNECT_ATTEMPTS ? RECONNECT_FAST_MS : RECONNECT_SLOW_MS;
            _reconnectTimer = new CTimer(_ => Connect(), delay);
        }

        private void OnDataReceived(TCPClient c, int bytesReceived)
        {
            if (bytesReceived > 0) {
                var chunk = Encoding.ASCII.GetString(c.IncomingDataBuffer, 0, bytesReceived);
                _rxBuf.Append(chunk);
                ProcessFrames();
            }
            if (c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) {
                c.ReceiveDataAsync(OnDataReceived);
            }
        }

        private void ProcessFrames()
        {
            // Frames delimited by '>'. Anything past the last '>' is partial; keep it.
            var s = _rxBuf.ToString();
            int gt;
            int lastEnd = 0;
            while ((gt = s.IndexOf('>', lastEnd)) >= 0) {
                int start = s.IndexOf('<', lastEnd);
                if (start < 0 || start > gt) { lastEnd = gt + 1; continue; }
                var frame = s.Substring(start, gt - start + 1);
                OnFrame?.Invoke(frame);
                lastEnd = gt + 1;
            }
            _rxBuf.Clear();
            if (lastEnd < s.Length) _rxBuf.Append(s.Substring(lastEnd));
        }
    }
}
