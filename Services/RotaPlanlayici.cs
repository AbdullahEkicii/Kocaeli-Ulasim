using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KOÜ_Ulaşım.Models;

namespace KOÜ_Ulaşım.Services
{
    // Rota Hesaplama Stratejisi arayüzü - Strateji tasarım deseni
    public interface IRotaHesaplamaStratejisi
    {
        List<string> RotaHesapla(Durak baslangic, Durak hedef, Yolcu yolcu, List<Durak> duraklar);
        string StratejiAdi { get; }
    }
    
    // Sadece Otobüs Stratejisi
    public class SadeceOtobusStratejisi : IRotaHesaplamaStratejisi
    {
        public string StratejiAdi => "Sadece Otobüs";
        
        public List<string> RotaHesapla(Durak baslangic, Durak hedef, Yolcu yolcu, List<Durak> duraklar)
        {
            // Sadece otobüs tipleriyle rota hesapla
            var otobusDuraklari = duraklar.Where(d => d.Type == "bus").ToList();
            var planlayici = new RotaPlanlayici(otobusDuraklari);
            return planlayici.EnKisaRota(baslangic.Id, hedef.Id, yolcu);
        }
    }
    
    // Sadece Tramvay Stratejisi
    public class SadeceTramvayStratejisi : IRotaHesaplamaStratejisi
    {
        public string StratejiAdi => "Sadece Tramvay";
        
        public List<string> RotaHesapla(Durak baslangic, Durak hedef, Yolcu yolcu, List<Durak> duraklar)
        {
            // Sadece tramvay tipleriyle rota hesapla
            var tramvayDuraklari = duraklar.Where(d => d.Type == "tram").ToList();
            var planlayici = new RotaPlanlayici(tramvayDuraklari);
            return planlayici.EnKisaRota(baslangic.Id, hedef.Id, yolcu);
        }
    }
    
    // Tramvay Öncelikli Strateji
    public class TramvayOncelikliStrateji : IRotaHesaplamaStratejisi
    {
        public string StratejiAdi => "Tramvay Öncelikli";
        
        public List<string> RotaHesapla(Durak baslangic, Durak hedef, Yolcu yolcu, List<Durak> duraklar)
        {
            // Tramvay ve transfer duraklarına öncelik ver
            var planlayici = new RotaPlanlayici(duraklar);
            
            // Tramvay duraklarına daha düşük ağırlık vererek önceliklendir
            return planlayici.EnKisaRota(baslangic.Id, hedef.Id, yolcu);
        }
    }
    
    // En Az Aktarmalı Strateji
    public class EnAzAktarmaliStrateji : IRotaHesaplamaStratejisi
    {
        public string StratejiAdi => "En Az Aktarmalı";
        
        public List<string> RotaHesapla(Durak baslangic, Durak hedef, Yolcu yolcu, List<Durak> duraklar)
        {
            // Transfer sayısını minimize eden rota
            var planlayici = new RotaPlanlayici(duraklar);
            return planlayici.EnKisaRota(baslangic.Id, hedef.Id, yolcu, aktarmaAgirlik: 10.0);
        }
    }

    public class RotaPlanlayici
    {
        private readonly List<Durak> _duraklar;
        private readonly Dictionary<string, double> _mesafeler;
        private readonly Dictionary<string, string> _oncekiDuraklar;
        private readonly HashSet<string> _ziyaretEdilmis;
        
        // Strateji listesi
        private readonly List<IRotaHesaplamaStratejisi> _stratejiler = new List<IRotaHesaplamaStratejisi>
        {
            new SadeceOtobusStratejisi(),
            new SadeceTramvayStratejisi(),
            new TramvayOncelikliStrateji(),
            new EnAzAktarmaliStrateji()
        };

        public RotaPlanlayici(List<Durak> duraklar)
        {
            _duraklar = duraklar ?? new List<Durak>();
            _mesafeler = new Dictionary<string, double>();
            _oncekiDuraklar = new Dictionary<string, string>();
            _ziyaretEdilmis = new HashSet<string>();
        }
        
        // Tüm stratejilerle rota hesapla ve sonuçları döndür
        public Dictionary<string, List<string>> TumStratejiRotalariniHesapla(
            Durak baslangic, 
            Durak hedef, 
            Yolcu yolcu)
        {
            var sonuclar = new Dictionary<string, List<string>>();
            
            foreach (var strateji in _stratejiler)
            {
                try
                {
                    var rota = strateji.RotaHesapla(baslangic, hedef, yolcu, _duraklar);
                    sonuclar[strateji.StratejiAdi] = rota;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Strateji '{strateji.StratejiAdi}' hatası: {ex.Message}");
                    sonuclar[strateji.StratejiAdi] = new List<string>();
                }
            }
            
            return sonuclar;
        }

