using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace szamle
{
    public class Invoice
    {
        public String szolgaltato { get; set; }
        public String kibocsato { get; set; }
        public String szamlaszam { get; set; }
        public String kibocsatas { get; set; }
        public String vegosszeg { get; set; }
        public String fizhatido { get; set; }
        public String fizetendo { get; set; }
        public String allapot { get; set; }
        public String href { get; set; }

        public String fileNameMask
        {
            get { return cleanUpFileName(String.Format("{0}_{1}_{2}", szolgaltato, szamlaszam, kibocsatas)); }
        }

        private String cleanUpFileName(String fname)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fname = fname.Replace(c, '_');
            }
            return fname;
        }
    }
}
