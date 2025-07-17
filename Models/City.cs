using System.Collections.Generic;
using Newtonsoft.Json;

namespace KOÜ_Ulaşım.Models
{
    public class TaxiInfo
    {
        [JsonProperty("openingFee")]
        public double AcilisUcreti { get; set; } = 15.0;

        [JsonProperty("costPerKm")]
        public double KmBasinaUcret { get; set; } = 8.5;
    }

    public class City
    {
        public City()
        {
            Name = string.Empty;
            Taxi = new TaxiInfo();
            Duraklar = new List<Durak>();
        }

        [JsonProperty("city")]
        public string Name { get; set; }

        [JsonProperty("taxi")]
        public TaxiInfo Taxi { get; set; }

        [JsonProperty("duraklar")]
        public List<Durak> Duraklar { get; set; }
    }
}
