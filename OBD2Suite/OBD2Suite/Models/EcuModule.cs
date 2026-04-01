using System.Collections.Generic;

namespace OBD2Suite.Models
{
    public class AdaptationChannel
    {
        public int ChannelNumber { get; set; }
        public string Name { get; set; } = "";
        public string CurrentValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Description { get; set; } = "";
        public double MinValue { get; set; }
        public double MaxValue { get; set; } = 65535;
    }

    public class EcuModule
    {
        public int Address { get; set; }
        public string Name { get; set; } = "";
        public string Protocol { get; set; } = "CAN";
        public string Variant { get; set; } = "";
        public string PartNumber { get; set; } = "";
        public string SoftwareVersion { get; set; } = "";
        public string HardwareVersion { get; set; } = "";
        public byte[] CodingBytes { get; set; } = Array.Empty<byte>();
        public string LongCodingString { get; set; } = "";
        public List<AdaptationChannel> AdaptationChannels { get; set; } = new();
        public bool IsReachable { get; set; } = true;
        public string AddressHex => $"0x{Address:X2}";
        public string CodingHex => BitConverter.ToString(CodingBytes).Replace("-", " ");
    }
}
