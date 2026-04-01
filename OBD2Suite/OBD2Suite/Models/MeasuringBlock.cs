namespace OBD2Suite.Models
{
    /// <summary>
    /// Represents a single measurement field within a measuring block group (VAG/BMW style).
    /// </summary>
    public class MeasuringField
    {
        public int FieldIndex { get; set; }
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Unit { get; set; } = "";
        public string DisplayText => string.IsNullOrEmpty(Unit) ? Value : $"{Value} {Unit}";
    }

    /// <summary>
    /// A measuring block (Messwerteblock) as used in VAG/BMW diagnostics.
    /// Each block contains up to 4 measurement fields.
    /// </summary>
    public class MeasuringBlock
    {
        public int GroupNumber { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<MeasuringField> Fields { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string GroupLabel => $"Group {GroupNumber:D3} — {Name}";
    }
}
