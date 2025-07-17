using System;

namespace KOÜ_Ulaşım.Models
{
	public class Taksi : Arac
	{
		private const int ORTALAMA_HIZ = 50; // km/saat
		private const double ACILIS_UCRETI = 15.0; // TL
		private const double KM_BASINA_UCRET = 10.0; // TL/km
		private const double MINIMUM_UCRET = 50.0; // TL

		public Taksi()
		{
			Tip = "Taksi";
			BirimUcret = KM_BASINA_UCRET;
		}

		public override double UcretHesapla(double mesafe, Yolcu yolcu)
		{
			// Taksi ücretinde yolcu indirimi uygulanmaz
			double ucret = ACILIS_UCRETI + (BirimUcret * mesafe);
			return Math.Max(ucret, MINIMUM_UCRET); // Minimum 50 TL ücret
		}

		public override int SureHesapla(double mesafe)
		{
			// Taksinin ortalama hızına göre süre hesaplama (dakika)
			double saatCinsinden = mesafe / ORTALAMA_HIZ;
			return (int)(saatCinsinden * 60);
		}
	}
}
