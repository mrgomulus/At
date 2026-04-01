namespace OBD2Suite.Models
{
    public enum Manufacturer
    {
        Generic = 0,
        // VAG Group
        Volkswagen,
        Audi,
        Skoda,
        Seat,
        Porsche,
        Lamborghini,
        Bentley,
        // BMW Group
        BMW,
        Mini,
        RollsRoyce,
        // Mercedes-Benz Group
        Mercedes,
        Smart,
        // Ford Group
        Ford,
        Lincoln,
        // Toyota Group
        Toyota,
        Lexus,
        Daihatsu,
        // GM Group
        Opel,
        Chevrolet,
        Cadillac,
        Buick,
        GMC,
        Vauxhall,
        // PSA Group (Stellantis)
        Peugeot,
        Citroen,
        Fiat,
        AlfaRomeo,
        Lancia,
        Jeep,
        Maserati,
        // Renault Group
        Renault,
        Dacia,
        // Other
        Hyundai,
        Kia,
        Nissan,
        Mazda,
        Honda,
        Mitsubishi,
        Subaru,
        Suzuki,
        Volvo,
        Jaguar,
        LandRover
    }

    public static class ManufacturerHelper
    {
        public static string GetGroupName(Manufacturer m) => m switch
        {
            Manufacturer.Volkswagen or Manufacturer.Audi or Manufacturer.Skoda
                or Manufacturer.Seat or Manufacturer.Porsche or Manufacturer.Lamborghini
                or Manufacturer.Bentley => "VAG Group",
            Manufacturer.BMW or Manufacturer.Mini or Manufacturer.RollsRoyce => "BMW Group",
            Manufacturer.Mercedes or Manufacturer.Smart => "Mercedes-Benz Group",
            Manufacturer.Ford or Manufacturer.Lincoln => "Ford Group",
            Manufacturer.Toyota or Manufacturer.Lexus or Manufacturer.Daihatsu => "Toyota Group",
            Manufacturer.Opel or Manufacturer.Chevrolet or Manufacturer.Cadillac
                or Manufacturer.Buick or Manufacturer.GMC or Manufacturer.Vauxhall => "GM Group",
            Manufacturer.Peugeot or Manufacturer.Citroen or Manufacturer.Fiat
                or Manufacturer.AlfaRomeo or Manufacturer.Lancia or Manufacturer.Jeep
                or Manufacturer.Maserati => "Stellantis",
            Manufacturer.Renault or Manufacturer.Dacia => "Renault Group",
            _ => "Other"
        };

        public static string GetDefaultProtocol(Manufacturer m) => m switch
        {
            Manufacturer.Volkswagen or Manufacturer.Audi or Manufacturer.Skoda
                or Manufacturer.Seat => "KWP2000 / UDS on CAN (ISO 15765)",
            Manufacturer.BMW => "D-CAN (ISO 15765)",
            Manufacturer.Mercedes => "CAN / SDI",
            Manufacturer.Ford => "UDS / ISO 15765",
            Manufacturer.Toyota or Manufacturer.Lexus => "Toyota Enhanced OBD2",
            Manufacturer.Opel or Manufacturer.Chevrolet => "GM-LAN / ISO 15765",
            _ => "ISO 15765-4 (CAN)"
        };
    }
}
