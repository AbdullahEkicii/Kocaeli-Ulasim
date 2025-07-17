using System;
using System.Collections.Generic;
using System.Linq;
using KOÜ_Ulaşım.Models;

namespace KOÜ_Ulaşım.Services
{
    // Rota sonuç sınıfı
    public class RotaSonuc
    {
        public List<string> Rota { get; set; } = new List<string>();
        public List<string> DetayliRota { get; set; } = new List<string>();
        public double ToplamMesafe { get; set; }
        public int ToplamSure { get; set; }
        public double ToplamUcret { get; set; }
        public int TransferSayisi { get; set; }
        public bool BaslangictaTaksiGerekli { get; set; }
        public bool HedefteTaksiGerekli { get; set; }
        public double TaksiMesafe { get; set; }
        public double TaksiUcreti { get; set; }
        public double ToplamYurumeMesafesi { get; set; }
        public string StratejiAdi { get; set; } = "Standart";
    }

    public class RotaPlanlamaServisi
    {
        // DurakServisi ve TaksiServisi statik sınıflar oldukları için 
        // bunları instance değişkenler olarak tanımlayamayız
        private readonly List<Durak> _duraklar;
        
        private readonly Dictionary<string, Arac> _araclar = new Dictionary<string, Arac>
        {
            { "bus", new Otobus() },
            { "tram", new Tramvay() }
        };
        
        public RotaPlanlamaServisi()
        {
            _duraklar = VeriServisi.TumDuraklariGetir();
            // Statik sınıfların örneklerini oluşturamayız
        }
        
        // Ana rota planlama metodu
        public Dictionary<string, RotaSonuc> RotaPlanla(
            double kullaniciLat, 
            double kullaniciLon, 
            double hedefLat, 
            double hedefLon, 
            Yolcu yolcu)
        {
            try
            {
                // En yakın durakları bul - DurakServisi statik bir sınıf olduğu için doğrudan metodunu çağırıyoruz
                var baslangicDurak = DurakServisi.EnYakinDurak(kullaniciLat, kullaniciLon, _duraklar);
                var hedefDurak = DurakServisi.EnYakinDurak(hedefLat, hedefLon, _duraklar);
                
                if (baslangicDurak == null || hedefDurak == null)
                {
                    Console.WriteLine("HATA: Başlangıç veya hedef durak bulunamadı!");
                    return new Dictionary<string, RotaSonuc>();
                }
                
                // Başlangıç ve hedefte taksi gerekli mi kontrol et - TaksiServisi de statik bir sınıf
                bool baslangictaTaksiGerekli = TaksiServisi.TaksiGerekliMi(kullaniciLat, kullaniciLon, baslangicDurak.Lat, baslangicDurak.Lon);
                bool hedefteTaksiGerekli = TaksiServisi.TaksiGerekliMi(hedefLat, hedefLon, hedefDurak.Lat, hedefDurak.Lon);
                
                // Taksi mesafeleri ve ücretleri
                double baslangicTaksiMesafe = TaksiServisi.MesafeHesapla(kullaniciLat, kullaniciLon, baslangicDurak.Lat, baslangicDurak.Lon);
                double hedefTaksiMesafe = TaksiServisi.MesafeHesapla(hedefDurak.Lat, hedefDurak.Lon, hedefLat, hedefLon);
                double baslangicTaksiUcret = baslangictaTaksiGerekli ? TaksiServisi.UcretHesapla(baslangicTaksiMesafe) : 0;
                double hedefTaksiUcret = hedefteTaksiGerekli ? TaksiServisi.UcretHesapla(hedefTaksiMesafe) : 0;
                
                // Rota stratejilerini uygula
                var rotaPlanlayici = new RotaPlanlayici(_duraklar);
                var stratejiler = rotaPlanlayici.TumStratejiRotalariniHesapla(baslangicDurak, hedefDurak, yolcu);
                
                // Sadece taksi opsiyonu ekle
                var taksiRotaSonuc = HesaplaSadeceTaksiRotasi(
                    kullaniciLat, kullaniciLon, hedefLat, hedefLon, yolcu);
                
                // Tüm stratejilerin sonuçlarını bir araya getir
                var sonuclar = new Dictionary<string, RotaSonuc>();
                sonuclar["Sadece Taksi"] = taksiRotaSonuc;
                
                foreach (var strateji in stratejiler)
                {
                    string stratejiAdi = strateji.Key;
                    List<string> rotaIds = strateji.Value;
                    
                    if (rotaIds == null || rotaIds.Count == 0)
                    {
                        Console.WriteLine($"{stratejiAdi} stratejisi için rota bulunamadı.");
                        continue;
                    }
                    
                    var rotaSonuc = HesaplaTopluTasimaRotasi(
                        rotaIds, 
                        kullaniciLat, kullaniciLon, 
                        hedefLat, hedefLon, 
                        yolcu,
                        baslangictaTaksiGerekli,
                        hedefteTaksiGerekli,
                        baslangicTaksiMesafe,
                        hedefTaksiMesafe,
                        baslangicTaksiUcret,
                        hedefTaksiUcret);
                    
                    rotaSonuc.StratejiAdi = stratejiAdi;
                    sonuclar[stratejiAdi] = rotaSonuc;
                }
                
                return sonuclar;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RotaPlanla hatası: {ex.Message}");
                return new Dictionary<string, RotaSonuc>();
            }
        }
        
