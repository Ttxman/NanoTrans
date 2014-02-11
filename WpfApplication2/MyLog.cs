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
        public static MyLog Log
        {
            get { return m_log; }
        }

        private static MyLog m_log = new MyLog();
        public static void LogujChybu(Exception e)
        {
            m_log.intLogujChybu(e);
        }
        public ArrayList seznamChyb = new ArrayList();



        private MyLog()
        {
            //constructor
        }

        private void intLogujChybu(Exception e)
        {
            try
            {
                seznamChyb.Add(e);

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }

        }

    }
}
