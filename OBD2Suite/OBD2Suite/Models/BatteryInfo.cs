namespace OBD2Suite.Models
{
    public enum BatteryTestResult { Good, Replace, ChargeThenRetest, Bad }

    /// <summary>Battery health test result from a load test / conductance test.</summary>
    public class BatteryTestInfo
    {
        public double Voltage { get; set; }
        public double ColdCrankingAmps { get; set; }      // Measured CCA
        public double RatedColdCrankingAmps { get; set; } // Battery label CCA
        public double StateOfCharge { get; set; }         // 0-100 %
        public double StateOfHealth { get; set; }         // 0-100 %
        public double InternalResistance { get; set; }    // mΩ
        public BatteryTestResult Result { get; set; }
        public string BatteryType { get; set; } = "Lead-Acid";  // Lead-Acid, AGM, EFB, Lithium
        public bool IsRegistered { get; set; }
        public string PartNumber { get; set; } = "";
        public DateTime TestedAt { get; set; } = DateTime.Now;

        public string ResultText => Result switch
        {
            BatteryTestResult.Good             => "✔ Good — No replacement needed",
            BatteryTestResult.Replace          => "✖ Replace Battery",
            BatteryTestResult.ChargeThenRetest => "⚠ Charge and retest",
            BatteryTestResult.Bad              => "✖ Battery defective — Replace immediately",
            _ => "Unknown"
        };

        public string ResultColor => Result switch
        {
            BatteryTestResult.Good             => "#4CAF50",
            BatteryTestResult.Replace          => "#FF5252",
            BatteryTestResult.ChargeThenRetest => "#FFB300",
            BatteryTestResult.Bad              => "#FF1744",
            _ => "#888888"
        };
    }
}
