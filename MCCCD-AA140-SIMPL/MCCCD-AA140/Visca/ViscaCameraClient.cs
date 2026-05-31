// ViscaCameraClient — thin persistent TCP link to one 1Beyond IV-CAM speaking
// Sony VISCA on port 5500. Sends command frames; logs replies (ACK/Completion/
// Error) and connect lifecycle to DebugTrace so the debug panel shows real
// camera online status. Owns reconnect on drop. Streamlined from the ISMIv3
// ViscaCamera (no inquiry/telemetry/queue — the AA140 panel only fires
// human-paced PTZ/zoom/preset commands).
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using MCCCD_AA140.Debug;

namespace MCCCD_AA140.Visca
{
    public class ViscaCameraClient
    {
        public const int DefaultPort = 5500;

        private const int RECONNECT_FAST_MS       = 5000;
        private const int RECONNECT_SLOW_MS       = 60000;
        private const int FAST_RECONNECT_ATTEMPTS = 3;
        private const int RX_BUFFER_SIZE          = 1024;

        private string _host;
        private readonly int _port;
        private readonly string _name;
        private TCPClient _client;
        private readonly List<byte> _rxBuf = new List<byte>(64);
        private CTimer _reconnectTimer;
        private int _failedAttempts;
        private bool _enabled;
        private readonly object _stateLock = new object();

        // Inquiry polling — one inquiry in flight at a time (cameras = 1 socket).
        private enum Inq { None, PanTilt, Zoom, Tracking }
        private Inq _pending = Inq.None;
        private CTimer _pollTimer;
        private int _pollCycle;

        public short  PanPosition  { get; private set; }
        public short  TiltPosition { get; private set; }
        public ushort ZoomPosition { get; private set; }
        public bool   TrackingActive { get; private set; }

        public ViscaCameraClient(string host, int port, string name)
        {
            _host = host;
            _port = port;
            _name = name;
        }