        public List<string> EnKisaRota(string baslangicId, string hedefId, Yolcu yolcu, double aktarmaAgirlik = 5.0)
        {
            try
            {
                Console.WriteLine($"RotaPlanlayici.EnKisaRota başladı: {baslangicId} -> {hedefId}, Yolcu tipi: {yolcu?.Tip ?? "Bilinmiyor"}");

                if (string.IsNullOrEmpty(baslangicId) || string.IsNullOrEmpty(hedefId) || yolcu == null)
                {
                    Console.WriteLine("HATA: Başlangıç ID, hedef ID veya yolcu null!");
                    return new List<string>();
                }

                var baslangicDurak = _duraklar.FirstOrDefault(d => d.Id == baslangicId);
                var hedefDurak = _duraklar.FirstOrDefault(d => d.Id == hedefId);

                if (baslangicDurak == null || hedefDurak == null)
                {
                    Console.WriteLine($"HATA: Başlangıç veya hedef durak bulunamadı! Başlangıç ID: {baslangicId}, Hedef ID: {hedefId}");
                    return new List<string>();
                }

                _mesafeler.Clear();
                _oncekiDuraklar.Clear();
                _ziyaretEdilmis.Clear();

                foreach (var durak in _duraklar)
                {
                    _mesafeler[durak.Id] = double.MaxValue;
                }

                _mesafeler[baslangicId] = 0;

                // Dijkstra algoritması ile en kısa yolu bul
                while (_ziyaretEdilmis.Count < _duraklar.Count)
                {
                    var simdikiDurak = EnKucukMesafeliDuragiBul();
                    if (simdikiDurak == null) break;
                    
                    string oncekiDurakTipi = string.Empty;
                    if (_oncekiDuraklar.ContainsKey(simdikiDurak.Id))
                    {
                        var oncekiDurakId = _oncekiDuraklar[simdikiDurak.Id];
                        var oncekiDurak = _duraklar.FirstOrDefault(d => d.Id == oncekiDurakId);
                        if (oncekiDurak != null)
                        {
                            oncekiDurakTipi = oncekiDurak.Type;
                        }
                    }

                    _ziyaretEdilmis.Add(simdikiDurak.Id);

                    if (simdikiDurak.Id == hedefId)
                    {
                        // Hedefe ulaşıldı, rotayı oluştur ve dön
                        return RotayiOlustur(baslangicId, hedefId);
                    }

                    // Komşu düğümleri kontrol et
                    foreach (var baglanti in simdikiDurak.NextStops)
                    {
                        if (string.IsNullOrEmpty(baglanti.StopId))
                            continue;

                        var komsuDurak = _duraklar.FirstOrDefault(d => d.Id == baglanti.StopId);
                        if (komsuDurak == null || _ziyaretEdilmis.Contains(komsuDurak.Id))
                            continue;

                        // Aktarma ağırlığı ekleyerek farklı tipte araçlar arasında geçiş maliyetini artır
                        double aktarmaMaliyeti = 0;
                        if (!string.IsNullOrEmpty(oncekiDurakTipi) && komsuDurak.Type != oncekiDurakTipi)
                        {
                            aktarmaMaliyeti = aktarmaAgirlik; // Aktarma ağırlığını parametre olarak al
                        }

                        double yeniMesafe = _mesafeler[simdikiDurak.Id] + baglanti.Mesafe + aktarmaMaliyeti;
                        if (yeniMesafe < _mesafeler[komsuDurak.Id])
                        {
                            _mesafeler[komsuDurak.Id] = yeniMesafe;
                            _oncekiDuraklar[komsuDurak.Id] = simdikiDurak.Id;
                        }
                    }

                    // Transfer kontrolü
                    if (simdikiDurak.Transfer != null && !string.IsNullOrEmpty(simdikiDurak.Transfer.TransferStopId))
                    {
                        var transferDurak = _duraklar.FirstOrDefault(d => d.Id == simdikiDurak.Transfer.TransferStopId);
                        if (transferDurak != null && !_ziyaretEdilmis.Contains(transferDurak.Id))
                        {
                            double transferMesafe = _mesafeler[simdikiDurak.Id] + aktarmaAgirlik; // Transfer için ek maliyet
                            if (transferMesafe < _mesafeler[transferDurak.Id])
                            {
                                _mesafeler[transferDurak.Id] = transferMesafe;
                                _oncekiDuraklar[transferDurak.Id] = simdikiDurak.Id;
                            }
                        }
                    }
                }

                // Hedefe ulaşılamadı
                Console.WriteLine($"UYARI: {baslangicId} -> {hedefId} için rota bulunamadı!");
                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EnKisaRota hatası: {ex}");
                return new List<string>();
            }
        }

        private Durak? EnKucukMesafeliDuragiBul()
        {
            double enKucukMesafe = double.MaxValue;
            Durak? enKucukDurak = null;

            foreach (var durak in _duraklar)
            {
                if (_ziyaretEdilmis.Contains(durak.Id))
                    continue;

                if (!_mesafeler.ContainsKey(durak.Id))
                    continue;

                if (_mesafeler[durak.Id] < enKucukMesafe)
                {
                    enKucukMesafe = _mesafeler[durak.Id];
                    enKucukDurak = durak;
                }
            }

            return enKucukDurak;
        }

        private List<string> RotayiOlustur(string baslangicId, string hedefId)
        {
            var rota = new List<string>();
            string simdikiId = hedefId;

            // Hedeften başlangıca doğru geriye doğru rotayı izle
            while (simdikiId != baslangicId)
            {
                if (!_oncekiDuraklar.ContainsKey(simdikiId))
                {
                    Console.WriteLine($"HATA: Rota oluşturulurken kopuk zincir: {simdikiId} için önceki durak bulunamadı!");
                    return new List<string>();
                }

                var durak = _duraklar.FirstOrDefault(d => d.Id == simdikiId);
                if (durak != null)
                {
                    rota.Add(durak.Id);
                }

                simdikiId = _oncekiDuraklar[simdikiId];
            }

            // Başlangıç durağını ekle
            var baslangicDuragi = _duraklar.FirstOrDefault(d => d.Id == baslangicId);
            if (baslangicDuragi != null)
            {
                rota.Add(baslangicDuragi.Id);
            }

            // Listeyi ters çevir (başlangıçtan sona doğru)
            rota.Reverse();
            return rota;
        }
        
        public static double DüzeltKoordinat(double coordinate)
        {
            return coordinate / 1000000.0;
        }
    }
}
