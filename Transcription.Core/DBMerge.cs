using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TranscriptionCore
{
    public class DBMerge
    {
        public string DBID { get; private set; }
        public DBType DBtype { get; private set; }

        public DBMerge(string DBID, DBType type)
        {
            this.DBID = DBID;
            this.DBtype = type;
        }

        internal XElement Serialize()
        {
            string val;
            if (this.DBtype == DBType.Api)
                val = "api";
            else if (DBtype == DBType.User)
                val = "user";
            else
                val = "file";

            return new XElement("m",
                new XAttribute("dbid", DBID),
                new XAttribute("dbtype",val));
        }
    }
}
