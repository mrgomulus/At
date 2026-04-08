# OBD2Suite - Professional Vehicle Diagnostics Software

## Überblick / Overview

OBD2Suite ist eine professionelle Diagnosesoftware für Fahrzeuge, die eine breite Palette von OBD-Adaptern unterstützt und erweiterte Diagnosefunktionen für zahlreiche Fahrzeughersteller bietet.

OBD2Suite is professional vehicle diagnostic software that supports a wide range of OBD adapters and provides advanced diagnostic functions for numerous vehicle manufacturers.

## 🚀 Hauptmerkmale / Key Features

### Adapter-Unterstützung / Adapter Support

**21 unterstützte Adapter-Marken:**
- ✅ **WOW** (Wurth WOW/Snooper) - Professional diagnostic adapter
- ✅ **ELM327** - Generic chipset (most common)
- ✅ **OBDLink** - High-speed professional adapters (MX/MX+/EX series)
- ✅ **BlueDriver** - Professional Bluetooth Pro
- ✅ **Veepeak**, **BAFX**, **Vgate/iCar** - Popular consumer adapters
- ✅ **Bosch KTS** - Professional diagnostic system
- ✅ **Launch**, **Autel** - Professional scanners
- ✅ **Carista**, **Carly**, **OBDeleven** - OEM-specific coding tools
- ✅ **TOPDON**, **Foxwell**, **Ancel** - Advanced diagnostic adapters
- ✅ **UniCarScan**, **KONNWEI**, **ThinkDiag**, **Actron**

**Verbindungstypen / Connection Types:**
- 🔌 **Serial/USB** - COM port with auto-baud detection (9600-500000 baud)
- 📡 **Bluetooth** - Direct RFCOMM or virtual COM port
- 📶 **WiFi/Network** - TCP connections with extensive IP discovery

### Fahrzeughersteller / Vehicle Manufacturers

**45 unterstützte Hersteller in 8 Gruppen:**

- **VAG Gruppe**: Volkswagen, Audi, Skoda, Seat, Porsche, Lamborghini, Bentley
- **BMW Gruppe**: BMW, Mini, Rolls-Royce
- **Mercedes Gruppe**: Mercedes-Benz, Smart
- **Ford Gruppe**: Ford, Lincoln
- **Toyota Gruppe**: Toyota, Lexus, Daihatsu
- **GM Gruppe**: Opel, Chevrolet, Cadillac, Buick, GMC, Vauxhall
- **Stellantis PSA**: Peugeot, Citroën
- **Stellantis FCA**: Fiat, Alfa Romeo, Lancia, Jeep, Maserati
- **Renault Gruppe**: Renault, Dacia
- **Andere**: Hyundai, Kia, Nissan, Mazda, Honda, Mitsubishi, Subaru, Suzuki, Volvo, Jaguar, Land Rover

### Erweiterte Funktionen / Advanced Features

#### 🔍 Diagnose-Funktionen
- Standard OBD-II mit 100+ PIDs
- Erweiterte PIDs für Diesel, Turbo, Hybrid/Elektro
- J1939 Protokoll für Nutzfahrzeuge/LKW
- Herstellerspezifische PIDs (Toyota, VAG, BMW, Mercedes, Ford, GM)
- DTC (Fehlercode) Lesen und Löschen
- Freeze Frame Daten
- Live-Daten Streaming
- Readiness Monitors
- On-Board Tests

#### ⚙️ Herstellerspezifische Funktionen
- **VAG**: Codierung, Anpassungen, Messwertblöcke, 23+ ECUs
- **BMW**: FA/FDL Codierung, CBS Reset, 22+ ECUs
- **Mercedes**: Komponentenstatus, Service-Intervall Reset
- **Toyota**: Hybridbatterie-Diagnose, erweiterte Tests
- **Ford/GM**: Modul-Scanning, erweiterte DTCs

