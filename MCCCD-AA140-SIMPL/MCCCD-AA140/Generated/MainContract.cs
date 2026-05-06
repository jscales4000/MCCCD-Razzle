using System;
using Crestron.SimplSharpPro.DeviceSupport;

namespace MCCCD_AA140.Generated
{
    // STUB — emitted from contracts/MCCCD-AA140.cce so the project compiles
    // without a Crestron Contract Editor build. Replace this file with the
    // Contract-Editor-generated MCCCD_AA140.g.cs once Phase 4 is run.
    //
    // Keep signal names in lockstep with src/lib/contract.ts in the panel
    // project — drift = silent join failures.

    public class BoolInputSig
    {
        public bool BoolValue { get; set; }
        public event Action OnDigitalRise = delegate { };
        public event Action<bool> OnDigitalChange = delegate { };

        public void Drive(bool v)
        {
            var prev = BoolValue;
            BoolValue = v;
            OnDigitalChange(v);
            if (!prev && v) OnDigitalRise();
        }
    }

    public class UShortInputSig
    {
        public ushort UShortValue { get; set; }
        public event Action<ushort> OnAnalogChange = delegate { };

        public void Drive(ushort v)
        {
            UShortValue = v;
            OnAnalogChange(v);
        }
    }

    public class BoolOutputSig
    {
        public bool BoolValue { get; set; }
    }

    public class UShortOutputSig
    {
        public ushort UShortValue { get; set; }
    }

    public class MainContract
    {
        public BoolInputSig DisplayPower { get; } = new BoolInputSig();
        public UShortInputSig Display1Source { get; } = new UShortInputSig();
        public UShortInputSig Display2Source { get; } = new UShortInputSig();
        public UShortInputSig Display3Source { get; } = new UShortInputSig();
        public BoolInputSig D1MirrorToD3 { get; } = new BoolInputSig();
        public BoolInputSig D2MirrorToD3 { get; } = new BoolInputSig();
        public UShortInputSig AudioOutputSelect { get; } = new UShortInputSig();
        public BoolInputSig VolumeUp { get; } = new BoolInputSig();
        public BoolInputSig VolumeDown { get; } = new BoolInputSig();
        public BoolInputSig MuteAll { get; } = new BoolInputSig();
        public BoolInputSig MicLavMute { get; } = new BoolInputSig();
        public BoolInputSig MicHandheldMute { get; } = new BoolInputSig();
        public UShortInputSig CameraSelect { get; } = new UShortInputSig();
        public BoolInputSig PtzUp { get; } = new BoolInputSig();
        public BoolInputSig PtzDown { get; } = new BoolInputSig();
        public BoolInputSig PtzLeft { get; } = new BoolInputSig();
        public BoolInputSig PtzRight { get; } = new BoolInputSig();
        public UShortInputSig ShotPresetRecall { get; } = new UShortInputSig();
        public UShortInputSig ShotPresetSave { get; } = new UShortInputSig();
        public UShortInputSig ShotPresetDelete { get; } = new UShortInputSig();
        public BoolInputSig CamSendToVtc { get; } = new BoolInputSig();
        public UShortInputSig CamTrackingMode { get; } = new UShortInputSig();
        public BoolInputSig ZoomIn { get; } = new BoolInputSig();
        public BoolInputSig ZoomOut { get; } = new BoolInputSig();
        public BoolInputSig MicCeiling1Mute { get; } = new BoolInputSig();
        public BoolInputSig MicCeiling2Mute { get; } = new BoolInputSig();
        public BoolInputSig MicCeiling3Mute { get; } = new BoolInputSig();
        public UShortInputSig MicLavTrim { get; } = new UShortInputSig();
        public UShortInputSig MicHandheldTrim { get; } = new UShortInputSig();
        public UShortInputSig MicCeiling1Trim { get; } = new UShortInputSig();
        public UShortInputSig MicCeiling2Trim { get; } = new UShortInputSig();
        public UShortInputSig MicCeiling3Trim { get; } = new UShortInputSig();
        public UShortInputSig MicLavLineOut { get; } = new UShortInputSig();
        public UShortInputSig MicHandheldLineOut { get; } = new UShortInputSig();
        public UShortInputSig MicCeiling1LineOut { get; } = new UShortInputSig();
        public UShortInputSig MicCeiling2LineOut { get; } = new UShortInputSig();
        public UShortInputSig MicCeiling3LineOut { get; } = new UShortInputSig();

        public BoolOutputSig PanelOnline { get; } = new BoolOutputSig();
        public UShortOutputSig Display1SourceFb { get; } = new UShortOutputSig();
        public UShortOutputSig Display2SourceFb { get; } = new UShortOutputSig();
        public UShortOutputSig Display3SourceFb { get; } = new UShortOutputSig();
        public UShortOutputSig AudioOutputSelectFb { get; } = new UShortOutputSig();
        public BoolOutputSig MicLavMuteFb { get; } = new BoolOutputSig();
        public BoolOutputSig MicHandheldMuteFb { get; } = new BoolOutputSig();
        public UShortOutputSig CamTrackingModeFb { get; } = new UShortOutputSig();
        public UShortOutputSig OccupancyState { get; } = new UShortOutputSig();
        public UShortOutputSig ShutdownCountdown { get; } = new UShortOutputSig();
        public BoolOutputSig SystemPowerFb { get; } = new BoolOutputSig();
        public BoolOutputSig Display1PowerFb { get; } = new BoolOutputSig();
        public BoolOutputSig Display2PowerFb { get; } = new BoolOutputSig();
        public BoolOutputSig Display3PowerFb { get; } = new BoolOutputSig();
        public BoolOutputSig MicCeiling1MuteFb { get; } = new BoolOutputSig();
        public BoolOutputSig MicCeiling2MuteFb { get; } = new BoolOutputSig();
        public BoolOutputSig MicCeiling3MuteFb { get; } = new BoolOutputSig();
        public UShortOutputSig MicLavTrimFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicHandheldTrimFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling1TrimFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling2TrimFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling3TrimFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicLavLineOutFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicHandheldLineOutFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling1LineOutFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling2LineOutFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling3LineOutFb { get; } = new UShortOutputSig();
        public UShortOutputSig MicLavLevel { get; } = new UShortOutputSig();
        public UShortOutputSig MicHandheldLevel { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling1Level { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling2Level { get; } = new UShortOutputSig();
        public UShortOutputSig MicCeiling3Level { get; } = new UShortOutputSig();
        public BoolOutputSig MicLavConnected { get; } = new BoolOutputSig();
        public BoolOutputSig MicHandheldConnected { get; } = new BoolOutputSig();
        public BoolOutputSig MicCeiling1Connected { get; } = new BoolOutputSig();
        public BoolOutputSig MicCeiling2Connected { get; } = new BoolOutputSig();
        public BoolOutputSig MicCeiling3Connected { get; } = new BoolOutputSig();

        public MainContract(BasicTriList tp1, BasicTriList tp2)
        {
            // Stub: real Contract Editor build wires CIP signals here so panel
            // events drive Drive(...) on the input sigs and writes to the
            // *Output sigs propagate to the panel via CIP.
        }
    }
}
