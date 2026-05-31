using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace MCCCD_AA140
{
    /// <summary>
    /// Digital feedback - NVX E30 (Room PC) HDMI sync detect
    /// </summary>
    /// <summary>
    /// Digital feedback - NVX E30 (Ext PC) HDMI sync detect
    /// </summary>
    /// <summary>
    /// Digital feedback - NVX E30 (AirMedia output) HDMI sync detect
    /// </summary>
    /// <summary>
    /// Digital feedback - AM-3200 Miracast session active
    /// </summary>
    /// <summary>
    /// Digital feedback - AM-3200 AirPlay session active
    /// </summary>
    /// <summary>
    /// Digital feedback - AM-3200 AM-TX3-200 wired transmitter active
    /// </summary>
    /// <summary>
    /// Digital feedback - NVX-384 input 1 (BYOD HDMI) sync detect
    /// </summary>
    /// <summary>
    /// Digital feedback - NVX-384 input 3 (BYOD USB-C) sync detect
    /// </summary>
    /// <summary>
    /// Dummy pair for RoomPcSync - unused at runtime
    /// </summary>
    /// <summary>
    /// Dummy pair for ExtPcSync - unused at runtime
    /// </summary>
    /// <summary>
    /// Dummy pair for AirMediaSync - unused at runtime
    /// </summary>
    /// <summary>
    /// Dummy pair for AirMediaMiracast - unused at runtime
    /// </summary>
    /// <summary>
    /// Dummy pair for AirMediaAirPlay - unused at runtime
    /// </summary>
    /// <summary>
    /// Dummy pair for AirMediaTx3 - unused at runtime
    /// </summary>
    /// <summary>
    /// Dummy pair for LaptopHdmiSync - unused at runtime
    /// </summary>
    /// <summary>
    /// Dummy pair for LaptopUsbcSync - unused at runtime
    /// </summary>
    public interface IVideoSync
    {
        object UserObject { get; set; }

        event EventHandler<UIEventArgs> RoomPcSync;
        event EventHandler<UIEventArgs> ExtPcSync;
        event EventHandler<UIEventArgs> AirMediaSync;
        event EventHandler<UIEventArgs> AirMediaMiracast;
        event EventHandler<UIEventArgs> AirMediaAirPlay;
        event EventHandler<UIEventArgs> AirMediaTx3;
        event EventHandler<UIEventArgs> LaptopHdmiSync;
        event EventHandler<UIEventArgs> LaptopUsbcSync;

        void RoomPcSyncSet(VideoSyncBoolInputSigDelegate callback);
        void ExtPcSyncSet(VideoSyncBoolInputSigDelegate callback);
        void AirMediaSyncSet(VideoSyncBoolInputSigDelegate callback);
        void AirMediaMiracastSet(VideoSyncBoolInputSigDelegate callback);
        void AirMediaAirPlaySet(VideoSyncBoolInputSigDelegate callback);
        void AirMediaTx3Set(VideoSyncBoolInputSigDelegate callback);
        void LaptopHdmiSyncSet(VideoSyncBoolInputSigDelegate callback);
        void LaptopUsbcSyncSet(VideoSyncBoolInputSigDelegate callback);

    }

    public delegate void VideoSyncBoolInputSigDelegate(BoolInputSig boolInputSig, IVideoSync videoSync);

    internal class VideoSync : IVideoSync, IDisposable
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
                public const uint RoomPcSync = 1;
                public const uint ExtPcSync = 2;
                public const uint AirMediaSync = 3;
                public const uint AirMediaMiracast = 4;
                public const uint AirMediaAirPlay = 5;
                public const uint AirMediaTx3 = 6;
                public const uint LaptopHdmiSync = 7;
                public const uint LaptopUsbcSync = 8;

                public const uint RoomPcSyncSet = 1;
                public const uint ExtPcSyncSet = 2;
                public const uint AirMediaSyncSet = 3;
                public const uint AirMediaMiracastSet = 4;
                public const uint AirMediaAirPlaySet = 5;
                public const uint AirMediaTx3Set = 6;
                public const uint LaptopHdmiSyncSet = 7;
                public const uint LaptopUsbcSyncSet = 8;
            }
        }

        #endregion

        #region Construction and Initialization

        internal VideoSync(ComponentMediator componentMediator, uint controlJoinId)
        {
            ComponentMediator = componentMediator;
            Initialize(controlJoinId);
        }

        private void Initialize(uint controlJoinId)
        {
            ControlJoinId = controlJoinId; 
 
            _devices = new List<BasicTriListWithSmartObject>(); 
 
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.RoomPcSync, onRoomPcSync);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.ExtPcSync, onExtPcSync);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.AirMediaSync, onAirMediaSync);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.AirMediaMiracast, onAirMediaMiracast);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.AirMediaAirPlay, onAirMediaAirPlay);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.AirMediaTx3, onAirMediaTx3);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.LaptopHdmiSync, onLaptopHdmiSync);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.LaptopUsbcSync, onLaptopUsbcSync);

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

        public event EventHandler<UIEventArgs> RoomPcSync;
        private void onRoomPcSync(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = RoomPcSync;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> ExtPcSync;
        private void onExtPcSync(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = ExtPcSync;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> AirMediaSync;
        private void onAirMediaSync(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = AirMediaSync;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> AirMediaMiracast;
        private void onAirMediaMiracast(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = AirMediaMiracast;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> AirMediaAirPlay;
        private void onAirMediaAirPlay(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = AirMediaAirPlay;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> AirMediaTx3;
        private void onAirMediaTx3(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = AirMediaTx3;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> LaptopHdmiSync;
        private void onLaptopHdmiSync(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = LaptopHdmiSync;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> LaptopUsbcSync;
        private void onLaptopUsbcSync(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = LaptopUsbcSync;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void RoomPcSyncSet(VideoSyncBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.RoomPcSyncSet], this);
            }
        }

        public void ExtPcSyncSet(VideoSyncBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.ExtPcSyncSet], this);
            }
        }

        public void AirMediaSyncSet(VideoSyncBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AirMediaSyncSet], this);
            }
        }

        public void AirMediaMiracastSet(VideoSyncBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AirMediaMiracastSet], this);
            }
        }

        public void AirMediaAirPlaySet(VideoSyncBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AirMediaAirPlaySet], this);
            }
        }

        public void AirMediaTx3Set(VideoSyncBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AirMediaTx3Set], this);
            }
        }

        public void LaptopHdmiSyncSet(VideoSyncBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.LaptopHdmiSyncSet], this);
            }
        }

        public void LaptopUsbcSyncSet(VideoSyncBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.LaptopUsbcSyncSet], this);
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
            return string.Format("Contract: {0} Component: {1} HashCode: {2} {3}", "VideoSync", GetType().Name, GetHashCode(), UserObject != null ? "UserObject: " + UserObject : null);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            RoomPcSync = null;
            ExtPcSync = null;
            AirMediaSync = null;
            AirMediaMiracast = null;
            AirMediaAirPlay = null;
            AirMediaTx3 = null;
            LaptopHdmiSync = null;
            LaptopUsbcSync = null;
        }

        #endregion

    }
}
