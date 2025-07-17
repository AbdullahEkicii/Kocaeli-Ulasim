using System.Collections.Generic;
using Newtonsoft.Json;

namespace KOÜ_Ulaşım.Models
{
    public class DurakBaglanti
    {
        public DurakBaglanti()
        {
            StopId = string.Empty;
        }

        [JsonProperty("stopId")]
        public string StopId { get; set; }

        [JsonProperty("mesafe")]
        public double Mesafe { get; set; }

        [JsonProperty("sure")]
        public int Sure { get; set; }

        [JsonProperty("ucret")]
        public double Ucret { get; set; }
    }

    public class TransferInfo
    {
        public TransferInfo()
        {
            TransferStopId = string.Empty;
        }

        [JsonProperty("transferStopId")]
        public string TransferStopId { get; set; }

        [JsonProperty("transferSure")]
        public int TransferSure { get; set; }

        [JsonProperty("transferUcret")]
        public double TransferUcret { get; set; }
    }

    public class Durak
    {
        public Durak()
        {
            Id = string.Empty;
            Name = string.Empty;
            Type = string.Empty;
            NextStops = new List<DurakBaglanti>();
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lon")]
        public double Lon { get; set; }

        [JsonProperty("sonDurak")]
        public bool SonDurak { get; set; }

        [JsonProperty("nextStops")]
        public List<DurakBaglanti> NextStops { get; set; }

        [JsonProperty("transfer")]
        public TransferInfo? Transfer { get; set; }

        // Dahili kullanım için özellikler (JSON'a dahil edilmeyecek)
        [JsonIgnore]
        public double Mesafe { get; set; }

        [JsonIgnore]
        public int Sure { get; set; }
    }
}
