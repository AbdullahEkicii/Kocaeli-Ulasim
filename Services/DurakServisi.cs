using System;
using System.Collections.Generic;
using System.Linq;
using KOÜ_Ulaşım.Models;

namespace KOÜ_Ulaşım.Services
{
    public static class DurakServisi
    {
        private static Durak? EnYakinDurakBulInternal(double lat, double lon, string? durakTipi = null)
        {
            var duraklar = VeriServisi.TumDuraklariGetir();
            if (duraklar == null || !duraklar.Any())
                return null;

            if (!string.IsNullOrEmpty(durakTipi))
            {
                duraklar = duraklar.Where(d => d.Type.Equals(durakTipi, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!duraklar.Any())
                    return null;
            }

            return duraklar
                .Select(d => new { Durak = d, Mesafe = TaksiServisi.MesafeHesapla(lat, lon, d.Lat, d.Lon) })
                .OrderBy(x => x.Mesafe)
                .FirstOrDefault()?.Durak;
        }

        public static Durak? EnYakinDurakBul(double lat, double lon)
        {
            return EnYakinDurakBulInternal(lat, lon);
        }

        public static Durak? EnYakinDurakBul(double lat, double lon, string durakTipi)
        {
            return EnYakinDurakBulInternal(lat, lon, durakTipi);
        }

        /// <summary>
        /// Verilen koordinata en yakın hem otobüs hem tramvay durağını bulur ve 
        /// hangisinin daha iyi olduğunu değerlendirir
        /// </summary>
        public static (Durak durak, string tip, double mesafe) EnIyiDurakBul(double lat, double lon)
        {
            try 
            {
                Console.WriteLine($"EnIyiDurakBul çağrıldı: Koordinatlar({lat:F6}, {lon:F6})");
                
                var duraklar = VeriServisi.TumDuraklariGetir();
                if (duraklar == null || !duraklar.Any())
                {
                    Console.WriteLine("HATA: Durak listesi boş!");
                    return (null, "", 0);
                }

                // Çok yakındaki herhangi bir durağı kontrol et (öncelikli)
                const double COK_YAKIN_MESAFE = 0.3; // 300 metre içindeki duraklar doğrudan seçilir
                
                var tumDuraklar = duraklar.ToList();
                var durakMesafeleri = tumDuraklar
                    .Select(d => new { Durak = d, Mesafe = TaksiServisi.MesafeHesapla(lat, lon, d.Lat, d.Lon) })
                    .OrderBy(x => x.Mesafe)
                    .ToList();
                
                var enYakinHerhangiBirDurak = durakMesafeleri.FirstOrDefault();
                
                // Tüm durakları ve mesafelerini logla
                Console.WriteLine("Tüm duraklar ve mesafeleri:");
                foreach (var dm in durakMesafeleri.Take(5)) // İlk 5 durak
                {
                    Console.WriteLine($"- {dm.Durak.Name} ({dm.Durak.Type}): {dm.Mesafe:F2} km");
                }
                
                // Eğer çok yakında bir durak varsa, doğrudan onu kullan
                if (enYakinHerhangiBirDurak != null && enYakinHerhangiBirDurak.Mesafe <= COK_YAKIN_MESAFE)
                {
                    Console.WriteLine($"Çok yakın durak bulundu: {enYakinHerhangiBirDurak.Durak.Name}, mesafe: {enYakinHerhangiBirDurak.Mesafe:F2} km");
                    return (enYakinHerhangiBirDurak.Durak, enYakinHerhangiBirDurak.Durak.Type, enYakinHerhangiBirDurak.Mesafe);
                }

                // Tramvay durakları
                var tramvayDurakları = duraklar.Where(d => d.Type.ToUpper() == "TRAM").ToList();
                
                // Otobüs durakları
                var otobusDurakları = duraklar.Where(d => d.Type.ToUpper() == "BUS").ToList();
                
                // En yakın tramvay durağı
                Durak enYakinTramvay = null;
                double enKisaTramvayMesafe = double.MaxValue;
                
                foreach (var durak in tramvayDurakları)
                {
                    var mesafe = TaksiServisi.MesafeHesapla(lat, lon, durak.Lat, durak.Lon);
                    if (mesafe < enKisaTramvayMesafe)
                    {
                        enKisaTramvayMesafe = mesafe;
                        enYakinTramvay = durak;
                    }
                }
                
                // En yakın otobüs durağı
                Durak enYakinOtobus = null;
                double enKisaOtobusMesafe = double.MaxValue;
                
                foreach (var durak in otobusDurakları)
                {
                    var mesafe = TaksiServisi.MesafeHesapla(lat, lon, durak.Lat, durak.Lon);
                    if (mesafe < enKisaOtobusMesafe)
                    {
                        enKisaOtobusMesafe = mesafe;
                        enYakinOtobus = durak;
                    }
                }
                
                // Log durak bilgilerini
                Console.WriteLine($"En yakın tramvay: {(enYakinTramvay?.Name ?? "Yok")}, mesafe: {enKisaTramvayMesafe:F2} km");
                Console.WriteLine($"En yakın otobüs: {(enYakinOtobus?.Name ?? "Yok")}, mesafe: {enKisaOtobusMesafe:F2} km");
                
                // Hangi durak daha yakın?
                if (enYakinTramvay != null && enYakinOtobus != null)
                {
                    // Eğer tramvay durağı yürüme mesafesinde ve otobüsten en fazla 1.2 kat uzaksa, tramvayı tercih et
                    const double TRAMVAY_TERCIH_FAKTORU = 1.2;
                    const double MAKSIMUM_YURUME_MESAFESI = 2.0;
                    
                    if (enKisaTramvayMesafe <= MAKSIMUM_YURUME_MESAFESI && 
                        enKisaTramvayMesafe <= enKisaOtobusMesafe * TRAMVAY_TERCIH_FAKTORU)
                    {
                        Console.WriteLine($"Tramvay tercih edildi: {enYakinTramvay.Name}");
                        return (enYakinTramvay, "TRAM", enKisaTramvayMesafe);
                    }
                    
                    Console.WriteLine($"Otobüs tercih edildi: {enYakinOtobus.Name}");
                    return (enYakinOtobus, "BUS", enKisaOtobusMesafe);
                }
                else if (enYakinTramvay != null)
                {
                    Console.WriteLine($"Sadece tramvay bulundu: {enYakinTramvay.Name}");
                    return (enYakinTramvay, "TRAM", enKisaTramvayMesafe);
                }
                else if (enYakinOtobus != null)
                {
                    Console.WriteLine($"Sadece otobüs bulundu: {enYakinOtobus.Name}");
                    return (enYakinOtobus, "BUS", enKisaOtobusMesafe);
                }
                
                Console.WriteLine("HATA: Hiçbir durak bulunamadı!");
                return (null, "", 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HATA: EnIyiDurakBul metodu hatası: {ex.Message}");
                Console.WriteLine($"HATA DETAY: {ex.StackTrace}");
                return (null, "", 0);
            }
        }

        // Parametre listesinde duraklar listesini içeren overload ekleyelim
        public static Durak? EnYakinDurak(double lat, double lon, List<Durak> duraklar)
        {
            if (duraklar == null || !duraklar.Any())
                return null;

            return duraklar
                .Select(d => new { Durak = d, Mesafe = TaksiServisi.MesafeHesapla(lat, lon, d.Lat, d.Lon) })
                .OrderBy(x => x.Mesafe)
                .FirstOrDefault()?.Durak;
        }
    }
}
