using System.IO;
using KOÜ_Ulaşım.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;

namespace KOÜ_Ulaşım.Services
{
    public static class VeriServisi
    {
        private static City _sehirVerisi = new();
        private static readonly string _jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "veriseti.json");
        private static readonly object _lockObject = new object();
        private static DateTime _sonYüklemeTarihi = DateTime.MinValue;
        private static readonly TimeSpan _yenidenYüklemeAralığı = TimeSpan.FromMinutes(5); // 5 dakikada bir yeniden yükle

        static VeriServisi()
        {
            // Başlangıçta veriyi yükle
            YükleVeri();
        }

        public static void YükleVeri()
        {
            try
            {
                lock (_lockObject)
                {
                    // Dosya var mı kontrol et
                    if (!File.Exists(_jsonPath))
                    {
                        Console.WriteLine($"UYARI: Veriseti dosyası bulunamadı: {_jsonPath}");
                        _sehirVerisi = new City
                        {
                            Name = "Kocaeli",
                            Duraklar = BaslangicVerisiniOlustur()
                        };
                        return;
                    }

                    // Dosya değişiklik zamanını kontrol et
                    var dosyaZamanı = File.GetLastWriteTime(_jsonPath);
                    if (dosyaZamanı <= _sonYüklemeTarihi && DateTime.Now - _sonYüklemeTarihi < _yenidenYüklemeAralığı)
                    {
                        // Dosya değişmemiş ve son yüklemeden beri yeterli zaman geçmemiş
                        return;
                    }

                    Console.WriteLine($"JSON verisi yükleniyor: {_jsonPath}");
                    string json = File.ReadAllText(_jsonPath);
                    var yeniVeri = JsonConvert.DeserializeObject<City>(json);
                    
                    if (yeniVeri != null && yeniVeri.Duraklar != null && yeniVeri.Duraklar.Any())
                    {
                        _sehirVerisi = yeniVeri;
                        _sonYüklemeTarihi = DateTime.Now;

                        Console.WriteLine($"Durak sayısı: {_sehirVerisi.Duraklar.Count}");
                        Console.WriteLine($"Şehir adı: {_sehirVerisi.Name}");

                        // Her durağın Id değeri olduğunu kontrol et
                        foreach (var durak in _sehirVerisi.Duraklar)
                        {
                            if (string.IsNullOrEmpty(durak.Id))
                            {
                                // ID yok ise name ve type kullanarak oluştur
                                durak.Id = $"{durak.Type.ToLower()}_{RemoveSpaces(durak.Name.ToLower())}";
                                Console.WriteLine($"Durak için ID oluşturuldu: {durak.Id}");
                            }

                            // Next stops listesini kontrol et
                            if (durak.NextStops == null)
                            {
                                durak.NextStops = new List<DurakBaglanti>();
                            }

                            // Log bilgisi
                            Console.WriteLine($"Durak: {durak.Name}, Tür: {durak.Type}, ID: {durak.Id}, Konum: {durak.Lat}, {durak.Lon}");

                            // Bağlantıları kontrol et
                            if (durak.NextStops.Any())
                            {
                                foreach (var baglanti in durak.NextStops)
                                {
                                    Console.WriteLine($"  -> {baglanti.StopId}, Mesafe: {baglanti.Mesafe} km, Süre: {baglanti.Sure} dk, Ücret: {baglanti.Ucret} TL");
                                }
                            }
                            else
                            {
                                Console.WriteLine("  -> Bağlantı yok");
                            }

                            // Transfer bilgisi
                            if (durak.Transfer != null)
                            {
                                Console.WriteLine($"  Transfer -> {durak.Transfer.TransferStopId}, Süre: {durak.Transfer.TransferSure} dk, Ücret: {durak.Transfer.TransferUcret} TL");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("JSON verisinde geçerli durak bilgileri bulunamadı, varsayılan veriler kullanılacak.");
                        _sehirVerisi = new City
                        {
                            Name = "Kocaeli",
                            Duraklar = BaslangicVerisiniOlustur()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Veri yükleme hatası: {ex.Message}");
                Console.WriteLine($"Hata ayrıntıları: {ex.StackTrace}");
                
                // Hata durumunda varsayılan verileri kullan
                _sehirVerisi = new City
                {
                    Name = "Kocaeli",
                    Duraklar = BaslangicVerisiniOlustur()
                };
            }
        }

        private static string RemoveSpaces(string text)
        {
            return text.Replace(" ", "").Replace("(", "").Replace(")", "");
        }

        public static City GetSehirVerisi()
        {
            // Veri dosyasını periyodik olarak kontrol et
            if (DateTime.Now - _sonYüklemeTarihi > _yenidenYüklemeAralığı)
            {
                YükleVeri();
            }
            return _sehirVerisi;
        }

        public static Durak? EnYakinDurak(double lat, double lon)
        {
            return TumDuraklariGetir()
                .OrderBy(d => Math.Sqrt(Math.Pow(d.Lat - lat, 2) + Math.Pow(d.Lon - lon, 2)))
                .FirstOrDefault();
        }

        public static List<Durak> TumDuraklariGetir()
        {
            // Veriyi yükle (ilk kez veya güncelleme)
            YükleVeri();
            
            // Durakları döndür
            return _sehirVerisi?.Duraklar ?? new List<Durak>();
        }

        private static List<Durak> BaslangicVerisiniOlustur()
        {
            Console.WriteLine("Varsayılan durak verileri oluşturuluyor...");
            return new List<Durak>
            {
                new Durak { Id = "bus_otogar", Name = "Otogar", Type = "BUS", Lat = 40.78259, Lon = 29.94628 },
                new Durak { Id = "bus_sekapark", Name = "Sekapark", Type = "BUS", Lat = 40.76520, Lon = 29.96190 },
                new Durak { Id = "bus_yahyakaptan", Name = "Yahya Kaptan", Type = "BUS", Lat = 40.770965, Lon = 29.959499 },
                new Durak { Id = "bus_umuttepe", Name = "Umuttepe", Type = "BUS", Lat = 40.82103, Lon = 29.91843 },
                new Durak { Id = "bus_symbolavm", Name = "Symbol AVM", Type = "BUS", Lat = 40.77788, Lon = 29.94991 },
                new Durak { Id = "bus_41burda", Name = "41 Burda AVM", Type = "BUS", Lat = 40.77731, Lon = 29.92512 },
                new Durak { Id = "tram_otogar", Name = "Otogar", Type = "TRAM", Lat = 40.78245, Lon = 29.94610 },
                new Durak { Id = "tram_yahyakaptan", Name = "Yahya Kaptan", Type = "TRAM", Lat = 40.77160, Lon = 29.96010 },
                new Durak { Id = "tram_sekapark", Name = "Sekapark", Type = "TRAM", Lat = 40.76200, Lon = 29.96550 },
                new Durak { Id = "tram_halkevi", Name = "Halkevi", Type = "TRAM", Lat = 40.76350, Lon = 29.93870 }
            };
        }
    }
}
