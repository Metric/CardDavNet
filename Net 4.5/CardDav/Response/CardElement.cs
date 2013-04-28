using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardDav.Response
{
    public class CardElement
    {
        public CardElement() { }

        public string Id
        {
            get;
            set;
        }

        public string eTag
        {
            get;
            set;
        }

        public DateTime Modified
        {
            get;
            set;
        }

        public Uri Url
        {
            get;
            set;
        }

        public string DavName
        {
            get;
            set;
        }

        public override string ToString()
        {
            return "ID: " + Id + " eTag: " + eTag + " Modified: " + Modified.ToShortDateString() + " Uri: " + Url.ToString();
        }
    }
}
