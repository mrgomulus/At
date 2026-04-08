namespace OBD2Suite.Services
{
    /// <summary>
    /// Decode a 17-character VIN (ISO 3779) into all meaningful sub-fields.
    /// Uses a built-in WMI (World Manufacturer Identifier) database.
    /// </summary>
    public static class VinDecoderService
    {
        public static VinDecodeResult Decode(string vin)
        {
            var r = new VinDecodeResult { Vin = vin.ToUpperInvariant() };
            if (vin.Length != 17)
            {
                r.IsValid = false;
                r.Error = "VIN must be exactly 17 characters";
                return r;
            }
            if (!IsChecksumValid(r.Vin))
            {
                r.IsValid = false;
                r.Error = "VIN check digit failed (position 9)";
            }
            r.Wmi       = r.Vin.Substring(0, 3);
            r.Vds       = r.Vin.Substring(3, 6);
            r.Vis       = r.Vin.Substring(9, 8);
            r.CheckDigit = r.Vin[8];
            r.Manufacturer = LookupWmi(r.Wmi);
            r.ModelYear    = DecodeModelYear(r.Vin[9]);
            r.PlantCode    = r.Vin[10].ToString();
            r.PlantName    = LookupPlant(r.Wmi, r.Vin[10]);
            r.SerialNumber = r.Vin.Substring(11, 6);
            r.IsValid = true;
            return r;
        }

        // ─── Check digit ────────────────────────────────────────────────────
        private static bool IsChecksumValid(string vin)
        {
            const string transliteration = "0123456789.ABCDEFGH..JKLMN.P.R..STUVWXYZ";
            int[] weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };
            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                int val = transliteration.IndexOf(vin[i]);
                if (val < 0) return false;
                sum += val * weights[i];
            }
            int rem = sum % 11;
            return vin[8] == (rem == 10 ? 'X' : (char)('0' + rem));
        }

        // ─── Model Year decode ──────────────────────────────────────────────
        private static string DecodeModelYear(char c) => c switch
        {
            'A' => "1980", 'B' => "1981", 'C' => "1982", 'D' => "1983", 'E' => "1984",
            'F' => "1985", 'G' => "1986", 'H' => "1987", 'J' => "1988", 'K' => "1989",
            'L' => "1990", 'M' => "1991", 'N' => "1992", 'P' => "1993", 'R' => "1994",
            'S' => "1995", 'T' => "1996", 'V' => "1997", 'W' => "1998", 'X' => "1999",
            'Y' => "2000",
            '1' => "2001", '2' => "2002", '3' => "2003", '4' => "2004", '5' => "2005",
            '6' => "2006", '7' => "2007", '8' => "2008", '9' => "2009",
            _ when c >= 'A' && c <= 'Z' => $"2010+({c})",
            _ => c.ToString()
        };

        // ─── Plant lookup ─────────────────────────────────────────────────
        private static string LookupPlant(string wmi, char plantCode) =>
            (wmi.Substring(0, 2), plantCode) switch
            {
                ("WVW", 'E') => "Emden, Germany",
                ("WVW", 'K') => "Osnabruck, Germany",
                ("WVW", 'W') => "Wolfsburg, Germany",
                ("WAU", 'N') => "Neckarsulm, Germany",
                ("WAU", 'I') => "Ingolstadt, Germany",
                ("WBA", 'A') => "Dingolfing, Germany",
                ("WBA", 'R') => "Regensburg, Germany",
                ("WBA", 'M') => "Munich, Germany",
                ("WDD", 'A') => "Sindelfingen, Germany",
                ("WDD", 'B') => "Bremen, Germany",
                ("1HG", 'A') => "Alliston, Canada",
                ("1HG", 'E') => "East Liberty, Ohio, USA",
                ("JHM", 'Y') => "Sayama, Japan",
                ("JT2", 'F') => "Fuji, Japan",
                ("1G1", 'A') => "Atlanta, Georgia, USA",
                ("KNA", 'G') => "Gwangju, Korea",
                ("KMH", 'U') => "Ulsan, Korea",
                _ => plantCode.ToString()
            };

        // ─── WMI database ─────────────────────────────────────────────────
        private static string LookupWmi(string wmi) =>
            _wmiDb.TryGetValue(wmi, out var name) ? name : $"Unknown ({wmi})";

        private static readonly Dictionary<string, string> _wmiDb = new(StringComparer.OrdinalIgnoreCase)
        {
            // Germany
            {"WVW","Volkswagen (Germany)"}, {"WV2","Volkswagen Commercial"}, {"WV3","Volkswagen Truck"},
            {"WAU","Audi (Ingolstadt)"}, {"TRU","Audi (Hungary)"}, {"WUA","Audi Sport (Neckarsulm)"},
            {"TMB","Škoda (Mlada Boleslav)"}, {"VSS","SEAT (Spain)"},
            {"WBA","BMW (Germany)"}, {"WBS","BMW M GmbH"}, {"WBY","BMW i"},
            {"WDD","Mercedes-Benz (Sindelfingen)"}, {"WDB","Mercedes-Benz (Bremen)"},
            {"WDC","Mercedes-Benz C-Class"}, {"WDF","Mercedes-Benz (Daimler)"},
            {"WMX","Mercedes-AMG"}, {"SAR","Rover/Land Rover"},
            {"WP0","Porsche (Zuffenhausen)"}, {"WP1","Porsche Cayenne"},
            {"ZFF","Ferrari"}, {"ZHW","Lamborghini"},
            {"WF0","Ford (Germany)"}, {"WFO","Ford Otosan (Turkey)"},
            {"SAL","Land Rover (UK)"}, {"SAJ","Jaguar (UK)"},
            {"SAB","Austin Rover"}, {"SCC","Lotus Cars"},
            {"VF1","Renault (Flins)"}, {"VF3","Peugeot"}, {"VF7","Citroën"},
            {"VF6","Renault Trucks"}, {"VF2","Renault Sandouville"},
            {"VS6","Ford España"}, {"VNK","Toyota Motor España"},
            {"VS7","Citroën Vigo"}, {"VS9","Renault Spain"},
            {"ZAR","Alfa Romeo"}, {"ZFA","Fiat (Italy)"}, {"ZFF","Ferrari (Maranello)"},
            {"ZAM","Maserati"}, {"ZCF","Iveco"}, {"ZLA","Lancia"},
            // Japan
            {"JHM","Honda (Japan)"}, {"JH4","Acura (Japan)"},
            {"JT2","Toyota (Tsutsumi)"}, {"JT3","Toyota (Takaoka)"},
            {"JTN","Toyota (Japan)"}, {"JTM","Toyota (Japan)"},
            {"JN1","Nissan (Japan)"}, {"JN6","Nissan Truck"},
            {"JA3","Mitsubishi (Japan)"}, {"JA4","Mitsubishi SUV"},
            {"JS1","Suzuki"}, {"JS2","Suzuki"},
            {"JMB","Mitsubishi"},
            {"JYA","Yamaha Motor"},
            // South Korea
            {"KNA","Kia (Korea)"}, {"KNB","Kia (Slovakia)"},
            {"KMH","Hyundai (Korea)"}, {"KM8","Hyundai SUV"},
            {"5NP","Hyundai (USA)"}, {"KMA","Kia"},
            // USA
            {"1FA","Ford (Wayne, MI)"}, {"1FB","Ford (Claycomo)"}, {"1FC","Ford"},
            {"1FD","Ford Truck"}, {"1FM","Ford SUV (Explorer)"}, {"1FT","Ford F-Series"},
            {"1G1","Chevrolet"}, {"1G2","Pontiac"}, {"1G3","Oldsmobile"}, {"1G4","Buick"},
            {"1G6","Cadillac"}, {"1GB","GMC Truck"}, {"1GC","Chevrolet/GMC Pickup"},
            {"2G1","Chevrolet Canada"}, {"2G2","Pontiac Canada"},
            {"1C3","Chrysler"}, {"1C4","Chrysler SUV"}, {"2C3","Chrysler Canada"},
            {"1B3","Dodge (car)"}, {"1B4","Dodge SUV"}, {"1B7","Dodge Pickup"},
            {"1HG","Honda (Alliston)"}, {"19X","Honda (Indiana)"},
            {"4T1","Toyota (Georgetown, KY)"}, {"4T3","Toyota (Indiana)"},
            {"5T","Toyota"}, {"5YJ","Tesla"},
            {"19U","Acura"}, {"JA4","Mitsubishi USA"},
            {"WBS","BMW M GmbH"},
            // China
            {"LVV","Volvo China"}, {"LSG","GM China (SAIC-GM)"},
            {"LFV","Volkswagen FAW China"}, {"LFG","SAIC VW"},
            {"LDC","Dongfeng Nissan"}, {"LGX","SAIC-GM Wuling"},
            // Sweden/UK
            {"YV1","Volvo Cars"}, {"YV2","Volvo Trucks"}, {"YV4","Volvo SUV"},
            {"SAL","Land Rover"}, {"SAJ","Jaguar"},
            // India
            {"MA1","Mahindra"}, {"MAR","Maruti Suzuki"}, {"MEE","BMW India"},
            // Others
            {"2T1","Toyota (Canada)"}, {"3VW","Volkswagen (Mexico)"},
            {"3HG","Honda (Mexico)"}, {"3FA","Ford (Mexico)"},
        };
    }

    public class VinDecodeResult
    {
        public string Vin          { get; set; } = "";
        public bool   IsValid      { get; set; } = true;
        public string Error        { get; set; } = "";
        public string Wmi          { get; set; } = "";   // pos 1-3
        public string Vds          { get; set; } = "";   // pos 4-9
        public string Vis          { get; set; } = "";   // pos 10-17
        public char   CheckDigit   { get; set; }         // pos 9
        public string Manufacturer { get; set; } = "";
        public string ModelYear    { get; set; } = "";
        public string PlantCode    { get; set; } = "";
        public string PlantName    { get; set; } = "";
        public string SerialNumber { get; set; } = "";

        public List<(string Field, string Value)> ToDisplayList() => new()
        {
            ("Full VIN",          Vin),
            ("Valid",             IsValid ? "✔ Yes" : $"✖ No — {Error}"),
            ("WMI (World Manuf.)",Wmi),
            ("Manufacturer",      Manufacturer),
            ("VDS (Vehicle Desc)",Vds),
            ("VIS (Vehicle ID)",  Vis),
            ("Check Digit (pos 9)", CheckDigit.ToString()),
            ("Model Year",        ModelYear),
            ("Assembly Plant",    $"{PlantCode} — {PlantName}"),
            ("Serial Number",     SerialNumber),
        };
    }
}
