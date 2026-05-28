using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using MCCCD_AA140.Debug;

namespace MCCCD_AA140
{
    /// <summary>
    /// Direct SmartObject 1 dispatcher bypassing the Contract Editor's
    /// generated wrappers (Main.g.cs) which have misaligned join names vs
    /// what the panel actually publishes/subscribes per the .cse2j map. See
    /// <see cref="PanelJoins"/> for the verbatim cse2j-derived constants.
    ///
    /// Pattern:
    ///   var p = new PanelDispatcher(_tswPrimary, _tswSecondary);
    ///   p.OnBool(PanelJoins.BoolOut.MicLavMute, isMuted => _audio.SetMicMute(...));
    ///   p.OnUShort(PanelJoins.UShortOut.CameraSelect, v => _cameras.Select(v));
    ///   p.WriteBool(PanelJoins.BoolIn.SystemPowerFb, true);
    ///   p.PulseBool(PanelJoins.BoolIn.MicLavMuteFb);  // momentary
    ///
    /// Both panels (TS-1070 + TSW-1070) are duplicates and receive the same
    /// writes; output handlers fire once per panel publish — we dedupe by
    /// value+lastSeen so a button press on one panel doesn't fire the handler
    /// twice when the other panel mirrors the same value back.
    /// </summary>
    public class PanelDispatcher
    {
        private readonly BasicTriListWithSmartObject[] _panels;
        private readonly Dictionary<uint, Action<bool>>   _boolHandlers   = new Dictionary<uint, Action<bool>>();
        private readonly Dictionary<uint, Action<ushort>> _ushortHandlers = new Dictionary<uint, Action<ushort>>();
        private readonly Dictionary<uint, bool>   _lastBool   = new Dictionary<uint, bool>();
        private readonly Dictionary<uint, ushort> _lastUShort = new Dictionary<uint, ushort>();
        private readonly CCriticalSection _lock = new CCriticalSection();

        public PanelDispatcher(params BasicTriListWithSmartObject[] panels)
        {
            _panels = panels ?? new BasicTriListWithSmartObject[0];
        }

        /// <summary>
        /// Hook SmartObject 1 on each panel. Must be called AFTER the Contract
        /// constructor (which is what materializes SmartObjects[1] via
        /// <c>AddDevice → ComponentMediator.HookSmartObjectEvents</c>).
        /// </summary>
        public void Start()
        {
            foreach (var panel in _panels) {
                if (panel == null) continue;
                try {
                    var so = panel.SmartObjects[PanelJoins.SmartObjectId];
                    if (so == null) {
                        ErrorLog.Warn("PanelDispatcher: SmartObject {0} null on panel IPID 0x{1:X2}",
                            PanelJoins.SmartObjectId, panel.ID);
                        continue;
                    }
                    so.SigChange += OnSmartObjectSigChange;
                    ErrorLog.Notice("PanelDispatcher: hooked SmartObject {0} on panel IPID 0x{1:X2}",
                        PanelJoins.SmartObjectId, panel.ID);
                } catch (Exception ex) {
                    ErrorLog.Warn("PanelDispatcher: hook failed on panel IPID 0x{0:X2}: {1}",
                        panel.ID, ex.Message);
                }
            }
        }

        public void OnBool(uint join, Action<bool> handler)
        {
            if (handler == null) return;
            _lock.Enter();
            try { _boolHandlers[join] = handler; }
            finally { _lock.Leave(); }
        }

        public void OnUShort(uint join, Action<ushort> handler)
        {
            if (handler == null) return;
            _lock.Enter();
            try { _ushortHandlers[join] = handler; }
            finally { _lock.Leave(); }
        }

        public void WriteBool(uint join, bool value)
        {
            foreach (var panel in _panels) {
                if (panel == null) continue;
                try {
                    var so = panel.SmartObjects[PanelJoins.SmartObjectId];
                    if (so == null) continue;
                    so.BooleanInput[join].BoolValue = value;
                } catch (Exception ex) {
                    ErrorLog.Warn("PanelDispatcher.WriteBool join={0}: {1}", join, ex.Message);
                }
            }
        }

        public void WriteUShort(uint join, ushort value)
        {
            foreach (var panel in _panels) {
                if (panel == null) continue;
                try {
                    var so = panel.SmartObjects[PanelJoins.SmartObjectId];
                    if (so == null) continue;
                    so.UShortInput[join].UShortValue = value;
                } catch (Exception ex) {
                    ErrorLog.Warn("PanelDispatcher.WriteUShort join={0}: {1}", join, ex.Message);
                }
            }
        }

        /// <summary>
        /// 100ms momentary pulse. Use for SIMPL→panel command echoes that the
        /// panel UI treats as transient (button-pressed feedback that should
        /// snap back to off).
        /// </summary>
        public void PulseBool(uint join)
        {
            WriteBool(join, true);
            new CTimer(_ => WriteBool(join, false), 100);
        }

