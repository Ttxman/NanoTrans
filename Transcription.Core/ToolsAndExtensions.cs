using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TranscriptionCore
{
    public static class ToolsAndExtensions
    {
        public static bool CheckRequiredAtributes(this XElement elm, params string[] attributes)
        {
            foreach (var a in attributes)
            {
                if (elm.Attribute(a) == null)
                    return false;
            }

            return true;
        }
    }
}
