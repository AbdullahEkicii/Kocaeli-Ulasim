using System;
using KOÜ_Ulaşım.Models;

namespace KOÜ_Ulaşım.Services
{
    public class TransferServisi
    {
        public static double AktarmaUcretiHesapla(Durak baslangic, Durak hedef)
        {
            if (baslangic.Transfer != null && baslangic.Transfer.TransferStopId == hedef.Id)
            {
                return baslangic.Transfer.TransferUcret;
            }
            return 0; // Aktarma yoksa ücret alınmaz
        }

        public static int AktarmaSuresiHesapla(Durak baslangic, Durak hedef)
        {
            if (baslangic.Transfer != null && baslangic.Transfer.TransferStopId == hedef.Id)
            {
                return baslangic.Transfer.TransferSure;
            }
            return 0; // Aktarma yoksa süre eklenmez
        }
    }
}