#### 🛠️ Spezialfunktionen
- EPB (Elektronische Parkbremse) Service
- DPF Regeneration
- Batterie-Registrierung
- ABS Entlüftung
- TPMS Registrierung
- Drosselklappen-Anpassung
- Getriebe-Anpassung Reset
- Injektoren Codierung
- AdBlue/DEF Reset
- Lenkwinkel-Kalibrierung

## 🔧 Installation

### Voraussetzungen / Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 8.0 Desktop Runtime**
- **OBD-Adapter** (siehe unterstützte Liste oben)
- **Fahrzeug** mit OBD-II Port (1996+ USA, 2001+ EU)

### Build-Anleitung / Build Instructions

```bash
# Repository klonen / Clone repository
git clone https://github.com/mrgomulus/At.git
cd At/OBD2Suite

# Projekt bauen / Build project
dotnet build --configuration Release

# Oder in Visual Studio öffnen / Or open in Visual Studio
# OBD2Suite.sln
```

## 📖 Verwendung / Usage

### 1. Adapter-Verbindung / Adapter Connection

#### Automatische Erkennung / Auto-Detection
```csharp
var scannerService = new AdapterScannerService();
var adapters = await scannerService.ScanAllAsync();

// Adapter auswählen / Select adapter
var selectedAdapter = adapters.First();
var connection = selectedAdapter.ToConnection();
```

#### Manuelle Verbindung / Manual Connection
```csharp
var connection = new ObdConnection
{
    Type = ConnectionType.Serial,
    PortName = "COM3",
    BaudRate = 38400  // Auto-detection available!
};

var obdService = new ObdService();
var success = await obdService.ConnectAsync(connection);
```

#### Mit Wiederholungslogik / With Retry Logic
```csharp
var retryHelper = new ConnectionRetryHelper(obdService, autoBaudDetector);
var policy = ConnectionRetryHelper.GetDefaultPolicy(ObdAdapterBrand.WOW);

var success = await retryHelper.ConnectWithRetryAsync(
    connection, 
    policy,
    onRetry: (attempt, ex) => Console.WriteLine($"Retry {attempt}: {ex.Message}")
);
```

### 2. Diagnose durchführen / Perform Diagnostics

#### Fehlercodes lesen / Read DTCs
```csharp
var dtcs = await obdService.ReadDtcsAsync();
foreach (var dtc in dtcs)
{
    Console.WriteLine($"{dtc.Code}: {dtc.Description}");
}
```

#### Live-Daten / Live Data
```csharp
var liveData = await obdService.ReadLiveDataAsync();
var rpm = liveData.FirstOrDefault(p => p.Name.Contains("RPM"));
var speed = liveData.FirstOrDefault(p => p.Name.Contains("Speed"));
```

#### Herstellerspezifische Diagnose / Manufacturer-Specific
```csharp
var manufacturer = Manufacturer.Volkswagen;
var mfrService = ManufacturerServiceFactory.Create(manufacturer, obdService);

// ECUs scannen / Scan ECUs
var ecus = await mfrService.ScanAllEcusAsync();

// Messwertblöcke lesen / Read measuring blocks
var blocks = await mfrService.ReadMeasuringBlockAsync(ecuAddress: 0x01, groupNumber: 1);
```

### 3. Verbindungsdiagnose / Connection Diagnostics

```csharp
// Gesundheitsprüfung / Health check
var diagnostic = await ConnectionDiagnostics.PerformHealthCheckAsync(obdService, connection);
var report = ConnectionDiagnostics.GenerateDiagnosticReport(diagnostic);
Console.WriteLine(report);

// Bei Verbindungsproblemen / On connection failure
try 
{
    await obdService.ConnectAsync(connection);
}
catch (Exception ex)
{
    var analysis = ConnectionDiagnostics.AnalyzeConnectionFailure(connection, ex);
    // Zeigt Lösungsvorschläge / Shows solution suggestions
    foreach (var suggestion in analysis.Suggestions)
    {
        Console.WriteLine($"• {suggestion}");
    }
}
```

## 🎯 Adapter-Fähigkeiten / Adapter Capabilities

Die Software nutzt adapter-spezifische Optimierungen:

