namespace KOÜ_Ulaşım.Models
{
    public abstract class Yolcu
    {
        protected Yolcu()
        {
            Tip = "Normal";
        }

        public string Tip { get; set; }
        public abstract double IndirimOrani();
    }

    public class Ogrenci : Yolcu
    {
        public Ogrenci()
        {
            Tip = "Öğrenci";
        }

        public override double IndirimOrani() => 0.5; // %50 indirim
    }

    public class Yasli : Yolcu
    {
        public Yasli()
        {
            Tip = "65+";
        }

        public override double IndirimOrani() => 1.0; // Ücretsiz
    }

    public class Genel : Yolcu
    {
        public Genel()
        {
            Tip = "Normal";
        }

        public override double IndirimOrani() => 0.0; // İndirim yok
    }

    public class Ogretmen : Yolcu
    {
        public Ogretmen()
        {
            Tip = "Öğretmen";
        }

        public override double IndirimOrani() => 0.3; // %30 indirim
    }

    public class Engelli : Yolcu
    {
        public Engelli()
        {
            Tip = "Engelli";
        }

        public override double IndirimOrani() => 1.0; // Ücretsiz
    }
}
