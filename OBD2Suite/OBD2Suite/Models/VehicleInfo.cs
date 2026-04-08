using System.Collections.Generic;

namespace OBD2Suite.Models
{
    public class VehicleInfo
    {
        public string Vin { get; set; } = "";
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public int Year { get; set; }
        public string EngineType { get; set; } = "";
        public string TransmissionType { get; set; } = "";
        public string CalibrationId { get; set; } = "";
        public string CalibrationVerificationNumber { get; set; } = "";
        public string EcuName { get; set; } = "";
        public string EcuPartNumber { get; set; } = "";
        public string SoftwareVersion { get; set; } = "";
        public List<EcuModule> EcuModules { get; set; } = new();

        public string VinDecoded =>
            Vin.Length >= 17
                ? $"WMI:{Vin.Substring(0, 3)} VDS:{Vin.Substring(3, 6)} VIS:{Vin.Substring(9)}"
                : Vin;
    }
}
