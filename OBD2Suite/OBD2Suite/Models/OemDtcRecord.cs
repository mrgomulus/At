namespace OBD2Suite.Models
{
    /// <summary>
    /// Extended DTC record with manufacturer-specific information.
    /// </summary>
    public class OemDtcRecord
    {
        public string Code { get; set; } = "";
        public string OemCode { get; set; } = "";           // e.g. "00532" (VAG 5-digit)
        public string Description { get; set; } = "";
        public string OemDescription { get; set; } = "";   // Manufacturer-specific text
        public string EcuName { get; set; } = "";
        public int EcuAddress { get; set; }
        public string Frequency { get; set; } = "";        // e.g. "3 times"
        public string SymptomCode { get; set; } = "";      // VAG symptom byte
        public bool IsActive { get; set; }
        public bool IsStored { get; set; }
        public bool IsIntermittent { get; set; }
        public string FreezeFrameData { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public string StatusText => IsActive ? "Active" : IsIntermittent ? "Intermittent" : "Stored";
    }
}