        public bool IsConnected
        {
            get { return _client != null && _client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
        }

        public string Host { get { return _host; } }
        public bool Enabled { get { return _enabled; } }

        public void Start() { lock (_stateLock) { _enabled = true; } Connect(); }
        public void Stop()  { lock (_stateLock) { _enabled = false; } CloseAndCancelReconnect(); }
        public void SetEnabled(bool value) { if (value) Start(); else Stop(); }

        public void SetHost(string host)
        {
            lock (_stateLock) {
                if (host == _host) return;
                _host = host ?? "";
                _failedAttempts = 0;
            }
            CloseAndCancelReconnect();
            if (_enabled) Connect();
        }

        /// <summary>Send a VISCA command frame. Dropped (logged) if not connected.</summary>
        public void Send(byte[] frame)
        {
            if (frame == null) return;
            if (!IsConnected) {
                DebugTrace.Error(_name, "drop (not connected): " + ViscaProtocol.Hex(frame));
                return;
            }
            _client.SendDataAsync(frame, frame.Length, null);
        }

        private void CloseAndCancelReconnect()
        {
            try { _pollTimer?.Dispose(); _pollTimer = null; } catch { }
            try { _reconnectTimer?.Dispose(); _reconnectTimer = null; } catch { }
            try {
                if (_client != null) {
                    _client.DisconnectFromServer();
                    _client.Dispose();
                    _client = null;
                }
            } catch { }
        }

        private void Connect()
        {
            lock (_stateLock) {
                if (!_enabled) return;
                if (string.IsNullOrEmpty(_host)) return;
            }
            try {
                _client = new TCPClient(_host, _port, RX_BUFFER_SIZE);
                _client.SocketStatusChange += OnSocketStatusChange;
                _client.ConnectToServerAsync(OnConnectComplete);
            } catch (System.Exception ex) {
                ErrorLog.Error("Visca {0}: connect setup failed: {1}", _name, ex.Message);
                ScheduleReconnect();
            }
        }

        private void OnConnectComplete(TCPClient c)
        {
            if (c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) {
                ErrorLog.Notice("Visca {0}: connected to {1}:{2}", _name, _host, _port);
                DebugTrace.Lifecycle("device_connected", new Dictionary<string, object> {
                    { "device", _name.ToLowerInvariant() },
                    { "host", _host },
                    { "port", _port },
                });
                _failedAttempts = 0;
                c.ReceiveDataAsync(OnDataReceived);
                _pending = Inq.None;
                _pollCycle = 0;
                _pollTimer?.Dispose();
                _pollTimer = new CTimer(_ => PollTick(), null, 500, 333);
            } else {
                _failedAttempts++;
                if (_failedAttempts <= FAST_RECONNECT_ATTEMPTS || _failedAttempts % 10 == 0) {
                    ErrorLog.Warn("Visca {0}: connect status {1} (attempt {2})", _name, c.ClientStatus, _failedAttempts);
                    DebugTrace.Lifecycle("device_connect_failed", new Dictionary<string, object> {
                        { "device", _name.ToLowerInvariant() },
                        { "host", _host },
                        { "status", c.ClientStatus.ToString() },
                        { "attempt", _failedAttempts },
                    });
                }
                ScheduleReconnect();
            }
        }

        private void OnSocketStatusChange(TCPClient c, SocketStatus status)
        {
            if (status != SocketStatus.SOCKET_STATUS_CONNECTED) {
                DebugTrace.Lifecycle("device_socket_change", new Dictionary<string, object> {
                    { "device", _name.ToLowerInvariant() },
                    { "status", status.ToString() },
                });
                ScheduleReconnect();
            }
        }

        private void ScheduleReconnect()
        {
            lock (_stateLock) { if (!_enabled) return; }
            _reconnectTimer?.Dispose();
            int delay = _failedAttempts < FAST_RECONNECT_ATTEMPTS ? RECONNECT_FAST_MS : RECONNECT_SLOW_MS;
            _reconnectTimer = new CTimer(_ => Connect(), delay);
        }

        // Send the next position/tracking inquiry. One in flight; drop a stale one
        // and keep cycling so a missed reply never wedges the poll.
        private void PollTick()
        {
            if (!IsConnected) return;
            if (_pending != Inq.None) _pending = Inq.None;
            _pollCycle++;
            if (_pollCycle % 3 == 0) { _pending = Inq.Tracking; SendRaw(ViscaProtocol.TrackingInq()); }
            else                     { _pending = Inq.PanTilt;  SendRaw(ViscaProtocol.PanTiltPosInq()); }
        }

        // Send without the "not connected" drop-logging used by command Send().
        private void SendRaw(byte[] frame)
        {
            if (frame == null || !IsConnected) return;
            _client.SendDataAsync(frame, frame.Length, null);
        }

        private void OnDataReceived(TCPClient c, int bytesReceived)
        {
            if (bytesReceived > 0) {
                for (int i = 0; i < bytesReceived; i++) {
                    byte b = c.IncomingDataBuffer[i];
                    _rxBuf.Add(b);
                    if (b == ViscaProtocol.FrameEnd) {
                        var frame = _rxBuf.ToArray();
                        _rxBuf.Clear();
                        HandleFrame(frame);
                    }
                }
            }
            if (c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) {
                c.ReceiveDataAsync(OnDataReceived);
            }
        }

        private void HandleFrame(byte[] frame)
        {
            var kind = ViscaProtocol.Categorize(frame, out var payload);
            if (kind == ViscaProtocol.Kind.InquiryResponse && _pending != Inq.None) {
                var which = _pending; _pending = Inq.None;
                if (which == Inq.PanTilt) {
                    if (ViscaProtocol.ParsePanTilt(payload, out var pan, out var tilt)) {
                        lock (_stateLock) { PanPosition = pan; TiltPosition = tilt; }
                    }
                    // chain the zoom inquiry right after pan/tilt
                    _pending = Inq.Zoom; SendRaw(ViscaProtocol.ZoomPosInq());
                    return;
                }
                if (which == Inq.Zoom) {
                    if (ViscaProtocol.ParseZoom(payload, out var z)) { lock (_stateLock) { ZoomPosition = z; } }
                    return;
                }
                if (which == Inq.Tracking) {
                    lock (_stateLock) { TrackingActive = ViscaProtocol.ParseTrackingActive(payload); }
                    return;
                }
            }
            // commands (ACK/Completion/Error) — log for the debug feed
            DebugTrace.Response(_name, ViscaProtocol.Hex(frame) + " [" + ViscaProtocol.ReplyKind(frame) + "]");
        }
    }
}
