using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Collections;


namespace NanoTrans
{
    //trida zodpovedna za logovani udlosti a chyb programu
    public class MyLog
    {
        public ArrayList seznamChyb = new ArrayList();
        
        public MyLog()
        {
            //constructor
        }

        public void LogujChybu(Exception e)
        {
            try
            {
                seznamChyb.Add(e);

            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
            }

        }

    }
}
