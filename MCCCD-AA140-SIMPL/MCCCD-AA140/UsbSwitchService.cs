// UsbSwitchService — owns the Crestron USB-SW-400 (USB 3.2 Data Matrix Switcher)
// on a static IPID. The room's USB peripherals (camera + Shure mic/speaker) sit
// on the switch's device ports and follow the selected HOST. The panel host
// selector (UsbHostSelect) is the only input; picking a host routes USB
// instantly. PowerUp seeds RoomPc; PowerDown leaves USB intact so a connected
// laptop isn't stranded mid-call.
//
// Control transport (per Crestron doc 9403, "USB 3.2 Data Matrix Switcher"):
// IPID peer-to-peer to the control system. The hardware Auto-Route toggle stays
// OFF — we drive routing explicitly. The actual SimplSharpPro device binding is
// isolated in BindDevice()/ApplyRoute(); SelectHost()'s contract behaviour
// (Fb echo, debug trace) works regardless, so a binding gap degrades gracefully
// (logs + the switch's own web UI / Auto-Route remain a fallback) rather than
// breaking the panel. Mirrors the existing field-config TODO pattern in
// SonyVplService / NewlineService.
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using MCCCD_AA140.Debug;

namespace MCCCD_AA140
{
    public class UsbSwitchService
    {
        private const uint IPID_USB_SW_400 = 0x31;

        public enum UsbHost { RoomPc = 1, AirMedia = 2, Laptop = 3 }

        // Host -> switch HOST-port index. Calibrate to the physical wiring at
        // commissioning (which host plugs into which USB-SW-400 host port).
        private static readonly Dictionary<UsbHost, uint> HostToPort = new Dictionary<UsbHost, uint> {
            { UsbHost.RoomPc,   1 },
            { UsbHost.AirMedia, 2 },
            { UsbHost.Laptop,   3 },
        };

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private UsbHost _current = UsbHost.RoomPc;

        public UsbSwitchService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            BindDevice();
            // Panel host selector (panel -> proc command). Value 1/2/3.
            _c.AA140.UsbHostSelect += (s, a) => {
                var v = (ushort)a.SigArgs.Sig.UShortValue;
                if (v < 1 || v > 3) { ErrorLog.Warn("USB: ignoring UsbHostSelect={0} (out of range)", v); return; }
                SelectHost((UsbHost)v);
            };
        }

        private void BindDevice()
        {
            // TODO field-config: register the USB-SW-400 SimplSharpPro device on
            // IPID_USB_SW_400 and subscribe OnlineStatusChange (emit
            // "usb_sw_online_change" via DebugTrace.Lifecycle, like NVX). Exact
            // device class confirmed at implementation (doc 9403, IPID peer-to-peer).
            DebugTrace.Lifecycle("usb_sw_init", new Dictionary<string, object> {
                { "device", "usb-sw-400" },
                { "ipid", IPID_USB_SW_400 },
            });
            ErrorLog.Notice("USB-SW-400: service initialized (IPID 0x{0:X2})", IPID_USB_SW_400);
        }

        /// <summary>Route the room USB peripherals to the chosen host and echo Fb.</summary>
        public void SelectHost(UsbHost host)
        {
            _current = host;
            uint port = HostToPort.TryGetValue(host, out var p) ? p : 1u;
            ApplyRoute(port);
            ErrorLog.Notice("USB: host -> {0} (switch host-port {1})", host, port);
            DebugTrace.Command("usb-sw-400", "select-host", host.ToString());
            _c.AA140.UsbHostSelectFb((sig, m) => sig.UShortValue = (ushort)host);
        }

        private void ApplyRoute(uint hostPort)
        {
            // TODO field-config: drive the matrix so the device ports (camera +
            // Shure USB) route to host column `hostPort`. No-op until the device
            // binding lands; the Fb + trace above still reflect intent.
        }

        public void SelectHostFromDebug(string key)
        {
            switch (key) {
                case "roompc":   SelectHost(UsbHost.RoomPc);   break;
                case "airmedia": SelectHost(UsbHost.AirMedia); break;
                case "laptop":   SelectHost(UsbHost.Laptop);   break;
            }
        }

        public UsbHost Current => _current;
    }
}
