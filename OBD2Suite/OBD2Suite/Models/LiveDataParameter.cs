namespace OBD2Suite.Models
{
    public enum PidCategory
    {
        Engine, Transmission, Fuel, Emissions, Temperature, Pressure, Speed, Electrical, Sensor, Other
    }

    public class LiveDataParameter
    {
        public string Name { get; set; } = "";
        public string PidHex { get; set; } = "";
        public byte PidByte { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; } = "";
        public double MinValue { get; set; }
        public double MaxValue { get; set; } = 100;
        public string FormulaDescription { get; set; } = "";
        public PidCategory Category { get; set; } = PidCategory.Engine;
        public bool IsSupported { get; set; } = true;
        public string DisplayValue => $"{Value:F1} {Unit}";
        public double NormalizedValue => MaxValue > MinValue ? (Value - MinValue) / (MaxValue - MinValue) : 0;

        public string StatusColor
        {
            get
            {
                double pct = NormalizedValue;
                if (pct > 0.9) return "#FF5252";
                if (pct > 0.75) return "#FFB300";
                return "#4CAF50";
            }
        }
    }
}
