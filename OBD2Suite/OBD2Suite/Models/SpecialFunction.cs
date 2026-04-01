namespace OBD2Suite.Models
{
    public enum SpecialFunctionCategory
    {
        BrakeSystem,
        Engine,
        Transmission,
        Emissions,
        Electrical,
        Body,
        Suspension,
        Steering,
        FuelSystem,
        Hybrid,
        ADAS,
        Maintenance
    }

    public enum SpecialFunctionStatus
    {
        Ready,
        Running,
        Success,
        Failed,
        NotSupported
    }

    /// <summary>
    /// A manufacturer-specific special service function (e.g. EPB retract, DPF regen, battery registration).
    /// </summary>
    public class SpecialFunction
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Note { get; set; } = "";           // Safety/procedure note
        public SpecialFunctionCategory Category { get; set; }
        public SpecialFunctionStatus Status { get; set; } = SpecialFunctionStatus.Ready;
        public string LastResult { get; set; } = "";
        public string Manufacturer { get; set; } = "All";
        public bool RequiresIgnitionOff { get; set; }
        public bool RequiresEngineRunning { get; set; }
        public bool RequiresVehicleStationary { get; set; } = true;
        public string CategoryIcon => Category switch
        {
            SpecialFunctionCategory.BrakeSystem   => "🛑",
            SpecialFunctionCategory.Engine        => "⚙",
            SpecialFunctionCategory.Transmission  => "🔄",
            SpecialFunctionCategory.Emissions     => "💨",
            SpecialFunctionCategory.Electrical    => "⚡",
            SpecialFunctionCategory.Body          => "🚗",
            SpecialFunctionCategory.Suspension    => "🔧",
            SpecialFunctionCategory.Steering      => "🎯",
            SpecialFunctionCategory.FuelSystem    => "⛽",
            SpecialFunctionCategory.Hybrid        => "🔋",
            SpecialFunctionCategory.ADAS          => "📡",
            SpecialFunctionCategory.Maintenance   => "🔑",
            _ => "⚙"
        };

        public string StatusText => Status switch
        {
            SpecialFunctionStatus.Running      => "⏳ Running…",
            SpecialFunctionStatus.Success      => "✔ Success",
            SpecialFunctionStatus.Failed       => "✖ Failed",
            SpecialFunctionStatus.NotSupported => "— Not Supported",
            _ => "Ready"
        };
    }
}
