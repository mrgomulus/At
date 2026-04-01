using System.Collections.Generic;
using System.Threading.Tasks;
using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    public interface IObdService
    {
        bool IsConnected { get; }
        bool IsSimulationMode { get; set; }
        ObdConnection? CurrentConnection { get; }

        Task<bool> ConnectAsync(ObdConnection connection);
        Task DisconnectAsync();
        Task<string> SendCommandAsync(string command, int timeoutMs = 2000);
        Task<List<DiagnosticTroubleCode>> ReadDtcsAsync();
        Task<bool> ClearDtcsAsync();
        Task<List<LiveDataParameter>> ReadLiveDataAsync();
        Task<VehicleInfo> ReadVehicleInfoAsync();
        Task<List<EcuModule>> ReadEcuModulesAsync();
        Task<string> ReadEcuCodingAsync(int ecuAddress, int dataIdentifier);
        Task<bool> WriteEcuCodingAsync(int ecuAddress, int dataIdentifier, byte[] data);
        Task<string> ReadAdaptationAsync(int ecuAddress, int channelNumber);
        Task<bool> WriteAdaptationAsync(int ecuAddress, int channelNumber, string value);
    }
}
