using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Factory for creating the correct manufacturer-specific service instance
    /// based on the selected manufacturer.
    /// </summary>
    public static class ManufacturerServiceFactory
    {
        public static IManufacturerService Create(Manufacturer manufacturer, IObdService obdService)
        {
            return manufacturer switch
            {
                // VAG Group
                Manufacturer.Volkswagen => new VagService(obdService, manufacturer),
                Manufacturer.Audi       => new VagService(obdService, manufacturer),
                Manufacturer.Skoda      => new VagService(obdService, manufacturer),
                Manufacturer.Seat       => new VagService(obdService, manufacturer),
                Manufacturer.Porsche    => new VagService(obdService, manufacturer),
                Manufacturer.Lamborghini=> new VagService(obdService, manufacturer),
                Manufacturer.Bentley    => new VagService(obdService, manufacturer),

                // BMW Group
                Manufacturer.BMW        => new BmwService(obdService, manufacturer),
                Manufacturer.Mini       => new BmwService(obdService, manufacturer),
                Manufacturer.RollsRoyce => new BmwService(obdService, manufacturer),

                // Mercedes Group
                Manufacturer.Mercedes   => new MercedesService(obdService, manufacturer),
                Manufacturer.Smart      => new MercedesService(obdService, manufacturer),

                // Ford Group
                Manufacturer.Ford       => new FordService(obdService, manufacturer),
                Manufacturer.Lincoln    => new FordService(obdService, manufacturer),

                // Toyota Group
                Manufacturer.Toyota     => new ToyotaService(obdService, manufacturer),
                Manufacturer.Lexus      => new ToyotaService(obdService, manufacturer),
                Manufacturer.Daihatsu   => new ToyotaService(obdService, manufacturer),

                // GM Group
                Manufacturer.Opel       => new GmService(obdService, manufacturer),
                Manufacturer.Vauxhall   => new GmService(obdService, manufacturer),
                Manufacturer.Chevrolet  => new GmService(obdService, manufacturer),
                Manufacturer.Cadillac   => new GmService(obdService, manufacturer),
                Manufacturer.Buick      => new GmService(obdService, manufacturer),
                Manufacturer.GMC        => new GmService(obdService, manufacturer),

                // Stellantis — PSA
                Manufacturer.Peugeot    => new PsaService(obdService, manufacturer),
                Manufacturer.Citroen    => new PsaService(obdService, manufacturer),

                // Stellantis — FCA
                Manufacturer.Fiat       => new FiatService(obdService, manufacturer),
                Manufacturer.AlfaRomeo  => new FiatService(obdService, manufacturer),
                Manufacturer.Lancia     => new FiatService(obdService, manufacturer),
                Manufacturer.Jeep       => new FiatService(obdService, manufacturer),
                Manufacturer.Maserati   => new FiatService(obdService, manufacturer),

                // Renault Group
                Manufacturer.Renault    => new RenaultService(obdService, manufacturer),
                Manufacturer.Dacia      => new RenaultService(obdService, manufacturer),

                // All others fall back to generic (standard OBD2 only)
                _ => new GenericManufacturerService(obdService, manufacturer)
            };
        }

        /// <summary>Returns all supported manufacturers grouped by OEM group.</summary>
        public static IEnumerable<(string Group, Manufacturer[] Members)> GetManufacturerGroups() =>
        new[]
        {
            ("VAG Group",       new[] { Manufacturer.Volkswagen, Manufacturer.Audi, Manufacturer.Skoda, Manufacturer.Seat, Manufacturer.Porsche, Manufacturer.Lamborghini, Manufacturer.Bentley }),
            ("BMW Group",       new[] { Manufacturer.BMW, Manufacturer.Mini, Manufacturer.RollsRoyce }),
            ("Mercedes Group",  new[] { Manufacturer.Mercedes, Manufacturer.Smart }),
            ("Ford Group",      new[] { Manufacturer.Ford, Manufacturer.Lincoln }),
            ("Toyota Group",    new[] { Manufacturer.Toyota, Manufacturer.Lexus, Manufacturer.Daihatsu }),
            ("GM Group",        new[] { Manufacturer.Opel, Manufacturer.Vauxhall, Manufacturer.Chevrolet, Manufacturer.Cadillac, Manufacturer.Buick, Manufacturer.GMC }),
            ("Stellantis PSA",  new[] { Manufacturer.Peugeot, Manufacturer.Citroen }),
            ("Stellantis FCA",  new[] { Manufacturer.Fiat, Manufacturer.AlfaRomeo, Manufacturer.Lancia, Manufacturer.Jeep, Manufacturer.Maserati }),
            ("Renault Group",   new[] { Manufacturer.Renault, Manufacturer.Dacia }),
            ("Other",           new[] { Manufacturer.Hyundai, Manufacturer.Kia, Manufacturer.Nissan, Manufacturer.Mazda, Manufacturer.Honda, Manufacturer.Mitsubishi, Manufacturer.Subaru, Manufacturer.Suzuki, Manufacturer.Volvo, Manufacturer.Jaguar, Manufacturer.LandRover }),
        };
    }

    /// <summary>
    /// Generic manufacturer service — standard OBD2 only, used as fallback.
    /// </summary>
    public class GenericManufacturerService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer == Models.Manufacturer.Generic ? "Generic OBD2" : _manufacturer.ToString();

        public GenericManufacturerService(IObdService obdService, Manufacturer manufacturer = Models.Manufacturer.Generic)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList() => new()
        {
            new EcuModule { Address = 0x7E0, Name = "Engine Control Module",         Protocol = "ISO 15765-4", PartNumber = "0000000", SoftwareVersion = "01.00", IsReachable = true },
            new EcuModule { Address = 0x7E1, Name = "Transmission Control Module",   Protocol = "ISO 15765-4", PartNumber = "0000001", SoftwareVersion = "01.00", IsReachable = true },
            new EcuModule { Address = 0x7A0, Name = "ABS / Brake Control Module",    Protocol = "ISO 15765-4", PartNumber = "0000002", SoftwareVersion = "01.00", IsReachable = true },
            new EcuModule { Address = 0x720, Name = "Airbag / SRS Module",           Protocol = "ISO 15765-4", PartNumber = "0000003", SoftwareVersion = "01.00", IsReachable = true },
        };
    }
}
