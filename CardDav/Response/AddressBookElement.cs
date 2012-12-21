using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardDav.Response
{
    public class AddressBookElement
    {
        public AddressBookElement() { }

        public string DisplayName
        {
            get;
            set;
        }

        public Uri Url
        {
            get;
            set;
        }

        public DateTime Modified
        {
            get;
            set;
        }

        public override string ToString()
        {
            return "Display Name: " + DisplayName + " Url: " + Url.ToString() + " Modified: " + Modified.ToShortDateString();
        }
    }
}