### Auto-Baudrate-Erkennung / Auto-Baud Detection
```csharp
var detector = new AutoBaudDetector(obdService);
var result = await detector.DetectBaudRateAsync(connection, ObdAdapterBrand.WOW);

if (result.HasValue)
{
    Console.WriteLine($"Detected: {result.Value.BaudRate} baud");
    Console.WriteLine($"Version: {result.Value.ElmVersion}");
}
```

### Adapter-Eigenschaften / Adapter Characteristics
```csharp
// Empfohlene Baudraten / Recommended baud rates
var rates = AdapterCapabilities.GetRecommendedBaudRates(ObdAdapterBrand.WOW);
// → [38400, 115200, 230400]

// Unterstützt erweiterte Diagnose? / Supports advanced diagnostics?
bool advanced = AdapterCapabilities.SupportsAdvancedDiagnostics(ObdAdapterBrand.WOW);
// → true

// Empfohlener Timeout / Recommended timeout
int timeout = AdapterCapabilities.GetRecommendedTimeout(ObdAdapterBrand.WOW);
// → 3000 ms
```

## 📊 Unterstützte Protokolle / Supported Protocols

- **ISO 9141-2** (K-Line)
- **ISO 14230-4** (KWP2000)
- **ISO 15765-4** (CAN 11-bit & 29-bit)
- **SAE J1850 PWM**
- **SAE J1850 VPW**
- **SAE J1939** (Heavy Duty Vehicles/Trucks)

### Erweiterte PIDs / Extended PIDs

- **Standard OBD-II**: 100+ PIDs (Mode 01, 02, 03, 04, 06, 07, 09, 0A)
- **Diesel-spezifisch**: DPF, NOx, AdBlue/DEF, Abgastemperatur
- **Turbo-spezifisch**: Ladedruck, VGT, Wastegate, Ladeluftkühler
- **Hybrid/Elektro**: Batteriespannung/-strom/-temperatur, MG1/MG2 Drehzahl
- **Hersteller-spezifisch**: Toyota, VAG, BMW, Mercedes, Ford, GM PIDs
- **J1939 SPNs**: Motordrehzahl, Öldruck, Kraftstoffrate, 30+ Parameter

## 🔒 Sicherheit und Zuverlässigkeit / Security & Reliability

- **Verbindungs-Wiederholung**: Exponentielles Backoff bei Fehlern
- **Timeout-Verwaltung**: Adapter-spezifische Timeouts
- **Fehlerdiagnose**: Intelligente Fehlererkennung mit Lösungsvorschlägen
- **Gesundheitsprüfung**: Automatische Verbindungsprüfung
- **Adapter-Kompatibilität**: Prüfung der Fahrzeug-Adapter-Kompatibilität

## 🧪 Testing

```bash
# Unit Tests ausführen / Run unit tests
cd OBD2Suite.Tests
dotnet test

# Simulationsmodus für Tests / Simulation mode for testing
var obdService = new ObdService { IsSimulationMode = true };
```

**Test Coverage:**
- Adapter Scanner Tests (23 methods)
- OBD Service Tests (13 methods)
- Extended Services Tests (50+ methods)
- Manufacturer Service Tests
- Special Functions Tests

## 📁 Projektstruktur / Project Structure

```
OBD2Suite/
├── OBD2Suite/                  # Hauptprojekt / Main project
│   ├── Models/                 # Datenmodelle / Data models
│   │   ├── ObdConnection.cs    # Verbindungskonfiguration
│   │   ├── ObdAdapterInfo.cs   # Adapter-Metadaten (21 Marken)
│   │   ├── Manufacturer.cs     # 45 Hersteller
│   │   └── ...
│   ├── Services/               # Geschäftslogik / Business logic
│   │   ├── ObdService.cs       # Kern-OBD-Kommunikation
│   │   ├── AdapterScannerService.cs  # Adapter-Erkennung
│   │   ├── AutoBaudDetector.cs       # Auto-Baudrate
│   │   ├── ConnectionRetryHelper.cs  # Wiederholungslogik
│   │   ├── ConnectionDiagnostics.cs  # Diagnose-Hilfe
│   │   ├── AdapterCapabilities.cs    # Adapter-Fähigkeiten
│   │   ├── ElmProtocol.cs            # ELM327 Protokoll
│   │   ├── ExtendedPids.cs           # Erweiterte PIDs
│   │   ├── ManufacturerServiceFactory.cs
│   │   ├── VagService.cs       # VW/Audi/Skoda/Seat
│   │   ├── BmwService.cs       # BMW/Mini
│   │   ├── MercedesService.cs  # Mercedes/Smart
│   │   └── ...
│   ├── ViewModels/             # MVVM ViewModels
│   ├── Views/                  # WPF Views
│   └── Resources/              # Ressourcen
└── OBD2Suite.Tests/            # Unit Tests
```

