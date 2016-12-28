using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace szamle
{
    class InvoiceIpc
    {
        public List<Invoice> invoices { get; set; }

        public InvoiceIpc()
        {
            invoices = new List<Invoice>();
        }

        public void addNewInvoice(
            String _href, 
            String _szolg = null, 
            String _kibocsato = null, 
            String _szlaszam = null, 
            String _kibocsatas = null, 
            String _vegosszeg = null, 
            String _fizhatido = null, 
            String _fizetendo = null, 
            String _allapot = null)
        {
            Invoice i = new Invoice()
            {
                href = _href,
                szolgaltato = _szolg,
                kibocsato = _kibocsato,
                szamlaszam = _szlaszam,
                kibocsatas = _kibocsatas,
                vegosszeg = _vegosszeg,
                fizhatido = _fizhatido,
                fizetendo = _fizetendo,
                allapot = _allapot
            };
            invoices.Add(i);
        }

        public String testBound()
        {
            return "hello world";
        }
    }
}
