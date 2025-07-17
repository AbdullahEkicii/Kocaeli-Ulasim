namespace KOÜ_Ulaşım.Models
{
    public abstract class Odeme
    {
        public abstract bool OdemeYap(double tutar);
    }

    public class Nakit : Odeme
    {
        public override bool OdemeYap(double tutar)
        {
            Console.WriteLine($"✅ Nakit ödeme yapıldı: {tutar} TL");
            return true;
        }
    }

    public class KrediKarti : Odeme
    {
        public override bool OdemeYap(double tutar)
        {
            Console.WriteLine($"✅ Kredi Kartı ile ödeme yapıldı: {tutar} TL");
            return true;
        }
    }

    public class Kentkart : Odeme
    {
        public double Bakiye { get; set; }

        public Kentkart(double bakiye)
        {
            Bakiye = bakiye;
        }

        public override bool OdemeYap(double tutar)
        {
            if (Bakiye >= tutar)
            {
                Bakiye -= tutar;
                Console.WriteLine($"✅ Kentkart ile ödeme yapıldı! Kalan bakiye: {Bakiye} TL");
                return true;
            }
            Console.WriteLine("❌ Kentkart bakiyesi yetersiz!");
            return false;
        }
    }
}
