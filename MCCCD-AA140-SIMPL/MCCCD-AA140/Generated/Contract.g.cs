using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace MCCCD_AA140
{
    /// <summary>
    /// Common Interface for Root Contracts.
    /// </summary>
    public interface IContract
    {
        object UserObject { get; set; }
        void AddDevice(BasicTriListWithSmartObject device);
        void RemoveDevice(BasicTriListWithSmartObject device);
    }

    /// <summary>
    /// AA140 panel: 3 displays, 4 sources, 3 cameras, Q-SYS audio.
    /// </summary>
    public class Contract : IContract, IDisposable
    {
        #region Components

        private ComponentMediator ComponentMediator { get; set; }

        public MCCCD_AA140.IMain AA140 { get { return (MCCCD_AA140.IMain)InternalAA140; } }
        private MCCCD_AA140.Main InternalAA140 { get; set; }

        public MCCCD_AA140.VideoSync.IVideoSync VideoSync { get { return (MCCCD_AA140.VideoSync.IVideoSync)InternalVideoSync; } }
        private MCCCD_AA140.VideoSync.VideoSync InternalVideoSync { get; set; }

        #endregion

        #region Construction and Initialization

        public Contract()
            : this(new List<BasicTriListWithSmartObject>().ToArray())
        {
        }

        public Contract(BasicTriListWithSmartObject device)
            : this(new [] { device })
        {
        }

        public Contract(BasicTriListWithSmartObject[] devices)
        {
            if (devices == null)
                throw new ArgumentNullException("Devices is null");

            ComponentMediator = new ComponentMediator();

            InternalAA140 = new MCCCD_AA140.Main(ComponentMediator, 1);
            InternalVideoSync = new MCCCD_AA140.VideoSync.VideoSync(ComponentMediator, 2);

            for (int index = 0; index < devices.Length; index++)
            {
                AddDevice(devices[index]);
            }
        }

        #endregion

        #region Standard Contract Members

        public object UserObject { get; set; }

        public void AddDevice(BasicTriListWithSmartObject device)
        {
            InternalAA140.AddDevice(device);
            InternalVideoSync.AddDevice(device);
        }

        public void RemoveDevice(BasicTriListWithSmartObject device)
        {
            InternalAA140.RemoveDevice(device);
            InternalVideoSync.RemoveDevice(device);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            InternalAA140.Dispose();
            InternalVideoSync.Dispose();
            ComponentMediator.Dispose(); 
        }

        #endregion

    }
}