        // Sadece taksi ile gidiş rotası
        private RotaSonuc HesaplaSadeceTaksiRotasi(
            double baslangicLat, double baslangicLon, 
            double hedefLat, double hedefLon, 
            Yolcu yolcu)
        {
            double mesafe = TaksiServisi.MesafeHesapla(baslangicLat, baslangicLon, hedefLat, hedefLon);
            double ucret = TaksiServisi.UcretHesapla(mesafe);
            var taksi = new Taksi();
            int sure = taksi.SureHesapla(mesafe);
            
            var sonuc = new RotaSonuc
            {
                Rota = new List<string> { "taksi_rota" },
                DetayliRota = new List<string> { 
                    $"🚖 Taksi ile direkt ulaşım",
                    $"📏 Mesafe: {mesafe:F2} km",
                    $"⏱️ Süre: {sure} dakika",
                    $"💰 Ücret: {ucret:F2} TL"
                },
                ToplamMesafe = mesafe,
                ToplamSure = sure,
                ToplamUcret = ucret,
                TransferSayisi = 0,
                BaslangictaTaksiGerekli = true,
                HedefteTaksiGerekli = false,
                TaksiMesafe = mesafe,
                TaksiUcreti = ucret,
                ToplamYurumeMesafesi = 0,
                StratejiAdi = "Sadece Taksi"
            };
            
            return sonuc;
        }
        
