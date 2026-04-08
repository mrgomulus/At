namespace OBD2Suite.Models
{
    public enum ActuatorTestStatus
    {
        Idle,
        Running,
        Completed,
        Failed
    }

    /// <summary>
    /// Represents an output/actuator test that can be triggered on an ECU.
    /// Corresponds to VAG "Output Tests" / BMW "Actuator Tests".
    /// </summary>
    public class ActuatorTest
    {
        public int TestId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int EcuAddress { get; set; }
        public ActuatorTestStatus Status { get; set; } = ActuatorTestStatus.Idle;
        public string LastResult { get; set; } = "";
        public string Category { get; set; } = "";
        public bool RequiresEngineOff { get; set; }
        public bool RequiresEngineRunning { get; set; }
        public string StatusText => Status switch
        {
            ActuatorTestStatus.Running => "⏳ Running…",
            ActuatorTestStatus.Completed => "✔ Completed",
            ActuatorTestStatus.Failed => "✖ Failed",
            _ => "— Idle"
        };
    }
}
