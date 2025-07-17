using System;
using System.Collections.Generic;

namespace KOÜ_Ulaşım.Models
{
    // Araç soyut sınıfı - Tüm ulaşım araçları için temel sınıf
    public abstract class Arac
    {
        public string Tip { get; protected set; }
        public double BirimUcret { get; protected set; }
        
        // Ücret hesaplama yöntemi (varsayılan implementasyon)
        public virtual double UcretHesapla(double mesafe, Yolcu yolcu)
        {
            // İndirim uygulanmış birim ücret
            double indirimliUcret = BirimUcret * (1 - yolcu.IndirimOrani());
            return indirimliUcret * mesafe;
        }
        
        // Seyahat süresi hesaplama (dakika cinsinden)
        public abstract int SureHesapla(double mesafe);
        
        // Araç tipi bilgisi döndür
        public override string ToString()
        {
            return Tip;
        }
    }
    
    // Otobüs sınıfı
    public class Otobus : Arac
    {
        private const int ORTALAMA_HIZ = 30; // km/saat
        private const int DURAK_BEKLEME = 1; // dakika
        
        public Otobus()
        {
            Tip = "Otobüs";
            BirimUcret = 7.5; // TL/yolculuk
        }
        
        public override int SureHesapla(double mesafe)
        {
            // Otobüsün ortalama hızına göre süre hesaplama (dakika)
            double saatCinsinden = mesafe / ORTALAMA_HIZ;
            int dakika = (int)(saatCinsinden * 60);
            
            // Her 2 km'de bir durak var varsayalım ve her durakta bekleme süresi ekleyelim
            int durakSayisi = (int)(mesafe / 2);
            return dakika + (durakSayisi * DURAK_BEKLEME);
        }
    }
    
    // Tramvay sınıfı
    public class Tramvay : Arac
    {
        private const int ORTALAMA_HIZ = 35; // km/saat
        private const int DURAK_BEKLEME = 1; // dakika
        
        public Tramvay()
        {
            Tip = "Tramvay";
            BirimUcret = 7.5; // TL/yolculuk
        }
        
        public override int SureHesapla(double mesafe)
        {
            // Tramvayın ortalama hızına göre süre hesaplama (dakika)
            double saatCinsinden = mesafe / ORTALAMA_HIZ;
            int dakika = (int)(saatCinsinden * 60);
            
            // Her 2 km'de bir durak var varsayalım ve her durakta bekleme süresi ekleyelim
            int durakSayisi = (int)(mesafe / 2);
            return dakika + (durakSayisi * DURAK_BEKLEME);
        }
    }
} 