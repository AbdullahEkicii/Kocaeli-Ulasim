using System;
using System.Collections.Generic;
using System.Linq;
using KOÜ_Ulaşım.Models;

namespace KOÜ_Ulaşım.Services { 
public static class RotaServisi
{
    public const double OTOBUS_UCRETI = 7.5;
    public const double TRAMVAY_UCRETI = 7.5;
    public const double TRANSFER_INDIRIMI = 2.5;

    // Haversine formülü kullanarak iki nokta arasındaki mesafeyi hesaplar
    private static double HesaplaMesafe(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Dünya yarıçapı (km)
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double degree)
    {
        return degree * Math.PI / 180;
    }

    public static Durak? EnYakinDurak(double kullaniciLat, double kullaniciLon)
    {
        var duraklar = VeriServisi.GetSehirVerisi()?.Duraklar;
        if (duraklar == null || !duraklar.Any())
        {
            return null;
        }
        return duraklar.OrderBy(d => HesaplaMesafe(kullaniciLat, kullaniciLon, d.Lat, d.Lon)).First();
    }

    /// <summary>
    /// İki durak arasındaki rotayı hesaplar
    /// </summary>
    /// <param name="baslangicDurak">Başlangıç durağı</param>
    /// <param name="hedefDurak">Hedef durağı</param>
    /// <param name="yolcu">Yolcu bilgisi</param>
    /// <param name="tramvayOncelikli">Tramvay öncelikli hesaplama yapılsın mı</param>
    /// <param name="otobusOncelikli">Otobüs öncelikli hesaplama yapılsın mı</param>
    /// <param name="aktarmaIzin">Aktarmalara izin verilsin mi</param>
    /// <returns>Rota ve detaylı rota bilgisi</returns>
    public static (List<string> rota, List<string> detayliRota) RotaHesapla(
        Durak baslangicDurak, 
        Durak hedefDurak, 
        Yolcu yolcu, 
        bool tramvayOncelikli = false,
        bool otobusOncelikli = false,
        bool aktarmaIzin = true)
    {
        try
        {
            Console.WriteLine($"RotaServisi.RotaHesapla başladı: {baslangicDurak?.Name} -> {hedefDurak?.Name}, " +
                  $"Tramvay öncelikli: {tramvayOncelikli}, Otobüs öncelikli: {otobusOncelikli}, Aktarma izin: {aktarmaIzin}");
            
            if (baslangicDurak == null || hedefDurak == null)
            {
                Console.WriteLine("HATA: Başlangıç veya hedef durak null!");
                return (new List<string>(), new List<string>());
            }

            // Sadece otobüs öncelikli ise
            if (otobusOncelikli)
            {
                var otobusRotasi = SadeceOtobusRotasiHesapla(baslangicDurak, hedefDurak, yolcu);
                if (otobusRotasi != null && otobusRotasi.Count > 0)
                {
                    var detayliRota = new List<string>
                    {
                        $"Başlangıç noktası: {baslangicDurak.Name} durağı"
                    };
                    
                    for (int i = 0; i < otobusRotasi.Count; i++)
                    {
                        if (i > 0)
                        {
                            detayliRota.Add($"Otobüs ile {otobusRotasi[i]} durağına gidin");
                        }
                    }
                    
                    detayliRota.Add($"Varış noktası: {hedefDurak.Name} durağı");
                    
                    Console.WriteLine($"Otobüs öncelikli rota bulundu, durak sayısı: {otobusRotasi.Count}");
                    return (otobusRotasi, detayliRota);
                }
            }
            
            // Tramvay öncelikli ise ve aktarma izni varsa veya tramvay durağından başlıyorsak
            if (tramvayOncelikli && (aktarmaIzin || baslangicDurak.Type == "TRAM"))
            {
                // Tramvay öncelikli rota hesapla
                var tramvayRotasi = TramvayOncelikliRotaHesapla(baslangicDurak, hedefDurak, yolcu);
                if (tramvayRotasi != null && tramvayRotasi.Count > 0)
                {
                    var detayliRota = new List<string>
                    {
                        $"Başlangıç noktası: {baslangicDurak.Name} durağı"
                    };
                    
                    string oncekiDurakTipi = null;
                    for (int i = 0; i < tramvayRotasi.Count; i++)
                    {
                        var durak = VeriServisi.TumDuraklariGetir().FirstOrDefault(d => d.Name == tramvayRotasi[i]);
                        if (durak == null) continue;
                        
                        if (i > 0)
                        {
                            var oncekiDurak = VeriServisi.TumDuraklariGetir().FirstOrDefault(d => d.Name == tramvayRotasi[i - 1]);
                            if (oncekiDurak == null) continue;
                            
                            if (oncekiDurak.Type != durak.Type)
                            {
                                // Aktarma izni yoksa ve farklı tipteki duraklara geçiş varsa, bu rota uygun değil
                                if (!aktarmaIzin)
                                {
                                    Console.WriteLine("Aktarma izni olmadığı için tramvay rotası uygun değil");
                                    break;
                                }
                                
                                detayliRota.Add($"Transfer: {oncekiDurak.Name} durağından {durak.Name} durağına yürüyerek geçin");
                            }
                            
                            if (durak.Type == "BUS" || durak.Type == "TRAM")
                            {
                                if (oncekiDurakTipi != durak.Type)
                                {
                                    detayliRota.Add($"{durak.Type} durağı {durak.Name}'e ulaştınız. Buradan {(durak.Type == "BUS" ? "otobüs" : "tramvay")} ile devam edin.");
                                }
                            }
                        }
                        
                        oncekiDurakTipi = durak.Type;
                    }
                    
                    detayliRota.Add($"Varış noktası: {hedefDurak.Name} durağı");
                    
                    Console.WriteLine($"Tramvay öncelikli rota bulundu, durak sayısı: {tramvayRotasi.Count}");
                    return (tramvayRotasi, detayliRota);
                }
            }
            
            // Varsayılan rota hesaplama
            var duraklar = VeriServisi.TumDuraklariGetir();
            if (duraklar == null || !duraklar.Any())
            {
                Console.WriteLine("HATA: Durak listesi boş!");
                return (new List<string>(), new List<string>());
            }
            
            // Aktarma izni yoksa, sadece aynı tipteki durakları seç
            if (!aktarmaIzin)
            {
                string durakTipi = baslangicDurak.Type;
                duraklar = duraklar.Where(d => d.Type == durakTipi).ToList();
                
                if (!duraklar.Any())
                {
                    Console.WriteLine($"HATA: Aktarma izni olmadığından ve {durakTipi} tipinde durak bulunamadığından rota hesaplanamıyor");
                    return (new List<string>(), new List<string>());
                }
            }
            
            var rotaPlanlayici = new RotaPlanlayici(duraklar);
            var rota = rotaPlanlayici.EnKisaRota(baslangicDurak.Id, hedefDurak.Id, yolcu);
            
            if (rota == null || !rota.Any())
            {
                Console.WriteLine("HATA: Rota bulunamadı!");
                return (new List<string>(), new List<string>());
            }
            
            // Durak adlarını al
            var rotaAdlari = new List<string>();
            foreach (var durakId in rota)
            {
                var durak = duraklar.FirstOrDefault(d => d.Id == durakId);
                if (durak != null)
                {
                    rotaAdlari.Add(durak.Name);
                }
            }
            
            // Detaylı rota adımlarını oluştur
            var detayliAdimlar = new List<string>
            {
                $"Başlangıç noktası: {baslangicDurak.Name} durağı"
            };
            
            string oncekiTip = null;
            for (int i = 0; i < rotaAdlari.Count; i++)
            {
                var durak = duraklar.FirstOrDefault(d => d.Name == rotaAdlari[i]);
                if (durak == null) continue;
                
                if (i > 0)
                {
                    var oncekiDurak = duraklar.FirstOrDefault(d => d.Name == rotaAdlari[i - 1]);
                    if (oncekiDurak == null) continue;
                    
                    if (oncekiDurak.Type != durak.Type)
                    {
                        // Aktarma izni yoksa atla
                        if (!aktarmaIzin)
                            continue;
                            
                        detayliAdimlar.Add($"Transfer: {oncekiDurak.Name} durağından {durak.Name} durağına yürüyerek geçin");
                    }
                    
                    if (durak.Type == "BUS" || durak.Type == "TRAM")
                    {
                        if (oncekiTip != durak.Type)
                        {
                            detayliAdimlar.Add($"{durak.Type} durağı {durak.Name}'e ulaştınız. Buradan {(durak.Type == "BUS" ? "otobüs" : "tramvay")} ile devam edin.");
                        }
                    }
                }
                
                oncekiTip = durak.Type;
            }
            
            detayliAdimlar.Add($"Varış noktası: {hedefDurak.Name} durağı");
            
            Console.WriteLine($"Standart rota bulundu, durak sayısı: {rotaAdlari.Count}");
            return (rotaAdlari, detayliAdimlar);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HATA: RotaHesapla metodu hatası: {ex.Message}");
            Console.WriteLine($"HATA DETAY: {ex.StackTrace}");
            return (new List<string>(), new List<string>());
        }
    }

