using System;
using KOÜ_Ulaşım.Models;

namespace KOÜ_Ulaşım.Services
{
    public static class TaksiServisi
    {
        private static IMesafeHesaplayici _mesafeHesaplayici = new HaversineMesafeHesaplayici();

        private const double VARSAYILAN_ACILIS = 15.0;
        private const double VARSAYILAN_KM_UCRETI = 10.0;
        private const double MINIMUM_UCRET = 50.0;

        public static void SetMesafeHesaplayici(IMesafeHesaplayici hesaplayici)
        {
            _mesafeHesaplayici = hesaplayici;
        }

        public static bool TaksiGerekliMi(double kullaniciLat, double kullaniciLon, double durakLat, double durakLon)
        {
            double mesafe = MesafeHesapla(kullaniciLat, kullaniciLon, durakLat, durakLon);
            Console.WriteLine($"\uD83D\uDCCF Başlangıç Taksi Mesafesi: {mesafe} km");

            const double TAKSI_ESIK = 3.0;
            return mesafe > TAKSI_ESIK + 0.05; // Küçük bir tolerans uygulanır
        }

        public static bool HedefeTaksiGerekliMi(double hedefLat, double hedefLon, double durakLat, double durakLon)
        {
            double mesafe = MesafeHesapla(hedefLat, hedefLon, durakLat, durakLon);
            Console.WriteLine($"\uD83D\uDCCF Hedef Taksi Mesafesi: {mesafe} km");

            const double TAKSI_ESIK = 3.0;
            return mesafe > TAKSI_ESIK + 0.05;
        }

        public static double MesafeHesapla(double lat1, double lon1, double lat2, double lon2)
        {
            return _mesafeHesaplayici.Hesapla(lat1, lon1, lat2, lon2);
        }

        public static double UcretHesapla(double mesafe)
        {
            try
            {
                var taxiInfo = VeriServisi.GetSehirVerisi()?.Taxi;

                double acilisUcreti = taxiInfo?.AcilisUcreti ?? VARSAYILAN_ACILIS;
                double kmBasinaUcret = taxiInfo?.KmBasinaUcret ?? VARSAYILAN_KM_UCRETI;

                double ucret = acilisUcreti + (mesafe * kmBasinaUcret);
                return Math.Max(ucret, MINIMUM_UCRET);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Taksi ücreti hesaplama hatası: {ex.Message}");
                double ucret = VARSAYILAN_ACILIS + (mesafe * VARSAYILAN_KM_UCRETI);
                return Math.Max(ucret, MINIMUM_UCRET);
            }
        }
    }

    public interface IMesafeHesaplayici
    {
        double Hesapla(double lat1, double lon1, double lat2, double lon2);
    }

    public class HaversineMesafeHesaplayici : IMesafeHesaplayici
    {
        public double Hesapla(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round(R * c, 2);
        }

        private double ToRadians(double derece)
        {
            return derece * Math.PI / 180;
        }
    }
}