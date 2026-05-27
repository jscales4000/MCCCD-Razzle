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
        private const int RECONNECT_DELAY_MS = 5000;
        private const int RX_BUFFER_SIZE     = 4096;

        private readonly string _host;
        private readonly int _port;
        private readonly string _name;
        private TCPClient _client;
        private readonly StringBuilder _rxBuf = new StringBuilder();
        private CTimer _reconnectTimer;

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
                c.ReceiveDataAsync(OnDataReceived);
                OnConnected?.Invoke(this);
            } else {
                ErrorLog.Warn("Shure {0}: connect returned status {1}", _name, c.ClientStatus);
                ScheduleReconnect();
            }
        }

        private void OnSocketStatusChange(TCPClient c, SocketStatus status)
        {
            if (status != SocketStatus.SOCKET_STATUS_CONNECTED) {
                ErrorLog.Notice("Shure {0}: socket {1}, reconnecting in {2}ms", _name, status, RECONNECT_DELAY_MS);
                ScheduleReconnect();
            }
        }

        private void ScheduleReconnect()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = new CTimer(_ => Connect(), RECONNECT_DELAY_MS);
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