    /// <summary>
    /// İki durak arasında sadece otobüs kullanan rotayı hesaplar
    /// </summary>
    public static List<string> SadeceOtobusRotasiHesapla(Durak baslangicDurak, Durak hedefDurak, Yolcu yolcu)
    {
        try
        {
            if (baslangicDurak == null || hedefDurak == null || yolcu == null)
            {
                return new List<string>();
            }
            
            // Sadece otobüs durağı olan durakları filtrele
            var otobusDuraklari = VeriServisi.TumDuraklariGetir()?.Where(d => d.Type == "BUS").ToList();
            if (otobusDuraklari == null || !otobusDuraklari.Any())
            {
                return new List<string>();
            }
            
            // Başlangıç ve hedef otobüs durağı mı kontrol et
            var baslangicOtobus = otobusDuraklari.FirstOrDefault(d => d.Id == baslangicDurak.Id);
            var hedefOtobus = otobusDuraklari.FirstOrDefault(d => d.Id == hedefDurak.Id);
            
            // Eğer başlangıç veya hedef otobüs durağı değilse, en yakın otobüs durağı bul
            if (baslangicOtobus == null)
            {
                baslangicOtobus = DurakServisi.EnYakinDurakBul(baslangicDurak.Lat, baslangicDurak.Lon, "BUS");
            }
            
            if (hedefOtobus == null)
            {
                hedefOtobus = DurakServisi.EnYakinDurakBul(hedefDurak.Lat, hedefDurak.Lon, "BUS");
            }
            
            if (baslangicOtobus == null || hedefOtobus == null)
            {
                return new List<string>();
            }
            
            // Otobüs rotasını hesapla
            var rotaPlanlayici = new RotaPlanlayici(otobusDuraklari);
            var rota = rotaPlanlayici.EnKisaRota(baslangicOtobus.Id, hedefOtobus.Id, yolcu);
            
            if (rota == null || !rota.Any())
            {
                return new List<string>();
            }
            
            // Durak adlarını al
            var rotaAdlari = new List<string>();
            foreach (var durakId in rota)
            {
                var durak = otobusDuraklari.FirstOrDefault(d => d.Id == durakId);
                if (durak != null)
                {
                    rotaAdlari.Add(durak.Name);
                }
            }
            
            return rotaAdlari;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HATA: SadeceOtobusRotasiHesapla metodu hatası: {ex.Message}");
            return new List<string>();
        }
    }
    
