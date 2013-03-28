using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using CardDav;
using CardDav.Card;
using CardDav.Response;

namespace CardDavDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CardDav.Client client = new CardDav.Client(this.urlTxt.Text, this.usernameTxt.Text, this.passwordTxt.Text);

            resultsTxt.Text = client.GetRaw();

            ///CardDavResponse response = client.Get();

            /** Output the results from the Listing **/

           // resultsTxt.Text = response.ToString();

            /** Set the client to the proper Address Book server URL **/
            //client.SetServer(response.AddressBooks.First().Url.ToString());
            //vCard card = client.GetVCard(response.Cards.First().Id);

            //resultsTxt.Text = resultsTxt.Text + "\r\n\r\n\r\n" + card.ToString();
        }
    }
}
