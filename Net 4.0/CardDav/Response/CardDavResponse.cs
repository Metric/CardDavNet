using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardDav.Response
{
    public class CardDavResponse
    {
        public CardDavResponse()
        {
            this.Cards = new List<CardElement>();
            this.AddressBooks = new List<AddressBookElement>();
        }

        public List<CardElement> Cards
        {
            get;
            set;
        }

        public List<AddressBookElement> AddressBooks
        {
            get;
            set;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("Cards\r\n");
            builder.Append("----------\r\n");

            foreach (CardElement element in Cards)
            {
                builder.Append(element.ToString() + "\r\n");
            }

            builder.Append("\r\nAddress Books\r\n");
            builder.Append("---------------\r\n");

            foreach (AddressBookElement element in AddressBooks)
            {
                builder.Append(element.ToString() + "\r\n");
            }

            return builder.ToString();
        }
    }
}
