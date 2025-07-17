using KOÜ_Ulaşım.Models;
using KOÜ_Ulaşım.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KOÜ_Ulaşım.Controllers
{
    public class HomeController : Controller
    {
        private readonly RotaPlanlamaServisi _rotaPlanlayici;
        
        public HomeController()
        {
            _rotaPlanlayici = new RotaPlanlamaServisi();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RotaHesapla(
            string kullaniciLat, 
            string kullaniciLon, 
            string hedefLat, 
            string hedefLon, 
            string yolcuTipi)
        {
            try
            {
                Console.WriteLine("------------------- ROTA HESAPLAMA BAŞLADI -------------------");
                Console.WriteLine($"Ham Koordinatlar - Başlangıç: ({kullaniciLat}, {kullaniciLon}), Hedef: ({hedefLat}, {hedefLon})");
                
                // Koordinatları düzelt
                double kullaniciEnlem = DüzeltKoordinat(kullaniciLat);
                double kullaniciBoylam = DüzeltKoordinat(kullaniciLon);
                double hedefEnlem = DüzeltKoordinat(hedefLat);
                double hedefBoylam = DüzeltKoordinat(hedefLon);
                
                Console.WriteLine($"Düzeltilmiş Koordinatlar - Başlangıç: ({kullaniciEnlem:F6}, {kullaniciBoylam:F6}), Hedef: ({hedefEnlem:F6}, {hedefBoylam:F6})");
                
                // Koordinat kontrolü - çok düşük/sıfır değerler için hata döndür
                if (Math.Abs(kullaniciEnlem) < 0.001 || Math.Abs(kullaniciBoylam) < 0.001 || 
                    Math.Abs(hedefEnlem) < 0.001 || Math.Abs(hedefBoylam) < 0.001)
                {
                    Console.WriteLine("HATA: Koordinat değerleri çok düşük veya sıfır!");
                    ViewBag.Hata = "Lütfen haritadan geçerli konumlar seçiniz. Koordinatlar geçersiz.";
                    return View("Index");
                }
                
                // Kullanıcı tipi
                Yolcu yolcu = YolcuOlustur(yolcuTipi);
                
                // Test amaçlı sabit koordinatlar kullanabiliriz
                if (kullaniciEnlem < 0.001)
                {
                    kullaniciEnlem = 40.762913; // Örnek değer (Kocaeli'de bir nokta)
                    kullaniciBoylam = 29.934316;
                }
                
                if (hedefEnlem < 0.001)
                {
                    hedefEnlem = 40.775783; // Örnek değer (Kocaeli'de başka bir nokta)
                    hedefBoylam = 29.952693;
                }
                
                // Rota planlamasını çağır
                var rotaSonuclari = _rotaPlanlayici.RotaPlanla(
                    kullaniciEnlem, 
                    kullaniciBoylam, 
                    hedefEnlem, 
                    hedefBoylam, 
                    yolcu);
                
                if (rotaSonuclari.Count == 0)
                {
                    ViewBag.Hata = "Rota hesaplanamadı. Lütfen farklı konumlar deneyiniz.";
                    return View("Index");
                }
                
                // Sadece Taksi rotası
                var taksiRotaSonuc = rotaSonuclari["Sadece Taksi"];
                ViewBag.TaksiRotaMesafe = taksiRotaSonuc.ToplamMesafe;
                ViewBag.TaksiRotaSure = taksiRotaSonuc.ToplamSure;
                ViewBag.TaksiRotaUcret = taksiRotaSonuc.ToplamUcret;
                ViewBag.TaksiRotaDetay = taksiRotaSonuc.DetayliRota;
                
                // Varsayılan (en optimum) rotayı bul
                var optimumRota = rotaSonuclari
                    .Where(kvp => kvp.Key != "Sadece Taksi")
                    .OrderBy(kvp => kvp.Value.ToplamSure)
                    .FirstOrDefault();
                
                if (optimumRota.Key == null)
                {
                    // Eğer toplu taşıma rotası bulunamadıysa sadece taksi kullan
                    optimumRota = new KeyValuePair<string, RotaSonuc>("Sadece Taksi", taksiRotaSonuc);
                }
                
                // Rota bilgisini ViewBag'e ekle
                ViewBag.StratejiAdi = optimumRota.Key;
                ViewBag.BaslangictaTaksiGerekli = optimumRota.Value.BaslangictaTaksiGerekli;
                ViewBag.HedefteTaksiGerekli = optimumRota.Value.HedefteTaksiGerekli;
                ViewBag.TaksiUcreti = optimumRota.Value.TaksiUcreti;
                ViewBag.ToplamMesafe = optimumRota.Value.ToplamMesafe;
                ViewBag.ToplamSure = optimumRota.Value.ToplamSure;
                ViewBag.ToplamUcret = optimumRota.Value.ToplamUcret;
                ViewBag.TransferSayisi = optimumRota.Value.TransferSayisi;
                ViewBag.ToplamYurumeMesafesi = optimumRota.Value.ToplamYurumeMesafesi;
                ViewBag.YolcuTipi = yolcu.Tip;
                ViewBag.IndirimOrani = yolcu.IndirimOrani() * 100;
                
                // KentKart Ücreti (opsiyonel)
                ViewBag.KentKartUcreti = optimumRota.Value.ToplamUcret * 0.9; // %10 indirim
                
                // Bakiye bilgisi (opsiyonel, gerçek uygulamada veritabanından getirilir)
                ViewBag.ToplamBakiye = 100.0;
                
                // Kullanıcı ve hedef koordinatları
                ViewBag.KullaniciKonum = new { Lat = kullaniciEnlem, Lon = kullaniciBoylam };
                ViewBag.HedefKonum = new { Lat = hedefEnlem, Lon = hedefBoylam };
                
                // RotaBilgisi tuple değişkenini ayarla - View'da kullanım için
                ViewBag.RotaBilgisi = Tuple.Create(optimumRota.Value.Rota, optimumRota.Value.DetayliRota);
                
                // Sadece otobüs ve tramvay öncelikli rotaları da ekle
                if (rotaSonuclari.ContainsKey("Sadece Otobüs"))
                {
                    ViewBag.SadeceOtobusRota = rotaSonuclari["Sadece Otobüs"].Rota;
                }
                
                if (rotaSonuclari.ContainsKey("Tramvay Öncelikli"))
                {
                    ViewBag.TramvayOncelikliRota = rotaSonuclari["Tramvay Öncelikli"].Rota;
                }
                
                // Tüm rota alternatifleri
                var rotaAlternatifleri = new Dictionary<string, object>();
                foreach (var rota in rotaSonuclari)
                {
                    rotaAlternatifleri[rota.Key] = new
                    {
                        stratejiAdi = rota.Key,
                        toplamMesafe = rota.Value.ToplamMesafe,
                        toplamSure = rota.Value.ToplamSure,
                        toplamUcret = rota.Value.ToplamUcret,
                        transferSayisi = rota.Value.TransferSayisi,
                        detayliRota = rota.Value.DetayliRota
                    };
                }
                ViewBag.RotaAlternatifleri = rotaAlternatifleri;
                
                // Detaylı rota bilgisini ekle
                ViewBag.DetayliRota = optimumRota.Value.DetayliRota;
                
                return View("RotaSonuc");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HATA: Rota hesaplama sırasında bir hata oluştu: {ex}");
                ViewBag.Hata = "Rota hesaplanırken bir hata oluştu. Lütfen tekrar deneyiniz.";
                return View("Index");
            }
        }

        private double DüzeltKoordinat(string koordinat)
        {
            try
            {
                if (string.IsNullOrEmpty(koordinat) || koordinat == "0")
                    return 0;

                // Gelen string içindeki virgülleri nokta ile değiştir (kültür farklılıkları için)
                koordinat = koordinat.Replace(',', '.');
                
                // Farklı kültürler için InvariantCulture kullan
                if (double.TryParse(koordinat, NumberStyles.Any, CultureInfo.InvariantCulture, out double deger))
                {
                    // JavaScript tarafından 1000000 ile çarpılmış olabilir
                    if (Math.Abs(deger) > 100)
                    {
                        deger = deger / 1000000.0;
                    }
                    
                    Console.WriteLine($"Koordinat dönüştürüldü: {koordinat} -> {deger:F6}");
                    return deger;
                }
                
                Console.WriteLine($"HATA: Koordinat çevrilemedi: {koordinat}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Koordinat düzeltme hatası: {ex.Message}, Değer: {koordinat}");
                return 0; // Hata durumunda 0 döndür
            }
        }

        private Yolcu YolcuOlustur(string yolcuTipi)
        {
            return yolcuTipi switch
            {
                "ogrenci" => new Ogrenci(),
                "yasli" => new Yasli(),
                "engelli" => new Engelli(),
                "ogretmen" => new Ogretmen(),
                _ => new Genel()
            };
        }

        [HttpGet]
        public IActionResult TestKoordinat()
        {
            try
            {
                // Test koordinatları
                var testKoordinatlar = new Dictionary<string, double>() {
                    { "kullaniciLat", 40762913.14211204 },
                    { "kullaniciLon", 29934316.57176595 },
                    { "hedefLat", 40775783.81534588 },
                    { "hedefLon", 29952693.42783435 }
                };

                var culture = System.Globalization.CultureInfo.InvariantCulture;
                
                foreach (var koordinat in testKoordinatlar)
                {
                    Console.WriteLine($"Test - {koordinat.Key}: Orijinal={koordinat.Value}");
                    
                    // 1000000'a böl
                    double divided = koordinat.Value / 1000000.0;
                    Console.WriteLine($"Test - {koordinat.Key}: Bölünmüş={divided}");
                }
                
                return Content("Koordinat testi tamamlandı. Konsol çıktısını kontrol edin.");
            }
            catch (Exception ex)
            {
                return Content("Hata: " + ex.Message);
            }
        }
    }
}
