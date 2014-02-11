using System;
using System.Collections.Generic;
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

        public static void LogujChybu(Exception e, bool rethrow)
        {
            m_log.intLogujChybu(e);
            if(rethrow)
                throw (e);
        }

        public static void LogujChybu(Exception e)
        {
            LogujChybu(e, false);
        }
        private List<Exception> m_seznamChyb = new List<Exception>();

        public static List<Exception> SeznamChyb
        {
            get { return m_log.m_seznamChyb; }
        }

        private MyLog()
        {
            //privatni konstruktor - singleton
        }

        private void intLogujChybu(Exception e)
        {
            try
            {
                m_seznamChyb.Add(e);

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }

        }

    }
}