        // Toplu taşıma rotasını hesapla (taksi gerekliyse ekle)
        private RotaSonuc HesaplaTopluTasimaRotasi(
            List<string> rotaIds,
            double kullaniciLat, double kullaniciLon,
            double hedefLat, double hedefLon,
            Yolcu yolcu,
            bool baslangictaTaksiGerekli,
            bool hedefteTaksiGerekli,
            double baslangicTaksiMesafe,
            double hedefTaksiMesafe,
            double baslangicTaksiUcret,
            double hedefTaksiUcret)
        {
            var sonuc = new RotaSonuc
            {
                Rota = rotaIds,
                BaslangictaTaksiGerekli = baslangictaTaksiGerekli,
                HedefteTaksiGerekli = hedefteTaksiGerekli,
                TaksiMesafe = baslangicTaksiMesafe + hedefTaksiMesafe,
                TaksiUcreti = baslangicTaksiUcret + hedefTaksiUcret
            };
            
            // Durakları ID'den al
            var duraklar = rotaIds.Select(id => _duraklar.FirstOrDefault(d => d.Id == id)).Where(d => d != null).ToList();
            
            if (duraklar.Count < 2)
            {
                return sonuc;
            }
            
            double toplamMesafe = 0;
            int toplamSure = 0;
            double toplamUcret = 0;
            int transferSayisi = 0;
            var detayliRota = new List<string>();
            
            // Taksi ile başlangıç gerekli ise ekle
            if (baslangictaTaksiGerekli)
            {
                var taksi = new Taksi();
                int taksiBinisSuresi = taksi.SureHesapla(baslangicTaksiMesafe);
                
                detayliRota.Add($"🚖 Taksi ile başlangıç durağına ulaşım");
                detayliRota.Add($"📏 Mesafe: {baslangicTaksiMesafe:F2} km");
                detayliRota.Add($"⏱️ Süre: {taksiBinisSuresi} dakika");
                detayliRota.Add($"💰 Ücret: {baslangicTaksiUcret:F2} TL");
                
                toplamSure += taksiBinisSuresi;
                toplamUcret += baslangicTaksiUcret;
            }
            else
            {
                // Yürüme süresi (dakika cinsinden, 5 km/saat yürüme hızı)
                double yurumeSuresiDk = (baslangicTaksiMesafe / 5.0) * 60;
                
                detayliRota.Add($"🚶 Yürüyerek başlangıç durağına ulaşım");
                detayliRota.Add($"📏 Mesafe: {baslangicTaksiMesafe:F2} km");
                detayliRota.Add($"⏱️ Süre: {yurumeSuresiDk:F0} dakika");
                
                toplamSure += (int)yurumeSuresiDk;
                sonuc.ToplamYurumeMesafesi += baslangicTaksiMesafe;
            }
            
            // Duraklar arası yolculuk
            string simdikiTip = "";
            
            for (int i = 0; i < duraklar.Count - 1; i++)
            {
                var simdikiDurak = duraklar[i];
                var sonrakiDurak = duraklar[i + 1];
                
                // Bu durağın sonraki durak bağlantısını bul
                var baglanti = simdikiDurak.NextStops.FirstOrDefault(b => b.StopId == sonrakiDurak.Id);
                
                if (baglanti == null)
                {
                    // Doğrudan bağlantı yok, transfer olabilir
                    if (simdikiDurak.Transfer != null && simdikiDurak.Transfer.TransferStopId == sonrakiDurak.Id)
                    {
                        // Transfer noktası
                        transferSayisi++;
                        
                        detayliRota.Add($"🔄 Transfer: {simdikiDurak.Name} → {sonrakiDurak.Name}");
                        detayliRota.Add($"⏱️ Süre: {simdikiDurak.Transfer.TransferSure} dakika");
                        detayliRota.Add($"💰 Ücret: {simdikiDurak.Transfer.TransferUcret:F2} TL");
                        
                        toplamSure += simdikiDurak.Transfer.TransferSure;
                        
                        // İndirimli transfer ücreti
                        double indirimliTransferUcreti = simdikiDurak.Transfer.TransferUcret * (1 - yolcu.IndirimOrani());
                        toplamUcret += indirimliTransferUcreti;
                        
                        // Tip değişimi
                        if (simdikiTip != sonrakiDurak.Type)
                        {
                            simdikiTip = sonrakiDurak.Type;
                        }
                    }
                    else
                    {
                        // Bir sorun var, bağlantı olmamalıydı
                        Console.WriteLine($"UYARI: {simdikiDurak.Id} ve {sonrakiDurak.Id} arasında bağlantı bulunamadı!");
                    }
                }
                else
                {
                    // Normal durak bağlantısı
                    bool tipDegisti = simdikiTip != simdikiDurak.Type;
                    simdikiTip = simdikiDurak.Type;
                    
                    string aracIkonu = simdikiDurak.Type == "bus" ? "🚌" : "🚋";
                    string aracAdi = simdikiDurak.Type == "bus" ? "Otobüs" : "Tramvay";
                    
                    if (i == 0 || tipDegisti)
                    {
                        detayliRota.Add($"{aracIkonu} {aracAdi} yolculuğu başladı");
                    }
                    
                    detayliRota.Add($"{simdikiDurak.Name} → {sonrakiDurak.Name}");
                    detayliRota.Add($"📏 Mesafe: {baglanti.Mesafe:F2} km");
                    detayliRota.Add($"⏱️ Süre: {baglanti.Sure} dakika");
                    
                    toplamMesafe += baglanti.Mesafe;
                    toplamSure += baglanti.Sure;
                    
                    // İlk biniş veya tip değiştiyse tam ücret, yoksa bağlantı ücreti
                    if (i == 0 || tipDegisti)
                    {
                        var arac = _araclar[simdikiDurak.Type];
                        double indirimliUcret = arac.UcretHesapla(baglanti.Mesafe, yolcu);
                        toplamUcret += indirimliUcret;
                        
                        detayliRota.Add($"💰 Ücret: {indirimliUcret:F2} TL");
                    }
                }
            }
            
            // Taksi ile hedefe ulaşım gerekli ise ekle
            if (hedefteTaksiGerekli)
            {
                var taksi = new Taksi();
                int taksiInisSuresi = taksi.SureHesapla(hedefTaksiMesafe);
                
                detayliRota.Add($"🚖 Taksi ile hedef noktaya ulaşım");
                detayliRota.Add($"📏 Mesafe: {hedefTaksiMesafe:F2} km");
                detayliRota.Add($"⏱️ Süre: {taksiInisSuresi} dakika");
                detayliRota.Add($"💰 Ücret: {hedefTaksiUcret:F2} TL");
                
                toplamSure += taksiInisSuresi;
                toplamUcret += hedefTaksiUcret;
            }
            else
            {
                // Yürüme süresi
                double yurumeSuresiDk = (hedefTaksiMesafe / 5.0) * 60;
                
                detayliRota.Add($"🚶 Yürüyerek hedef noktaya ulaşım");
                detayliRota.Add($"📏 Mesafe: {hedefTaksiMesafe:F2} km");
                detayliRota.Add($"⏱️ Süre: {yurumeSuresiDk:F0} dakika");
                
                toplamSure += (int)yurumeSuresiDk;
                sonuc.ToplamYurumeMesafesi += hedefTaksiMesafe;
            }
            
            // Özet
            detayliRota.Add($"📊 TOPLAM ÖZET");
            detayliRota.Add($"📏 Toplam Mesafe: {toplamMesafe + sonuc.TaksiMesafe + sonuc.ToplamYurumeMesafesi:F2} km");
            detayliRota.Add($"⏱️ Toplam Süre: {toplamSure} dakika");
            detayliRota.Add($"💰 Toplam Ücret: {toplamUcret:F2} TL");
            detayliRota.Add($"🔄 Transfer Sayısı: {transferSayisi}");
            
            // Sonuç değerlerini ata
            sonuc.DetayliRota = detayliRota;
            sonuc.ToplamMesafe = toplamMesafe + sonuc.TaksiMesafe;
            sonuc.ToplamSure = toplamSure;
            sonuc.ToplamUcret = toplamUcret;
            sonuc.TransferSayisi = transferSayisi;
            
            return sonuc;
        }
    }
} 