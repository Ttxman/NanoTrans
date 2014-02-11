//using System.Linq;


namespace NanoTrans
{
    public class MyTag
    {
        public int tKapitola;
        public int tSekce;
        public int tOdstavec;
        /// <summary>
        /// typ elementu -  0 normalni, 1 - foneticky, atd... vychozi je normalni
        /// </summary>
        public MyEnumTypElementu tTypElementu;
        public object tSender;

        /// <summary>
        /// vraci true, pokud tag reprezentuje kapitolu
        /// </summary>
        public bool JeKapitola { get { if (tKapitola >= 0 && tSekce < 0 && tOdstavec < 0) return true; else return false; } }
        /// <summary>
        /// vraci true, pokud tag reprezentuje sekci
        /// </summary>
        public bool JeSekce { get { if (tKapitola >= 0 && tSekce >= 0 && tOdstavec < 0) return true; else return false; } }
        /// <summary>
        /// vraci true, pokud tag reprezentuje odstavec
        /// </summary>
        public bool JeOdstavec { get { if (tKapitola >= 0 && tSekce >= 0 && tOdstavec >= 0) return true; else return false; } }

        /// <summary>
        /// kopie tagu
        /// </summary>
        /// <param name="aKopie"></param>
        public MyTag(MyTag aKopie)
        {
            if (aKopie != null)
            {
                this.tKapitola = aKopie.tKapitola;
                this.tSekce = aKopie.tSekce;
                this.tOdstavec = aKopie.tOdstavec;
                this.tTypElementu = aKopie.tTypElementu;
                this.tSender = aKopie.tSender;
            }
            else
            {
                this.tKapitola = -1;
                this.tSekce = -1;
                this.tOdstavec = -1;
                this.tTypElementu = MyEnumTypElementu.normalni;
                this.tSender = null;
            }
        }

        public MyTag()
        {
            this.tKapitola = -1;
            this.tSekce = -1;
            this.tOdstavec = -1;
            this.tTypElementu = MyEnumTypElementu.normalni;
            this.tSender = null;
        }

        public MyTag(int aKapitola, int aSekce, int aOdstavec)
        {
            this.tKapitola = aKapitola;
            this.tSekce = aSekce;
            this.tOdstavec = aOdstavec;
            this.tTypElementu = MyEnumTypElementu.normalni;
            this.tSender = null;
        }

        public MyTag(int aKapitola, int aSekce, int aOdstavec, object aSender)
        {
            this.tKapitola = aKapitola;
            this.tSekce = aSekce;
            this.tOdstavec = aOdstavec;
            this.tTypElementu = MyEnumTypElementu.normalni;
            this.tSender = aSender;
        }

        public MyTag(int aKapitola, int aSekce, int aOdstavec, MyEnumTypElementu aTypElementu, object aSender)
        {
            this.tKapitola = aKapitola;
            this.tSekce = aSekce;
            this.tOdstavec = aOdstavec;
            this.tTypElementu = aTypElementu;
            this.tSender = aSender;
        }

        //public override bool Equals(object obj)
        //{
        //    if (obj is MyTag)
        //    {
        //        MyTag mt = (MyTag)obj;
        //        return this == mt;
        //    }

        //    return false;
        //}

        //public static bool operator == (MyTag a, MyTag b)
        //{
            
        //    if ((object)a == null || (object)b == null)
        //        return false;
        //    return a.tKapitola == b.tKapitola && a.tOdstavec == b.tOdstavec && a.tSekce == b.tSekce;
        //}

        //public static bool operator !=(MyTag a, MyTag b)
        //{
        //    return !(a == b);
        //}

    }
}