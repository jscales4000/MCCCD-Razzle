using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace MCCCD_AA140
{
    /// <summary>
    /// Digital feedback - true when processor sees this panel
    /// </summary>
    /// <summary>
    /// Digital feedback - lav mic muted state
    /// </summary>
    /// <summary>
    /// Digital feedback - handheld mic muted state
    /// </summary>
    /// <summary>
    /// Digital feedback - true when system is ON (drives power-button enlarged variant)
    /// </summary>
    /// <summary>
    /// Digital feedback - D1 actually powered on (NVX D200 sink-connected)
    /// </summary>
    /// <summary>
    /// Digital feedback - D2 actually powered on
    /// </summary>
    /// <summary>
    /// Digital feedback - D3 actually powered on
    /// </summary>
    /// <summary>
    /// Digital feedback - ceiling 1 muted state
    /// </summary>
    /// <summary>
    /// Digital feedback - ceiling 2 muted state
    /// </summary>
    /// <summary>
    /// Digital feedback - ceiling 3 muted state
    /// </summary>
    /// <summary>
    /// Digital feedback - lav signal-present (mic detected)
    /// </summary>
    /// <summary>
    /// Digital feedback - handheld signal-present
    /// </summary>
    /// <summary>
    /// Digital feedback - ceiling 1 signal-present
    /// </summary>
    /// <summary>
    /// Digital feedback - ceiling 2 signal-present
    /// </summary>
    /// <summary>
    /// Digital feedback - ceiling 3 signal-present
    /// </summary>
    /// <summary>
    /// Analog feedback - D1 active source
    /// </summary>
    /// <summary>
    /// Analog feedback - D2 active source
    /// </summary>
    /// <summary>
    /// Analog feedback - D3 active source
    /// </summary>
    /// <summary>
    /// Analog feedback - 1=D1 owns audio, 2=D2 owns audio
    /// </summary>
    /// <summary>
    /// Analog feedback - current tracking mode (1/2/3)
    /// </summary>
    /// <summary>
    /// Analog feedback - 0=vacant, 1=occupied, 2=shutdown-pending
    /// </summary>
    /// <summary>
    /// Analog feedback - minutes remaining until system-off
    /// </summary>
    /// <summary>
    /// Analog feedback - lav input gain trim
    /// </summary>
    /// <summary>
    /// Analog feedback - handheld input gain trim
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 1 input gain trim
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 2 input gain trim
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 3 input gain trim
    /// </summary>
    /// <summary>
    /// Analog feedback - lav line-out level
    /// </summary>
    /// <summary>
    /// Analog feedback - handheld line-out level
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 1 line-out level
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 2 line-out level
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 3 line-out level
    /// </summary>
    /// <summary>
    /// Analog feedback - lav real-time level 0-100 (~10-30 Hz)
    /// </summary>
    /// <summary>
    /// Analog feedback - handheld real-time level 0-100
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 1 real-time level 0-100
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 2 real-time level 0-100
    /// </summary>
    /// <summary>
    /// Analog feedback - ceiling 3 real-time level 0-100
    /// </summary>
    /// <summary>
    /// Digital pulse - toggle system on/off
    /// </summary>
    /// <summary>
    /// Digital pulse - one-shot push of D1 source to D3
    /// </summary>
    /// <summary>
    /// Digital pulse - one-shot push of D2 source to D3
    /// </summary>
    /// <summary>
    /// Digital pulse - master program volume up
    /// </summary>
    /// <summary>
    /// Digital pulse - master program volume down
    /// </summary>
    /// <summary>
    /// Digital pulse - toggle master program mute
    /// </summary>
    /// <summary>
    /// Digital level - lav mic mute on/off
    /// </summary>
    /// <summary>
    /// Digital level - handheld mic mute on/off
    /// </summary>
    /// <summary>
    /// Digital level - press-and-hold pan/tilt up
    /// </summary>
    /// <summary>
    /// Digital level - press-and-hold pan/tilt down
    /// </summary>
    /// <summary>
    /// Digital level - press-and-hold pan left
    /// </summary>
    /// <summary>
    /// Digital level - press-and-hold pan right
    /// </summary>
    /// <summary>
    /// Digital pulse - set active camera as VTC ingest source
    /// </summary>
    /// <summary>
    /// Digital level - press-and-hold zoom in on active camera
    /// </summary>
    /// <summary>
    /// Digital level - press-and-hold zoom out
    /// </summary>
    /// <summary>
    /// Digital level - TCCM ceiling 1 mute (settings only)
    /// </summary>
    /// <summary>
    /// Digital level - TCCM ceiling 2 mute
    /// </summary>
    /// <summary>
    /// Digital level - TCCM ceiling 3 mute
    /// </summary>
    /// <summary>
    /// Analog set - D1 source (1=RoomPC,2=ExtPC,3=AirMedia,4=Laptop)
    /// </summary>
    /// <summary>
    /// Analog set - D2 source
    /// </summary>
    /// <summary>
    /// Analog set - D3 source (independent after boot init)
    /// </summary>
    /// <summary>
    /// Analog set - 1=D1 owns audio, 2=D2 owns audio (D3 never)
    /// </summary>
    /// <summary>
    /// Analog set - 1=Front i20, 2=BackL i12, 3=BackR i12
    /// </summary>
    /// <summary>
    /// Analog set - recall preset 1/2/3 on active camera (tap)
    /// </summary>
    /// <summary>
    /// Analog set - save preset 1/2/3 on active camera (3s hold)
    /// </summary>
    /// <summary>
    /// Analog set - delete preset (deferred to v2)
    /// </summary>
    /// <summary>
    /// Analog set - 1=People, 2=Group, 3=VX AutoSwitch
    /// </summary>
    /// <summary>
    /// Analog set - lav input gain trim 0-100
    /// </summary>
    /// <summary>
    /// Analog set - handheld input gain trim 0-100
    /// </summary>
    /// <summary>
    /// Analog set - ceiling 1 input gain trim 0-100
    /// </summary>
    /// <summary>
    /// Analog set - ceiling 2 input gain trim 0-100
    /// </summary>
    /// <summary>
    /// Analog set - ceiling 3 input gain trim 0-100
    /// </summary>
    /// <summary>
    /// Analog set - lav line-out level 0-100
    /// </summary>
    /// <summary>
    /// Analog set - handheld line-out level 0-100
    /// </summary>
    /// <summary>
    /// Analog set - ceiling 1 line-out level 0-100
    /// </summary>
    /// <summary>
    /// Analog set - ceiling 2 line-out level 0-100
    /// </summary>
    /// <summary>
    /// Analog set - ceiling 3 line-out level 0-100
    /// </summary>
    public interface IMain
    {
        object UserObject { get; set; }

        event EventHandler<UIEventArgs> PanelOnline;
        event EventHandler<UIEventArgs> MicLavMuteFb;
        event EventHandler<UIEventArgs> MicHandheldMuteFb;
        event EventHandler<UIEventArgs> SystemPowerFb;
        event EventHandler<UIEventArgs> Display1PowerFb;
        event EventHandler<UIEventArgs> Display2PowerFb;
        event EventHandler<UIEventArgs> Display3PowerFb;
        event EventHandler<UIEventArgs> MicCeiling1MuteFb;
        event EventHandler<UIEventArgs> MicCeiling2MuteFb;
        event EventHandler<UIEventArgs> MicCeiling3MuteFb;
        event EventHandler<UIEventArgs> MicLavConnected;
        event EventHandler<UIEventArgs> MicHandheldConnected;
        event EventHandler<UIEventArgs> MicCeiling1Connected;
        event EventHandler<UIEventArgs> MicCeiling2Connected;
        event EventHandler<UIEventArgs> MicCeiling3Connected;
        event EventHandler<UIEventArgs> Display1SourceFb;
        event EventHandler<UIEventArgs> Display2SourceFb;
        event EventHandler<UIEventArgs> Display3SourceFb;
        event EventHandler<UIEventArgs> AudioOutputSelectFb;
        event EventHandler<UIEventArgs> CamTrackingModeFb;
        event EventHandler<UIEventArgs> OccupancyState;
        event EventHandler<UIEventArgs> ShutdownCountdown;
        event EventHandler<UIEventArgs> MicLavTrimFb;
        event EventHandler<UIEventArgs> MicHandheldTrimFb;
        event EventHandler<UIEventArgs> MicCeiling1TrimFb;
        event EventHandler<UIEventArgs> MicCeiling2TrimFb;
        event EventHandler<UIEventArgs> MicCeiling3TrimFb;
        event EventHandler<UIEventArgs> MicLavLineOutFb;
        event EventHandler<UIEventArgs> MicHandheldLineOutFb;
        event EventHandler<UIEventArgs> MicCeiling1LineOutFb;
        event EventHandler<UIEventArgs> MicCeiling2LineOutFb;
        event EventHandler<UIEventArgs> MicCeiling3LineOutFb;
        event EventHandler<UIEventArgs> MicLavLevel;
        event EventHandler<UIEventArgs> MicHandheldLevel;
        event EventHandler<UIEventArgs> MicCeiling1Level;
        event EventHandler<UIEventArgs> MicCeiling2Level;
        event EventHandler<UIEventArgs> MicCeiling3Level;

        void DisplayPower(MainBoolInputSigDelegate callback);
        void D1MirrorToD3(MainBoolInputSigDelegate callback);
        void D2MirrorToD3(MainBoolInputSigDelegate callback);
        void VolumeUp(MainBoolInputSigDelegate callback);
        void VolumeDown(MainBoolInputSigDelegate callback);
        void MuteAll(MainBoolInputSigDelegate callback);
        void MicLavMute(MainBoolInputSigDelegate callback);
        void MicHandheldMute(MainBoolInputSigDelegate callback);
        void PtzUp(MainBoolInputSigDelegate callback);
        void PtzDown(MainBoolInputSigDelegate callback);
        void PtzLeft(MainBoolInputSigDelegate callback);
        void PtzRight(MainBoolInputSigDelegate callback);
        void CamSendToVtc(MainBoolInputSigDelegate callback);
        void ZoomIn(MainBoolInputSigDelegate callback);
        void ZoomOut(MainBoolInputSigDelegate callback);
        void MicCeiling1Mute(MainBoolInputSigDelegate callback);
        void MicCeiling2Mute(MainBoolInputSigDelegate callback);
        void MicCeiling3Mute(MainBoolInputSigDelegate callback);
        void Display1Source(MainUShortInputSigDelegate callback);
        void Display2Source(MainUShortInputSigDelegate callback);
        void Display3Source(MainUShortInputSigDelegate callback);
        void AudioOutputSelect(MainUShortInputSigDelegate callback);
        void CameraSelect(MainUShortInputSigDelegate callback);
        void ShotPresetRecall(MainUShortInputSigDelegate callback);
        void ShotPresetSave(MainUShortInputSigDelegate callback);
        void ShotPresetDelete(MainUShortInputSigDelegate callback);
        void CamTrackingMode(MainUShortInputSigDelegate callback);
        void MicLavTrim(MainUShortInputSigDelegate callback);
        void MicHandheldTrim(MainUShortInputSigDelegate callback);
        void MicCeiling1Trim(MainUShortInputSigDelegate callback);
        void MicCeiling2Trim(MainUShortInputSigDelegate callback);
        void MicCeiling3Trim(MainUShortInputSigDelegate callback);
        void MicLavLineOut(MainUShortInputSigDelegate callback);
        void MicHandheldLineOut(MainUShortInputSigDelegate callback);
        void MicCeiling1LineOut(MainUShortInputSigDelegate callback);
        void MicCeiling2LineOut(MainUShortInputSigDelegate callback);
        void MicCeiling3LineOut(MainUShortInputSigDelegate callback);

    }

    public delegate void MainBoolInputSigDelegate(BoolInputSig boolInputSig, IMain main);
    public delegate void MainUShortInputSigDelegate(UShortInputSig uShortInputSig, IMain main);

    internal class Main : IMain, IDisposable
    {
        #region Standard CH5 Component members

        private ComponentMediator ComponentMediator { get; set; }

        public object UserObject { get; set; }

        public uint ControlJoinId { get; private set; }

        private IList<BasicTriListWithSmartObject> _devices;
        public IList<BasicTriListWithSmartObject> Devices { get { return _devices; } }

        #endregion

        #region Joins

        private static class Joins
        {
            internal static class Booleans
            {
                public const uint PanelOnline = 1;
                public const uint MicLavMuteFb = 2;
                public const uint MicHandheldMuteFb = 3;
                public const uint SystemPowerFb = 4;
                public const uint Display1PowerFb = 5;
                public const uint Display2PowerFb = 6;
                public const uint Display3PowerFb = 7;
                public const uint MicCeiling1MuteFb = 8;
                public const uint MicCeiling2MuteFb = 9;
                public const uint MicCeiling3MuteFb = 10;
                public const uint MicLavConnected = 11;
                public const uint MicHandheldConnected = 12;
                public const uint MicCeiling1Connected = 13;
                public const uint MicCeiling2Connected = 14;
                public const uint MicCeiling3Connected = 15;

                public const uint DisplayPower = 1;
                public const uint D1MirrorToD3 = 2;
                public const uint D2MirrorToD3 = 3;
                public const uint VolumeUp = 4;
                public const uint VolumeDown = 5;
                public const uint MuteAll = 6;
                public const uint MicLavMute = 7;
                public const uint MicHandheldMute = 8;
                public const uint PtzUp = 9;
                public const uint PtzDown = 10;
                public const uint PtzLeft = 11;
                public const uint PtzRight = 12;
                public const uint CamSendToVtc = 13;
                public const uint ZoomIn = 14;
                public const uint ZoomOut = 15;
                public const uint MicCeiling1Mute = 16;
                public const uint MicCeiling2Mute = 17;
                public const uint MicCeiling3Mute = 18;
            }
            internal static class Numerics
            {
                public const uint Display1SourceFb = 1;
                public const uint Display2SourceFb = 2;
                public const uint Display3SourceFb = 3;
                public const uint AudioOutputSelectFb = 4;
                public const uint CamTrackingModeFb = 5;
                public const uint OccupancyState = 6;
                public const uint ShutdownCountdown = 7;
                public const uint MicLavTrimFb = 8;
                public const uint MicHandheldTrimFb = 9;
                public const uint MicCeiling1TrimFb = 10;
                public const uint MicCeiling2TrimFb = 11;
                public const uint MicCeiling3TrimFb = 12;
                public const uint MicLavLineOutFb = 13;
                public const uint MicHandheldLineOutFb = 14;
                public const uint MicCeiling1LineOutFb = 15;
                public const uint MicCeiling2LineOutFb = 16;
                public const uint MicCeiling3LineOutFb = 17;
                public const uint MicLavLevel = 18;
                public const uint MicHandheldLevel = 19;
                public const uint MicCeiling1Level = 20;
                public const uint MicCeiling2Level = 21;
                public const uint MicCeiling3Level = 22;

                public const uint Display1Source = 1;
                public const uint Display2Source = 2;
                public const uint Display3Source = 3;
                public const uint AudioOutputSelect = 4;
                public const uint CameraSelect = 5;
                public const uint ShotPresetRecall = 6;
                public const uint ShotPresetSave = 7;
                public const uint ShotPresetDelete = 8;
                public const uint CamTrackingMode = 9;
                public const uint MicLavTrim = 10;
                public const uint MicHandheldTrim = 11;
                public const uint MicCeiling1Trim = 12;
                public const uint MicCeiling2Trim = 13;
                public const uint MicCeiling3Trim = 14;
                public const uint MicLavLineOut = 15;
                public const uint MicHandheldLineOut = 16;
                public const uint MicCeiling1LineOut = 17;
                public const uint MicCeiling2LineOut = 18;
                public const uint MicCeiling3LineOut = 19;
            }
        }

        #endregion

        #region Construction and Initialization

        internal Main(ComponentMediator componentMediator, uint controlJoinId)
        {
            ComponentMediator = componentMediator;
            Initialize(controlJoinId);
        }

        private void Initialize(uint controlJoinId)
        {
            ControlJoinId = controlJoinId; 
 
            _devices = new List<BasicTriListWithSmartObject>(); 
 
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.PanelOnline, onPanelOnline);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicLavMuteFb, onMicLavMuteFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicHandheldMuteFb, onMicHandheldMuteFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.SystemPowerFb, onSystemPowerFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.Display1PowerFb, onDisplay1PowerFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.Display2PowerFb, onDisplay2PowerFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.Display3PowerFb, onDisplay3PowerFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling1MuteFb, onMicCeiling1MuteFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling2MuteFb, onMicCeiling2MuteFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling3MuteFb, onMicCeiling3MuteFb);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicLavConnected, onMicLavConnected);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicHandheldConnected, onMicHandheldConnected);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling1Connected, onMicCeiling1Connected);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling2Connected, onMicCeiling2Connected);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling3Connected, onMicCeiling3Connected);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Display1SourceFb, onDisplay1SourceFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Display2SourceFb, onDisplay2SourceFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Display3SourceFb, onDisplay3SourceFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.AudioOutputSelectFb, onAudioOutputSelectFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.CamTrackingModeFb, onCamTrackingModeFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.OccupancyState, onOccupancyState);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.ShutdownCountdown, onShutdownCountdown);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicLavTrimFb, onMicLavTrimFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicHandheldTrimFb, onMicHandheldTrimFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling1TrimFb, onMicCeiling1TrimFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling2TrimFb, onMicCeiling2TrimFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling3TrimFb, onMicCeiling3TrimFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicLavLineOutFb, onMicLavLineOutFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicHandheldLineOutFb, onMicHandheldLineOutFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling1LineOutFb, onMicCeiling1LineOutFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling2LineOutFb, onMicCeiling2LineOutFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling3LineOutFb, onMicCeiling3LineOutFb);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicLavLevel, onMicLavLevel);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicHandheldLevel, onMicHandheldLevel);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling1Level, onMicCeiling1Level);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling2Level, onMicCeiling2Level);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling3Level, onMicCeiling3Level);

        }

        public void AddDevice(BasicTriListWithSmartObject device)
        {
            Devices.Add(device);
            ComponentMediator.HookSmartObjectEvents(device.SmartObjects[ControlJoinId]);
        }

        public void RemoveDevice(BasicTriListWithSmartObject device)
        {
            Devices.Remove(device);
            ComponentMediator.UnHookSmartObjectEvents(device.SmartObjects[ControlJoinId]);
        }

        #endregion

        #region CH5 Contract

        public event EventHandler<UIEventArgs> PanelOnline;
        private void onPanelOnline(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = PanelOnline;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicLavMuteFb;
        private void onMicLavMuteFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicLavMuteFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicHandheldMuteFb;
        private void onMicHandheldMuteFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicHandheldMuteFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> SystemPowerFb;
        private void onSystemPowerFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = SystemPowerFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display1PowerFb;
        private void onDisplay1PowerFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display1PowerFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display2PowerFb;
        private void onDisplay2PowerFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display2PowerFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display3PowerFb;
        private void onDisplay3PowerFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display3PowerFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling1MuteFb;
        private void onMicCeiling1MuteFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling1MuteFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling2MuteFb;
        private void onMicCeiling2MuteFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling2MuteFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling3MuteFb;
        private void onMicCeiling3MuteFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling3MuteFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicLavConnected;
        private void onMicLavConnected(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicLavConnected;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicHandheldConnected;
        private void onMicHandheldConnected(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicHandheldConnected;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling1Connected;
        private void onMicCeiling1Connected(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling1Connected;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling2Connected;
        private void onMicCeiling2Connected(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling2Connected;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling3Connected;
        private void onMicCeiling3Connected(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling3Connected;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void DisplayPower(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.DisplayPower], this);
            }
        }

        public void D1MirrorToD3(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.D1MirrorToD3], this);
            }
        }

        public void D2MirrorToD3(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.D2MirrorToD3], this);
            }
        }

        public void VolumeUp(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.VolumeUp], this);
            }
        }

        public void VolumeDown(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.VolumeDown], this);
            }
        }

        public void MuteAll(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MuteAll], this);
            }
        }

        public void MicLavMute(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicLavMute], this);
            }
        }

        public void MicHandheldMute(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicHandheldMute], this);
            }
        }

        public void PtzUp(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.PtzUp], this);
            }
        }

        public void PtzDown(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.PtzDown], this);
            }
        }

        public void PtzLeft(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.PtzLeft], this);
            }
        }

        public void PtzRight(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.PtzRight], this);
            }
        }

        public void CamSendToVtc(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.CamSendToVtc], this);
            }
        }

        public void ZoomIn(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.ZoomIn], this);
            }
        }

        public void ZoomOut(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.ZoomOut], this);
            }
        }

        public void MicCeiling1Mute(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling1Mute], this);
            }
        }

        public void MicCeiling2Mute(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling2Mute], this);
            }
        }

        public void MicCeiling3Mute(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling3Mute], this);
            }
        }

        public event EventHandler<UIEventArgs> Display1SourceFb;
        private void onDisplay1SourceFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display1SourceFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display2SourceFb;
        private void onDisplay2SourceFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display2SourceFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display3SourceFb;
        private void onDisplay3SourceFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display3SourceFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> AudioOutputSelectFb;
        private void onAudioOutputSelectFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = AudioOutputSelectFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> CamTrackingModeFb;
        private void onCamTrackingModeFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CamTrackingModeFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> OccupancyState;
        private void onOccupancyState(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = OccupancyState;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> ShutdownCountdown;
        private void onShutdownCountdown(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = ShutdownCountdown;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicLavTrimFb;
        private void onMicLavTrimFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicLavTrimFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicHandheldTrimFb;
        private void onMicHandheldTrimFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicHandheldTrimFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling1TrimFb;
        private void onMicCeiling1TrimFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling1TrimFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling2TrimFb;
        private void onMicCeiling2TrimFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling2TrimFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling3TrimFb;
        private void onMicCeiling3TrimFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling3TrimFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicLavLineOutFb;
        private void onMicLavLineOutFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicLavLineOutFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicHandheldLineOutFb;
        private void onMicHandheldLineOutFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicHandheldLineOutFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling1LineOutFb;
        private void onMicCeiling1LineOutFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling1LineOutFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling2LineOutFb;
        private void onMicCeiling2LineOutFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling2LineOutFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling3LineOutFb;
        private void onMicCeiling3LineOutFb(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling3LineOutFb;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicLavLevel;
        private void onMicLavLevel(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicLavLevel;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicHandheldLevel;
        private void onMicHandheldLevel(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicHandheldLevel;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling1Level;
        private void onMicCeiling1Level(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling1Level;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling2Level;
        private void onMicCeiling2Level(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling2Level;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling3Level;
        private void onMicCeiling3Level(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling3Level;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void Display1Source(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Display1Source], this);
            }
        }

        public void Display2Source(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Display2Source], this);
            }
        }

        public void Display3Source(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Display3Source], this);
            }
        }

        public void AudioOutputSelect(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.AudioOutputSelect], this);
            }
        }

        public void CameraSelect(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.CameraSelect], this);
            }
        }

        public void ShotPresetRecall(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.ShotPresetRecall], this);
            }
        }

        public void ShotPresetSave(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.ShotPresetSave], this);
            }
        }

        public void ShotPresetDelete(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.ShotPresetDelete], this);
            }
        }

        public void CamTrackingMode(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.CamTrackingMode], this);
            }
        }

        public void MicLavTrim(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicLavTrim], this);
            }
        }

        public void MicHandheldTrim(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicHandheldTrim], this);
            }
        }

        public void MicCeiling1Trim(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling1Trim], this);
            }
        }

        public void MicCeiling2Trim(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling2Trim], this);
            }
        }

        public void MicCeiling3Trim(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling3Trim], this);
            }
        }

        public void MicLavLineOut(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicLavLineOut], this);
            }
        }

        public void MicHandheldLineOut(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicHandheldLineOut], this);
            }
        }

        public void MicCeiling1LineOut(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling1LineOut], this);
            }
        }

        public void MicCeiling2LineOut(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling2LineOut], this);
            }
        }

        public void MicCeiling3LineOut(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling3LineOut], this);
            }
        }

        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return (int)ControlJoinId;
        }

        public override string ToString()
        {
            return string.Format("Contract: {0} Component: {1} HashCode: {2} {3}", "Main", GetType().Name, GetHashCode(), UserObject != null ? "UserObject: " + UserObject : null);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            PanelOnline = null;
            MicLavMuteFb = null;
            MicHandheldMuteFb = null;
            SystemPowerFb = null;
            Display1PowerFb = null;
            Display2PowerFb = null;
            Display3PowerFb = null;
            MicCeiling1MuteFb = null;
            MicCeiling2MuteFb = null;
            MicCeiling3MuteFb = null;
            MicLavConnected = null;
            MicHandheldConnected = null;
            MicCeiling1Connected = null;
            MicCeiling2Connected = null;
            MicCeiling3Connected = null;
            Display1SourceFb = null;
            Display2SourceFb = null;
            Display3SourceFb = null;
            AudioOutputSelectFb = null;
            CamTrackingModeFb = null;
            OccupancyState = null;
            ShutdownCountdown = null;
            MicLavTrimFb = null;
            MicHandheldTrimFb = null;
            MicCeiling1TrimFb = null;
            MicCeiling2TrimFb = null;
            MicCeiling3TrimFb = null;
            MicLavLineOutFb = null;
            MicHandheldLineOutFb = null;
            MicCeiling1LineOutFb = null;
            MicCeiling2LineOutFb = null;
            MicCeiling3LineOutFb = null;
            MicLavLevel = null;
            MicHandheldLevel = null;
            MicCeiling1Level = null;
            MicCeiling2Level = null;
            MicCeiling3Level = null;
        }

        #endregion

    }
}
