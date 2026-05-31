using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace MCCCD_AA140
{
    public interface IMain
    {
        object UserObject { get; set; }

        event EventHandler<UIEventArgs> CamPresenterFraming;
        event EventHandler<UIEventArgs> MicLavMute;
        event EventHandler<UIEventArgs> MicHandheldMute;
        event EventHandler<UIEventArgs> MicCeiling1Mute;
        event EventHandler<UIEventArgs> MicCeiling2Mute;
        event EventHandler<UIEventArgs> MicCeiling3Mute;
        event EventHandler<UIEventArgs> DisplayPower;
        event EventHandler<UIEventArgs> D1MirrorToD3;
        event EventHandler<UIEventArgs> D2MirrorToD3;
        event EventHandler<UIEventArgs> VolumeUp;
        event EventHandler<UIEventArgs> VolumeDown;
        event EventHandler<UIEventArgs> MuteAll;
        event EventHandler<UIEventArgs> PtzUp;
        event EventHandler<UIEventArgs> PtzDown;
        event EventHandler<UIEventArgs> PtzLeft;
        event EventHandler<UIEventArgs> PtzRight;
        event EventHandler<UIEventArgs> CamSendToVtc;
        event EventHandler<UIEventArgs> ZoomIn;
        event EventHandler<UIEventArgs> ZoomOut;
        event EventHandler<UIEventArgs> CamHomeShot;
        event EventHandler<UIEventArgs> CamTrackingShot;
        event EventHandler<UIEventArgs> Display1Source;
        event EventHandler<UIEventArgs> Display2Source;
        event EventHandler<UIEventArgs> Display3Source;
        event EventHandler<UIEventArgs> Display4Source;
        event EventHandler<UIEventArgs> Display5Source;
        event EventHandler<UIEventArgs> UsbHostSelect;
        event EventHandler<UIEventArgs> AudioOutputSelect;
        event EventHandler<UIEventArgs> CamUsbOutput;
        event EventHandler<UIEventArgs> CamPresetZone;
        event EventHandler<UIEventArgs> CamTrackingProfile;
        event EventHandler<UIEventArgs> MicLavTrim;
        event EventHandler<UIEventArgs> MicHandheldTrim;
        event EventHandler<UIEventArgs> MicCeiling1Trim;
        event EventHandler<UIEventArgs> MicCeiling2Trim;
        event EventHandler<UIEventArgs> MicCeiling3Trim;
        event EventHandler<UIEventArgs> MicLavLineOut;
        event EventHandler<UIEventArgs> MicHandheldLineOut;
        event EventHandler<UIEventArgs> MicCeiling1LineOut;
        event EventHandler<UIEventArgs> MicCeiling2LineOut;
        event EventHandler<UIEventArgs> MicCeiling3LineOut;
        event EventHandler<UIEventArgs> CameraSelect;
        event EventHandler<UIEventArgs> ShotPresetRecall;
        event EventHandler<UIEventArgs> ShotPresetSave;
        event EventHandler<UIEventArgs> ShotPresetDelete;

        void CamPresenterFramingFb(MainBoolInputSigDelegate callback);
        void MicLavMuteFb(MainBoolInputSigDelegate callback);
        void MicHandheldMuteFb(MainBoolInputSigDelegate callback);
        void MicCeiling1MuteFb(MainBoolInputSigDelegate callback);
        void MicCeiling2MuteFb(MainBoolInputSigDelegate callback);
        void MicCeiling3MuteFb(MainBoolInputSigDelegate callback);
        void PanelOnline(MainBoolInputSigDelegate callback);
        void SystemPowerFb(MainBoolInputSigDelegate callback);
        void Display1PowerFb(MainBoolInputSigDelegate callback);
        void Display2PowerFb(MainBoolInputSigDelegate callback);
        void Display3PowerFb(MainBoolInputSigDelegate callback);
        void Display4PowerFb(MainBoolInputSigDelegate callback);
        void MicLavConnected(MainBoolInputSigDelegate callback);
        void MicHandheldConnected(MainBoolInputSigDelegate callback);
        void MicCeiling1Connected(MainBoolInputSigDelegate callback);
        void MicCeiling2Connected(MainBoolInputSigDelegate callback);
        void MicCeiling3Connected(MainBoolInputSigDelegate callback);
        void RoomPcSync(MainBoolInputSigDelegate callback);
        void ExtPcSync(MainBoolInputSigDelegate callback);
        void AirMediaSync(MainBoolInputSigDelegate callback);
        void AirMediaMiracast(MainBoolInputSigDelegate callback);
        void AirMediaAirPlay(MainBoolInputSigDelegate callback);
        void AirMediaTx3(MainBoolInputSigDelegate callback);
        void LaptopHdmiSync(MainBoolInputSigDelegate callback);
        void LaptopUsbcSync(MainBoolInputSigDelegate callback);
        void Display1SourceFb(MainUShortInputSigDelegate callback);
        void Display2SourceFb(MainUShortInputSigDelegate callback);
        void Display3SourceFb(MainUShortInputSigDelegate callback);
        void Display4SourceFb(MainUShortInputSigDelegate callback);
        void Display5SourceFb(MainUShortInputSigDelegate callback);
        void UsbHostSelectFb(MainUShortInputSigDelegate callback);
        void AudioOutputSelectFb(MainUShortInputSigDelegate callback);
        void CamUsbOutputFb(MainUShortInputSigDelegate callback);
        void CamPresetZoneFb(MainUShortInputSigDelegate callback);
        void CamTrackingProfileFb(MainUShortInputSigDelegate callback);
        void MicLavTrimFb(MainUShortInputSigDelegate callback);
        void MicHandheldTrimFb(MainUShortInputSigDelegate callback);
        void MicCeiling1TrimFb(MainUShortInputSigDelegate callback);
        void MicCeiling2TrimFb(MainUShortInputSigDelegate callback);
        void MicCeiling3TrimFb(MainUShortInputSigDelegate callback);
        void MicLavLineOutFb(MainUShortInputSigDelegate callback);
        void MicHandheldLineOutFb(MainUShortInputSigDelegate callback);
        void MicCeiling1LineOutFb(MainUShortInputSigDelegate callback);
        void MicCeiling2LineOutFb(MainUShortInputSigDelegate callback);
        void MicCeiling3LineOutFb(MainUShortInputSigDelegate callback);
        void OccupancyState(MainUShortInputSigDelegate callback);
        void ShutdownCountdown(MainUShortInputSigDelegate callback);
        void MicLavLevel(MainUShortInputSigDelegate callback);
        void MicHandheldLevel(MainUShortInputSigDelegate callback);
        void MicCeiling1Level(MainUShortInputSigDelegate callback);
        void MicCeiling2Level(MainUShortInputSigDelegate callback);
        void MicCeiling3Level(MainUShortInputSigDelegate callback);
        void CamPanPos(MainUShortInputSigDelegate callback);
        void CamTiltPos(MainUShortInputSigDelegate callback);
        void CamZoomPos(MainUShortInputSigDelegate callback);

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
                public const uint CamPresenterFraming = 1;
                public const uint MicLavMute = 2;
                public const uint MicHandheldMute = 3;
                public const uint MicCeiling1Mute = 4;
                public const uint MicCeiling2Mute = 5;
                public const uint MicCeiling3Mute = 6;
                public const uint DisplayPower = 7;
                public const uint D1MirrorToD3 = 8;
                public const uint D2MirrorToD3 = 9;
                public const uint VolumeUp = 10;
                public const uint VolumeDown = 11;
                public const uint MuteAll = 12;
                public const uint PtzUp = 13;
                public const uint PtzDown = 14;
                public const uint PtzLeft = 15;
                public const uint PtzRight = 16;
                public const uint CamSendToVtc = 17;
                public const uint ZoomIn = 18;
                public const uint ZoomOut = 19;
                public const uint CamHomeShot = 20;
                public const uint CamTrackingShot = 21;

                public const uint CamPresenterFramingFb = 1;
                public const uint MicLavMuteFb = 2;
                public const uint MicHandheldMuteFb = 3;
                public const uint MicCeiling1MuteFb = 4;
                public const uint MicCeiling2MuteFb = 5;
                public const uint MicCeiling3MuteFb = 6;
                public const uint PanelOnline = 22;
                public const uint SystemPowerFb = 23;
                public const uint Display1PowerFb = 24;
                public const uint Display2PowerFb = 25;
                public const uint Display3PowerFb = 26;
                public const uint Display4PowerFb = 27;
                public const uint MicLavConnected = 28;
                public const uint MicHandheldConnected = 29;
                public const uint MicCeiling1Connected = 30;
                public const uint MicCeiling2Connected = 31;
                public const uint MicCeiling3Connected = 32;
                public const uint RoomPcSync = 33;
                public const uint ExtPcSync = 34;
                public const uint AirMediaSync = 35;
                public const uint AirMediaMiracast = 36;
                public const uint AirMediaAirPlay = 37;
                public const uint AirMediaTx3 = 38;
                public const uint LaptopHdmiSync = 39;
                public const uint LaptopUsbcSync = 40;
            }
            internal static class Numerics
            {
                public const uint Display1Source = 1;
                public const uint Display2Source = 2;
                public const uint Display3Source = 3;
                public const uint Display4Source = 4;
                public const uint Display5Source = 5;
                public const uint UsbHostSelect = 6;
                public const uint AudioOutputSelect = 7;
                public const uint CamUsbOutput = 8;
                public const uint CamPresetZone = 9;
                public const uint CamTrackingProfile = 10;
                public const uint MicLavTrim = 11;
                public const uint MicHandheldTrim = 12;
                public const uint MicCeiling1Trim = 13;
                public const uint MicCeiling2Trim = 14;
                public const uint MicCeiling3Trim = 15;
                public const uint MicLavLineOut = 16;
                public const uint MicHandheldLineOut = 17;
                public const uint MicCeiling1LineOut = 18;
                public const uint MicCeiling2LineOut = 19;
                public const uint MicCeiling3LineOut = 20;
                public const uint CameraSelect = 21;
                public const uint ShotPresetRecall = 22;
                public const uint ShotPresetSave = 23;
                public const uint ShotPresetDelete = 24;

                public const uint Display1SourceFb = 1;
                public const uint Display2SourceFb = 2;
                public const uint Display3SourceFb = 3;
                public const uint Display4SourceFb = 4;
                public const uint Display5SourceFb = 5;
                public const uint UsbHostSelectFb = 6;
                public const uint AudioOutputSelectFb = 7;
                public const uint CamUsbOutputFb = 8;
                public const uint CamPresetZoneFb = 9;
                public const uint CamTrackingProfileFb = 10;
                public const uint MicLavTrimFb = 11;
                public const uint MicHandheldTrimFb = 12;
                public const uint MicCeiling1TrimFb = 13;
                public const uint MicCeiling2TrimFb = 14;
                public const uint MicCeiling3TrimFb = 15;
                public const uint MicLavLineOutFb = 16;
                public const uint MicHandheldLineOutFb = 17;
                public const uint MicCeiling1LineOutFb = 18;
                public const uint MicCeiling2LineOutFb = 19;
                public const uint MicCeiling3LineOutFb = 20;
                public const uint OccupancyState = 25;
                public const uint ShutdownCountdown = 26;
                public const uint MicLavLevel = 27;
                public const uint MicHandheldLevel = 28;
                public const uint MicCeiling1Level = 29;
                public const uint MicCeiling2Level = 30;
                public const uint MicCeiling3Level = 31;
                public const uint CamPanPos = 32;
                public const uint CamTiltPos = 33;
                public const uint CamZoomPos = 34;
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
 
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.CamPresenterFraming, onCamPresenterFraming);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicLavMute, onMicLavMute);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicHandheldMute, onMicHandheldMute);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling1Mute, onMicCeiling1Mute);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling2Mute, onMicCeiling2Mute);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MicCeiling3Mute, onMicCeiling3Mute);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.DisplayPower, onDisplayPower);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.D1MirrorToD3, onD1MirrorToD3);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.D2MirrorToD3, onD2MirrorToD3);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.VolumeUp, onVolumeUp);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.VolumeDown, onVolumeDown);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.MuteAll, onMuteAll);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.PtzUp, onPtzUp);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.PtzDown, onPtzDown);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.PtzLeft, onPtzLeft);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.PtzRight, onPtzRight);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.CamSendToVtc, onCamSendToVtc);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.ZoomIn, onZoomIn);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.ZoomOut, onZoomOut);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.CamHomeShot, onCamHomeShot);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.CamTrackingShot, onCamTrackingShot);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Display1Source, onDisplay1Source);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Display2Source, onDisplay2Source);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Display3Source, onDisplay3Source);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Display4Source, onDisplay4Source);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Display5Source, onDisplay5Source);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.UsbHostSelect, onUsbHostSelect);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.AudioOutputSelect, onAudioOutputSelect);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.CamUsbOutput, onCamUsbOutput);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.CamPresetZone, onCamPresetZone);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.CamTrackingProfile, onCamTrackingProfile);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicLavTrim, onMicLavTrim);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicHandheldTrim, onMicHandheldTrim);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling1Trim, onMicCeiling1Trim);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling2Trim, onMicCeiling2Trim);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling3Trim, onMicCeiling3Trim);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicLavLineOut, onMicLavLineOut);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicHandheldLineOut, onMicHandheldLineOut);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling1LineOut, onMicCeiling1LineOut);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling2LineOut, onMicCeiling2LineOut);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.MicCeiling3LineOut, onMicCeiling3LineOut);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.CameraSelect, onCameraSelect);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.ShotPresetRecall, onShotPresetRecall);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.ShotPresetSave, onShotPresetSave);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.ShotPresetDelete, onShotPresetDelete);

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

        public event EventHandler<UIEventArgs> CamPresenterFraming;
        private void onCamPresenterFraming(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CamPresenterFraming;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicLavMute;
        private void onMicLavMute(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicLavMute;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicHandheldMute;
        private void onMicHandheldMute(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicHandheldMute;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling1Mute;
        private void onMicCeiling1Mute(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling1Mute;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling2Mute;
        private void onMicCeiling2Mute(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling2Mute;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling3Mute;
        private void onMicCeiling3Mute(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling3Mute;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> DisplayPower;
        private void onDisplayPower(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = DisplayPower;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> D1MirrorToD3;
        private void onD1MirrorToD3(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = D1MirrorToD3;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> D2MirrorToD3;
        private void onD2MirrorToD3(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = D2MirrorToD3;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> VolumeUp;
        private void onVolumeUp(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = VolumeUp;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> VolumeDown;
        private void onVolumeDown(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = VolumeDown;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MuteAll;
        private void onMuteAll(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MuteAll;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> PtzUp;
        private void onPtzUp(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = PtzUp;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> PtzDown;
        private void onPtzDown(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = PtzDown;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> PtzLeft;
        private void onPtzLeft(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = PtzLeft;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> PtzRight;
        private void onPtzRight(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = PtzRight;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> CamSendToVtc;
        private void onCamSendToVtc(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CamSendToVtc;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> ZoomIn;
        private void onZoomIn(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = ZoomIn;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> ZoomOut;
        private void onZoomOut(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = ZoomOut;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> CamHomeShot;
        private void onCamHomeShot(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CamHomeShot;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> CamTrackingShot;
        private void onCamTrackingShot(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CamTrackingShot;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void CamPresenterFramingFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.CamPresenterFramingFb], this);
            }
        }

        public void MicLavMuteFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicLavMuteFb], this);
            }
        }

        public void MicHandheldMuteFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicHandheldMuteFb], this);
            }
        }

        public void MicCeiling1MuteFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling1MuteFb], this);
            }
        }

        public void MicCeiling2MuteFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling2MuteFb], this);
            }
        }

        public void MicCeiling3MuteFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling3MuteFb], this);
            }
        }

        public void PanelOnline(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.PanelOnline], this);
            }
        }

        public void SystemPowerFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.SystemPowerFb], this);
            }
        }

        public void Display1PowerFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.Display1PowerFb], this);
            }
        }

        public void Display2PowerFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.Display2PowerFb], this);
            }
        }

        public void Display3PowerFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.Display3PowerFb], this);
            }
        }

        public void Display4PowerFb(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.Display4PowerFb], this);
            }
        }

        public void MicLavConnected(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicLavConnected], this);
            }
        }

        public void MicHandheldConnected(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicHandheldConnected], this);
            }
        }

        public void MicCeiling1Connected(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling1Connected], this);
            }
        }

        public void MicCeiling2Connected(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling2Connected], this);
            }
        }

        public void MicCeiling3Connected(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.MicCeiling3Connected], this);
            }
        }

        public void RoomPcSync(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.RoomPcSync], this);
            }
        }

        public void ExtPcSync(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.ExtPcSync], this);
            }
        }

        public void AirMediaSync(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AirMediaSync], this);
            }
        }

        public void AirMediaMiracast(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AirMediaMiracast], this);
            }
        }

        public void AirMediaAirPlay(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AirMediaAirPlay], this);
            }
        }

        public void AirMediaTx3(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AirMediaTx3], this);
            }
        }

        public void LaptopHdmiSync(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.LaptopHdmiSync], this);
            }
        }

        public void LaptopUsbcSync(MainBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.LaptopUsbcSync], this);
            }
        }

        public event EventHandler<UIEventArgs> Display1Source;
        private void onDisplay1Source(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display1Source;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display2Source;
        private void onDisplay2Source(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display2Source;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display3Source;
        private void onDisplay3Source(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display3Source;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display4Source;
        private void onDisplay4Source(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display4Source;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Display5Source;
        private void onDisplay5Source(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Display5Source;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> UsbHostSelect;
        private void onUsbHostSelect(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = UsbHostSelect;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> AudioOutputSelect;
        private void onAudioOutputSelect(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = AudioOutputSelect;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> CamUsbOutput;
        private void onCamUsbOutput(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CamUsbOutput;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> CamPresetZone;
        private void onCamPresetZone(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CamPresetZone;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> CamTrackingProfile;
        private void onCamTrackingProfile(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CamTrackingProfile;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicLavTrim;
        private void onMicLavTrim(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicLavTrim;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicHandheldTrim;
        private void onMicHandheldTrim(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicHandheldTrim;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling1Trim;
        private void onMicCeiling1Trim(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling1Trim;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling2Trim;
        private void onMicCeiling2Trim(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling2Trim;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling3Trim;
        private void onMicCeiling3Trim(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling3Trim;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicLavLineOut;
        private void onMicLavLineOut(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicLavLineOut;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicHandheldLineOut;
        private void onMicHandheldLineOut(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicHandheldLineOut;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling1LineOut;
        private void onMicCeiling1LineOut(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling1LineOut;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling2LineOut;
        private void onMicCeiling2LineOut(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling2LineOut;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> MicCeiling3LineOut;
        private void onMicCeiling3LineOut(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = MicCeiling3LineOut;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> CameraSelect;
        private void onCameraSelect(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = CameraSelect;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> ShotPresetRecall;
        private void onShotPresetRecall(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = ShotPresetRecall;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> ShotPresetSave;
        private void onShotPresetSave(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = ShotPresetSave;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> ShotPresetDelete;
        private void onShotPresetDelete(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = ShotPresetDelete;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void Display1SourceFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Display1SourceFb], this);
            }
        }

        public void Display2SourceFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Display2SourceFb], this);
            }
        }

        public void Display3SourceFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Display3SourceFb], this);
            }
        }

        public void Display4SourceFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Display4SourceFb], this);
            }
        }

        public void Display5SourceFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Display5SourceFb], this);
            }
        }

        public void UsbHostSelectFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.UsbHostSelectFb], this);
            }
        }

        public void AudioOutputSelectFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.AudioOutputSelectFb], this);
            }
        }

        public void CamUsbOutputFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.CamUsbOutputFb], this);
            }
        }

        public void CamPresetZoneFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.CamPresetZoneFb], this);
            }
        }

        public void CamTrackingProfileFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.CamTrackingProfileFb], this);
            }
        }

        public void MicLavTrimFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicLavTrimFb], this);
            }
        }

        public void MicHandheldTrimFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicHandheldTrimFb], this);
            }
        }

        public void MicCeiling1TrimFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling1TrimFb], this);
            }
        }

        public void MicCeiling2TrimFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling2TrimFb], this);
            }
        }

        public void MicCeiling3TrimFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling3TrimFb], this);
            }
        }

        public void MicLavLineOutFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicLavLineOutFb], this);
            }
        }

        public void MicHandheldLineOutFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicHandheldLineOutFb], this);
            }
        }

        public void MicCeiling1LineOutFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling1LineOutFb], this);
            }
        }

        public void MicCeiling2LineOutFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling2LineOutFb], this);
            }
        }

        public void MicCeiling3LineOutFb(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling3LineOutFb], this);
            }
        }

        public void OccupancyState(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.OccupancyState], this);
            }
        }

        public void ShutdownCountdown(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.ShutdownCountdown], this);
            }
        }

        public void MicLavLevel(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicLavLevel], this);
            }
        }

        public void MicHandheldLevel(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicHandheldLevel], this);
            }
        }

        public void MicCeiling1Level(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling1Level], this);
            }
        }

        public void MicCeiling2Level(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling2Level], this);
            }
        }

        public void MicCeiling3Level(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.MicCeiling3Level], this);
            }
        }

        public void CamPanPos(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.CamPanPos], this);
            }
        }

        public void CamTiltPos(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.CamTiltPos], this);
            }
        }

        public void CamZoomPos(MainUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.CamZoomPos], this);
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

            CamPresenterFraming = null;
            MicLavMute = null;
            MicHandheldMute = null;
            MicCeiling1Mute = null;
            MicCeiling2Mute = null;
            MicCeiling3Mute = null;
            DisplayPower = null;
            D1MirrorToD3 = null;
            D2MirrorToD3 = null;
            VolumeUp = null;
            VolumeDown = null;
            MuteAll = null;
            PtzUp = null;
            PtzDown = null;
            PtzLeft = null;
            PtzRight = null;
            CamSendToVtc = null;
            ZoomIn = null;
            ZoomOut = null;
            CamHomeShot = null;
            CamTrackingShot = null;
            Display1Source = null;
            Display2Source = null;
            Display3Source = null;
            Display4Source = null;
            Display5Source = null;
            UsbHostSelect = null;
            AudioOutputSelect = null;
            CamUsbOutput = null;
            CamPresetZone = null;
            CamTrackingProfile = null;
            MicLavTrim = null;
            MicHandheldTrim = null;
            MicCeiling1Trim = null;
            MicCeiling2Trim = null;
            MicCeiling3Trim = null;
            MicLavLineOut = null;
            MicHandheldLineOut = null;
            MicCeiling1LineOut = null;
            MicCeiling2LineOut = null;
            MicCeiling3LineOut = null;
            CameraSelect = null;
            ShotPresetRecall = null;
            ShotPresetSave = null;
            ShotPresetDelete = null;
        }

        #endregion

    }
}