## 🌟 Neue Funktionen in dieser Version / New Features

### ✨ Phase 1: Erweiterte Adapter-Unterstützung
- **21 Adapter-Marken** (vorher 12) inkl. WOW, UniCarScan, KONNWEI, Foxwell, etc.
- Erweiterte Netzwerk-Erkennung mit 13 IP-Adressen
- `AdapterCapabilities` Service für Marken-spezifische Optimierung
- `SupportedBaudRates` mit 8 Standard-Baudraten

### ✨ Phase 2: Verbindungsverbesserungen
- **AutoBaudDetector** - Intelligente Baudrate-Erkennung
- **ConnectionRetryHelper** - Wiederholung mit exponentiellem Backoff
- **ConnectionDiagnostics** - Fehleranalyse und Lösungsvorschläge
- Gesundheitsprüfung für aktive Verbindungen
- Adapter-Fahrzeug-Kompatibilitätsprüfung

### ✨ Phase 3: Erweiterte Fahrzeugunterstützung
- **50+ neue PIDs** für bessere Fahrzeugabdeckung
- **J1939 Protokoll** für Nutzfahrzeuge/LKW (30+ SPNs)
- Diesel-spezifische PIDs (DPF, NOx, AdBlue)
- Turbo-spezifische PIDs (Ladedruck, VGT, Wastegate)
- Hybrid/Elektro PIDs (Batterie, Inverter, MG1/MG2)
- Hersteller-spezifische PIDs (Toyota, VAG, BMW, Mercedes, Ford, GM)

## 🤝 Beitragen / Contributing

Beiträge sind willkommen! Bitte:
1. Forken Sie das Repository
2. Erstellen Sie einen Feature-Branch
3. Committen Sie Ihre Änderungen
4. Pushen Sie zum Branch
5. Öffnen Sie einen Pull Request

## 📄 Lizenz / License

Dieses Projekt ist unter der MIT-Lizenz lizenziert - siehe LICENSE-Datei für Details.

## 🐛 Bekannte Einschränkungen / Known Limitations

- **Windows-spezifisch**: Bluetooth und serielle Adapter erfordern Windows
- **WPF-Abhängigkeit**: Desktop Runtime erforderlich
- **Simulationsmodus**: Einige OEM-Funktionen nur simuliert
- **Hardware-Tests**: Unit Tests laufen ohne echte Hardware

## 📞 Support

- **Issues**: GitHub Issues für Bug-Reports und Feature-Requests
- **Dokumentation**: Siehe `/docs` Ordner für Details
- **Projekt-Website**: https://github.com/mrgomulus/At

## 🙏 Danksagungen / Acknowledgments

- ELM327 Spezifikation und Protokoll-Dokumentation
- OBD-II PIDs Standardisierung (SAE J1979)
- Herstellerspezifische Protokoll-Dokumentationen
- Open-Source OBD-Community

---

**Hinweis**: Diese Software ist für Diagnose- und Informationszwecke. Ändern Sie ECU-Einstellungen nur, wenn Sie wissen, was Sie tun. Unsachgemäße Verwendung kann Fahrzeugschäden verursachen.

**Note**: This software is for diagnostic and informational purposes. Only modify ECU settings if you know what you're doing. Improper use can damage your vehicle.