    /// <summary>
    /// İki durak arasında tramvay öncelikli rotayı hesaplar
    /// </summary>
    public static List<string> TramvayOncelikliRotaHesapla(Durak baslangicDurak, Durak hedefDurak, Yolcu yolcu)
    {
        try
        {
            if (baslangicDurak == null || hedefDurak == null || yolcu == null)
            {
                return new List<string>();
            }
            
            var duraklar = VeriServisi.TumDuraklariGetir();
            if (duraklar == null || !duraklar.Any())
            {
                return new List<string>();
            }
            
            // Tüm tramvay duraklarını al
            var tramvayDuraklari = duraklar.Where(d => d.Type == "TRAM").ToList();
            if (tramvayDuraklari == null || !tramvayDuraklari.Any())
            {
                return new List<string>();
            }
            
            // Başlangıç ve hedef tramvay durağı mı kontrol et
            var baslangicTramvay = tramvayDuraklari.FirstOrDefault(d => d.Id == baslangicDurak.Id);
            var hedefTramvay = tramvayDuraklari.FirstOrDefault(d => d.Id == hedefDurak.Id);
            
            // Eğer her ikisi de tramvay durağıysa, direkt tramvay rotası hesapla
            if (baslangicTramvay != null && hedefTramvay != null)
            {
                var rotaPlanlayici = new RotaPlanlayici(tramvayDuraklari);
                var rota = rotaPlanlayici.EnKisaRota(baslangicTramvay.Id, hedefTramvay.Id, yolcu);
                
                if (rota == null || !rota.Any())
                {
                    return new List<string>();
                }
                
                var rotaAdlari = new List<string>();
                foreach (var durakId in rota)
                {
                    var durak = tramvayDuraklari.FirstOrDefault(d => d.Id == durakId);
                    if (durak != null)
                    {
                        rotaAdlari.Add(durak.Name);
                    }
                }
                
                return rotaAdlari;
            }
            
            // Başlangıç tramvay durağı değilse, en yakın tramvay durağı bul
            if (baslangicTramvay == null)
            {
                baslangicTramvay = DurakServisi.EnYakinDurakBul(baslangicDurak.Lat, baslangicDurak.Lon, "TRAM");
            }
            
            // Hedef tramvay durağı değilse, en yakın tramvay durağı bul
            if (hedefTramvay == null)
            {
                hedefTramvay = DurakServisi.EnYakinDurakBul(hedefDurak.Lat, hedefDurak.Lon, "TRAM");
            }
            
            if (baslangicTramvay == null || hedefTramvay == null)
            {
                // Tramvay rotası bulunamadı, boş liste döndür
                return new List<string>();
            }
            
            // Başlangıç noktasından tramvay durağına otobüs ile git
            List<string> tamRota = new List<string>();
            
            if (baslangicDurak.Id != baslangicTramvay.Id)
            {
                var otobusDuraklari = duraklar.Where(d => d.Type == "BUS").ToList();
                var enYakinOtobusDuragi = DurakServisi.EnYakinDurakBul(baslangicDurak.Lat, baslangicDurak.Lon, "BUS");
                var enYakinTramvayOtobusDuragi = DurakServisi.EnYakinDurakBul(baslangicTramvay.Lat, baslangicTramvay.Lon, "BUS");
                
                if (enYakinOtobusDuragi != null && enYakinTramvayOtobusDuragi != null)
                {
                    var rotaPlanlayici = new RotaPlanlayici(otobusDuraklari);
                    var otobusRota = rotaPlanlayici.EnKisaRota(enYakinOtobusDuragi.Id, enYakinTramvayOtobusDuragi.Id, yolcu);
                    
                    if (otobusRota != null)
                    {
                        foreach (var durakId in otobusRota)
                        {
                            var durak = otobusDuraklari.FirstOrDefault(d => d.Id == durakId);
                            if (durak != null)
                            {
                                tamRota.Add(durak.Name);
                            }
                        }
                    }
                }
            }
            
            // Tramvay rotası
            var tramvayRotaPlanlayici = new RotaPlanlayici(tramvayDuraklari);
            var tramvayRota = tramvayRotaPlanlayici.EnKisaRota(baslangicTramvay.Id, hedefTramvay.Id, yolcu);
            
            if (tramvayRota != null)
            {
                foreach (var durakId in tramvayRota)
                {
                    var durak = tramvayDuraklari.FirstOrDefault(d => d.Id == durakId);
                    if (durak != null)
                    {
                        tamRota.Add(durak.Name);
                    }
                }
            }
            
            // Tramvay durağından hedef noktaya otobüs ile git
            if (hedefDurak.Id != hedefTramvay.Id)
            {
                var otobusDuraklari = duraklar.Where(d => d.Type == "BUS").ToList();
                var enYakinTramvayOtobusDuragi = DurakServisi.EnYakinDurakBul(hedefTramvay.Lat, hedefTramvay.Lon, "BUS");
                var enYakinHedefOtobusDuragi = DurakServisi.EnYakinDurakBul(hedefDurak.Lat, hedefDurak.Lon, "BUS");
                
                if (enYakinTramvayOtobusDuragi != null && enYakinHedefOtobusDuragi != null)
                {
                    var rotaPlanlayici = new RotaPlanlayici(otobusDuraklari);
                    var otobusRota = rotaPlanlayici.EnKisaRota(enYakinTramvayOtobusDuragi.Id, enYakinHedefOtobusDuragi.Id, yolcu);
                    
                    if (otobusRota != null)
                    {
                        // İlk durağı hariç tut (çünkü zaten tramvay durağındayız)
                        bool ilkDurak = true;
                        foreach (var durakId in otobusRota)
                        {
                            if (ilkDurak)
                            {
                                ilkDurak = false;
                                continue;
                            }
                            
                            var durak = otobusDuraklari.FirstOrDefault(d => d.Id == durakId);
                            if (durak != null)
                            {
                                tamRota.Add(durak.Name);
                            }
                        }
                    }
                }
            }
            
            return tamRota;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HATA: TramvayOncelikliRotaHesapla metodu hatası: {ex.Message}");
            return new List<string>();
        }
    }
}
}