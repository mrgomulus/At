using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Interface for manufacturer-specific diagnostic functions beyond standard OBD2.
    /// </summary>
    public interface IManufacturerService
    {
        Manufacturer Manufacturer { get; }
        string DisplayName { get; }

        // ECU scanning — full vehicle scan across all bus addresses
        Task<List<EcuModule>> ScanAllEcusAsync();

        // OEM-specific fault codes (incl. sub-system codes not in OBD2 Mode 03)
        Task<List<OemDtcRecord>> ReadAllFaultCodesAsync(int ecuAddress);
        Task<bool> ClearFaultCodesAsync(int ecuAddress);

        // Measuring blocks / live data groups
        Task<List<MeasuringBlock>> ReadMeasuringBlockAsync(int ecuAddress, int groupNumber);
        Task<List<int>> GetAvailableMeasuringGroupsAsync(int ecuAddress);

        // Adaptations
        Task<string> ReadAdaptationAsync(int ecuAddress, int channel);
        Task<bool> WriteAdaptationAsync(int ecuAddress, int channel, string newValue);

        // ECU Coding
        Task<string> ReadCodingAsync(int ecuAddress);
        Task<bool> WriteCodingAsync(int ecuAddress, string newCoding);

        // Long coding (byte array)
        Task<byte[]> ReadLongCodingAsync(int ecuAddress);
        Task<bool> WriteLongCodingAsync(int ecuAddress, byte[] codingBytes);

        // Actuator / output tests
        Task<List<ActuatorTest>> GetActuatorTestsAsync(int ecuAddress);
        Task<string> RunActuatorTestAsync(int ecuAddress, int testId);

        // Security access / login
        Task<bool> LoginAsync(int ecuAddress, int accessCode);

        // Service resets
        Task<bool> ResetServiceIntervalAsync(int ecuAddress);
        Task<bool> ResetThrottleBodyAsync(int ecuAddress);
        Task<bool> PerformSteeringAngleCalibrationAsync(int ecuAddress);
        Task<bool> PerformThrottleBasicSettingAsync(int ecuAddress);

        // ECU identification
        Task<Dictionary<string, string>> ReadEcuIdentificationAsync(int ecuAddress);

        // Extended special functions (all return status string)
        Task<string> PerformEpbServiceAsync(int ecuAddress);
        Task<string> PerformDpfRegenerationAsync(int ecuAddress);
        Task<string> RegisterBatteryAsync(int ecuAddress, int capacityAh, string batteryType);
        Task<string> ResetAbsBleedAsync(int ecuAddress);
        Task<string> PerformGearboxAdaptationResetAsync(int ecuAddress);
        Task<string> PerformInjectorCodingAsync(int ecuAddress, string[] codes);
        Task<string> PerformTpmsReregistrationAsync(int ecuAddress);
        Task<string> ResetAdBlueAsync(int ecuAddress);
        Task<string> ResetDpfAshCounterAsync(int ecuAddress);

        // Manufacturer-specific info (OEM custom PIDs, enhanced live data)
        Task<Dictionary<string, string>> ReadEnhancedPidsAsync();
    }
}