        private void OnSmartObjectSigChange(GenericBase dev, SmartObjectEventArgs args)
        {
            if (args == null || args.Sig == null) return;
            try {
                if (args.Sig.Type == eSigType.Bool) {
                    var v = args.Sig.BoolValue;
                    Action<bool> handler;
                    bool dispatched;
                    _lock.Enter();
                    try {
                        // Dedupe — both panels mirror state so the same value
                        // can echo back. We only fire on actual change.
                        if (_lastBool.TryGetValue(args.Sig.Number, out bool prev) && prev == v) return;
                        _lastBool[args.Sig.Number] = v;
                        dispatched = _boolHandlers.TryGetValue(args.Sig.Number, out handler);
                    } finally { _lock.Leave(); }
                    ErrorLog.Notice("PanelDispatcher: bool join={0} val={1} dispatched={2}",
                        args.Sig.Number, v, dispatched);
                    DebugTrace.SigChange("panel", FriendlyBoolName(args.Sig.Number), "bool", v);
                    handler?.Invoke(v);
                } else if (args.Sig.Type == eSigType.UShort) {
                    var v = args.Sig.UShortValue;
                    Action<ushort> handler;
                    bool dispatched;
                    _lock.Enter();
                    try {
                        if (_lastUShort.TryGetValue(args.Sig.Number, out ushort prev) && prev == v) return;
                        _lastUShort[args.Sig.Number] = v;
                        dispatched = _ushortHandlers.TryGetValue(args.Sig.Number, out handler);
                    } finally { _lock.Leave(); }
                    ErrorLog.Notice("PanelDispatcher: ushort join={0} val={1} dispatched={2}",
                        args.Sig.Number, v, dispatched);
                    DebugTrace.SigChange("panel", FriendlyUShortName(args.Sig.Number), "ushort", (int)v);
                    handler?.Invoke(v);
                }
            } catch (Exception ex) {
                ErrorLog.Warn("PanelDispatcher: handler threw on join {0}: {1}",
                    args.Sig.Number, ex.Message);
            }
        }

        // Friendly names for the debug-panel live log. Kept in sync with the
        // cse2j states maps mirrored in PanelJoins.{BoolOut,UShortOut}.
        private static string FriendlyBoolName(uint join)
        {
            switch (join) {
                case PanelJoins.BoolOut.DisplayPower:    return "DisplayPower";
                case PanelJoins.BoolOut.D1MirrorToD3:    return "D1MirrorToD3";
                case PanelJoins.BoolOut.D2MirrorToD3:    return "D2MirrorToD3";
                case PanelJoins.BoolOut.VolumeUp:        return "VolumeUp";
                case PanelJoins.BoolOut.VolumeDown:      return "VolumeDown";
                case PanelJoins.BoolOut.MuteAll:         return "MuteAll";
                case PanelJoins.BoolOut.MicLavMute:      return "MicLavMute";
                case PanelJoins.BoolOut.MicHandheldMute: return "MicHandheldMute";
                case PanelJoins.BoolOut.PtzUp:           return "PtzUp";
                case PanelJoins.BoolOut.PtzDown:         return "PtzDown";
                case PanelJoins.BoolOut.PtzLeft:         return "PtzLeft";
                case PanelJoins.BoolOut.PtzRight:        return "PtzRight";
                case PanelJoins.BoolOut.CamSendToVtc:    return "CamSendToVtc";
                case PanelJoins.BoolOut.ZoomIn:          return "ZoomIn";
                case PanelJoins.BoolOut.ZoomOut:         return "ZoomOut";
                case PanelJoins.BoolOut.MicCeiling1Mute: return "MicCeiling1Mute";
                case PanelJoins.BoolOut.MicCeiling2Mute: return "MicCeiling2Mute";
                case PanelJoins.BoolOut.MicCeiling3Mute: return "MicCeiling3Mute";
                default: return "bool/" + join;
            }
        }

        private static string FriendlyUShortName(uint join)
        {
            switch (join) {
                case PanelJoins.UShortOut.Display1Source:    return "Display1Source";
                case PanelJoins.UShortOut.Display2Source:    return "Display2Source";
                case PanelJoins.UShortOut.Display3Source:    return "Display3Source";
                case PanelJoins.UShortOut.AudioOutputSelect: return "AudioOutputSelect";
                case PanelJoins.UShortOut.CameraSelect:      return "CameraSelect";
                case PanelJoins.UShortOut.ShotPresetRecall:  return "ShotPresetRecall";
                case PanelJoins.UShortOut.ShotPresetSave:    return "ShotPresetSave";
                case PanelJoins.UShortOut.ShotPresetDelete:  return "ShotPresetDelete";
                case PanelJoins.UShortOut.CamTrackingMode:   return "CamTrackingMode";
                case PanelJoins.UShortOut.MicLavTrim:        return "MicLavTrim";
                case PanelJoins.UShortOut.MicHandheldTrim:   return "MicHandheldTrim";
                case PanelJoins.UShortOut.MicCeiling1Trim:   return "MicCeiling1Trim";
                case PanelJoins.UShortOut.MicCeiling2Trim:   return "MicCeiling2Trim";
                case PanelJoins.UShortOut.MicCeiling3Trim:   return "MicCeiling3Trim";
                case PanelJoins.UShortOut.MicLavLineOut:     return "MicLavLineOut";
                case PanelJoins.UShortOut.MicHandheldLineOut:return "MicHandheldLineOut";
                case PanelJoins.UShortOut.MicCeiling1LineOut:return "MicCeiling1LineOut";
                case PanelJoins.UShortOut.MicCeiling2LineOut:return "MicCeiling2LineOut";
                case PanelJoins.UShortOut.MicCeiling3LineOut:return "MicCeiling3LineOut";
                default: return "ushort/" + join;
            }
        }
    }
}
