namespace MCCCD_AA140
{
    /// <summary>
    /// Constants for raw SmartObject join numbers, derived verbatim from the
    /// generated <c>MCCCD_AA140.cse2j</c> mapping that ships with the .ch5z.
    ///
    /// Why these exist rather than using <c>Contract.AA140.&lt;Event&gt;</c> /
    /// <c>Contract.AA140.&lt;Method&gt;(callback)</c>:
    /// Contract Editor generated <c>Main.g.cs</c> registers event handlers and
    /// I/O writes against join numbers derived from a names table that does
    /// NOT match the panel-side .cse2j mapping for boolean signals and for
    /// numerics past index 4. e.g. tapping <c>AA140.DisplayPower</c> fires the
    /// <c>PanelOnline</c> event (both occupy SmartObject 1 join 1 but in
    /// different banks). Driving <c>_c.AA140.DisplayPower((sig,_)=&gt;sig.BoolValue=true)</c>
    /// writes <c>BooleanInput[1]</c> which the panel reads as
    /// <c>AA140.PanelOnline</c>. The wrappers are unreliable.
    ///
    /// PanelDispatcher uses these constants to talk directly to the panel's
    /// SmartObject 1 input/output banks, bypassing the misalignment. When the
    /// .cce is rebuilt with aligned joins, we can either keep this dispatcher
    /// or migrate back to the Contract Editor wrappers.
    ///
    /// SmartObject ID for the AA140 Main contract is always 1.
    /// </summary>
    public static class PanelJoins
    {
        public const uint SmartObjectId = 1;

        /// <summary>
        /// Boolean OUTPUT joins on SmartObject 1.
        /// Panel publishes here; C# subscribes via PanelDispatcher.OnBool.
        /// Source: <c>MCCCD_AA140.cse2j</c> → <c>signals.states.boolean["1"]</c>.
        /// </summary>
        public static class BoolOut
        {
            public const uint DisplayPower     = 1;
            public const uint D1MirrorToD3     = 2;
            public const uint D2MirrorToD3     = 3;
            public const uint VolumeUp         = 4;
            public const uint VolumeDown       = 5;
            public const uint MuteAll          = 6;
            public const uint MicLavMute       = 7;
            public const uint MicHandheldMute  = 8;
            public const uint PtzUp            = 9;
            public const uint PtzDown          = 10;
            public const uint PtzLeft          = 11;
            public const uint PtzRight         = 12;
            public const uint CamSendToVtc     = 13;
            public const uint ZoomIn           = 14;
            public const uint ZoomOut          = 15;
            public const uint MicCeiling1Mute  = 16;
            public const uint MicCeiling2Mute  = 17;
            public const uint MicCeiling3Mute  = 18;
        }

        /// <summary>
        /// Boolean INPUT joins on SmartObject 1.
        /// Panel subscribes here; C# drives via PanelDispatcher.WriteBool.
        /// Source: <c>MCCCD_AA140.cse2j</c> → <c>signals.events.boolean</c>.
        /// </summary>
        public static class BoolIn
        {
            public const uint PanelOnline          = 1;
            public const uint MicLavMuteFb         = 2;
            public const uint MicHandheldMuteFb    = 3;
            public const uint SystemPowerFb        = 4;
            public const uint Display1PowerFb      = 5;
            public const uint Display2PowerFb      = 6;
            public const uint Display3PowerFb      = 7;
            public const uint MicCeiling1MuteFb    = 8;
            public const uint MicCeiling2MuteFb    = 9;
            public const uint MicCeiling3MuteFb    = 10;
            public const uint MicLavConnected      = 11;
            public const uint MicHandheldConnected = 12;
            public const uint MicCeiling1Connected = 13;
            public const uint MicCeiling2Connected = 14;
            public const uint MicCeiling3Connected = 15;
            public const uint Display4PowerFb      = 16;
            // Source video sync feedback (Home source card badges).
            // Driven from NvxRoutingService (HDMI sync FBs) and AirMediaService
            // (AM-3200 REST poll for the 3 sharing-method signals).
            public const uint RoomPcSync           = 17;
            public const uint ExtPcSync            = 18;
            public const uint AirMediaSync         = 19;
            public const uint AirMediaMiracast     = 20;
            public const uint AirMediaAirPlay      = 21;
            public const uint AirMediaTx3          = 22;
            public const uint LaptopHdmiSync       = 23;
            public const uint LaptopUsbcSync       = 24;
        }

        /// <summary>
        /// UShort OUTPUT joins on SmartObject 1.
        /// Panel publishes here; C# subscribes via PanelDispatcher.OnUShort.
        /// Source: <c>MCCCD_AA140.cse2j</c> → <c>signals.states.numeric["1"]</c>.
        /// </summary>
        public static class UShortOut
        {
            public const uint Display1Source      = 1;
            public const uint Display2Source      = 2;
            public const uint Display3Source      = 3;
            public const uint AudioOutputSelect   = 4;
            public const uint CameraSelect        = 5;
            public const uint ShotPresetRecall    = 6;
            public const uint ShotPresetSave      = 7;
            public const uint ShotPresetDelete    = 8;
            public const uint CamTrackingMode     = 9;
            public const uint MicLavTrim          = 10;
            public const uint MicHandheldTrim     = 11;
            public const uint MicCeiling1Trim     = 12;
            public const uint MicCeiling2Trim     = 13;
            public const uint MicCeiling3Trim     = 14;
            public const uint MicLavLineOut       = 15;
            public const uint MicHandheldLineOut  = 16;
            public const uint MicCeiling1LineOut  = 17;
            public const uint MicCeiling2LineOut  = 18;
            public const uint MicCeiling3LineOut  = 19;
            public const uint Display4Source      = 20;   // podium confidence monitor
        }

        /// <summary>
        /// UShort INPUT joins on SmartObject 1.
        /// Panel subscribes here; C# drives via PanelDispatcher.WriteUShort.
        /// Source: <c>MCCCD_AA140.cse2j</c> → <c>signals.events.numeric</c>.
        /// </summary>
        public static class UShortIn
        {
            public const uint Display1SourceFb     = 1;
            public const uint Display2SourceFb     = 2;
            public const uint Display3SourceFb     = 3;
            public const uint AudioOutputSelectFb  = 4;
            public const uint CamTrackingModeFb    = 5;
            public const uint OccupancyState       = 6;
            public const uint ShutdownCountdown    = 7;
            public const uint MicLavTrimFb         = 8;
            public const uint MicHandheldTrimFb    = 9;
            public const uint MicCeiling1TrimFb    = 10;
            public const uint MicCeiling2TrimFb    = 11;
            public const uint MicCeiling3TrimFb    = 12;
            public const uint MicLavLineOutFb      = 13;
            public const uint MicHandheldLineOutFb = 14;
            public const uint MicCeiling1LineOutFb = 15;
            public const uint MicCeiling2LineOutFb = 16;
            public const uint MicCeiling3LineOutFb = 17;
            public const uint MicLavLevel          = 18;
            public const uint MicHandheldLevel     = 19;
            public const uint MicCeiling1Level     = 20;
            public const uint MicCeiling2Level     = 21;
            public const uint MicCeiling3Level     = 22;
            public const uint Display4SourceFb     = 23;
        }
    }
}
