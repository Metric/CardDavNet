using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using CardDav.Card;
using CardDav.Response;

namespace CardDav
{
    public class Client
    {
        public static string Version = "0.1";
        public static string UserAgent = "CardDav .NET/";

        private static char[] vCardIdChars = new char[] {'0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F'};

        private string serverUrl;
        private string authentication;
        private string username;
        private string password;

        public Client() { }

        public Client(string url)
        {
            this.serverUrl = url;

            if (!this.serverUrl.Substring(this.serverUrl.Length - 1).Equals("/"))
                this.serverUrl = this.serverUrl + "/";
        }

        public Client(string url, string username, string password)
        {
            this.serverUrl = url;

            if (!this.serverUrl.Substring(this.serverUrl.Length - 1).Equals("/"))
                this.serverUrl = this.serverUrl + "/";

            this.username = username;
            this.password = password;
            this.authentication = this.username + ":" + this.password;
        }

        public void SetAuthentication(string username, string password)
        {
            this.username = username;
            this.password = password;
            this.authentication = this.username + ":" + this.password;
        }

        public void SetServer(string url)
        {
            this.serverUrl = url;
        }

        public CardDavResponse Get()
        {
            string result = this.cleanRawResponse(this.GetRaw());

            return this.SimplifyResponse(result);
        }

        public string GetRaw()
        {
            Dictionary<string, string> result = this.Query(this.serverUrl, "PROPFIND");

            if (result.ContainsKey("status"))
            {
                string status = result["status"];

                if (status.Equals("200") || status.Equals("207"))
                {
                    if (result.ContainsKey("response"))
                    {
                        return result["response"];
                    }
                    else
                    {
                        throw new HTTPException("Arrgg. No response returned from the server!");
                    }
                }
                else
                {
                    throw new HTTPException("Whoops something went wrong! The server returned a status code of: " + status);
                }
            }
            else
            {
                throw new HTTPException("No status code returned from HTTP Request");
            }
        }

        public vCard GetVCard(string id)
        {
            id = id.Replace(".vcf", "");

            Dictionary<string, string> results = this.Query(this.serverUrl + id, "GET");

            if (results.ContainsKey("status"))
            {
                string status = results["status"];

                if (status.Equals("200") || status.Equals("207") || status.Equals("204") || status.Equals("201") || status.ToLower().Equals("ok"))
                {
                    if (results.ContainsKey("response"))
                    {
                        StringReader reader = new StringReader(results["response"]);
                        vCard card = new vCard();
                        card.Parse(reader);

                        return card;
                    }
                    else
                    {
                        throw new HTTPException("Arrgg. No response returned from the server!");
                    }
                }
                else
                {
                    throw new HTTPException("Whoops something went wrong! The server returned a status code of: " + status);
                }
            }
            else
            {
                throw new HTTPException("No status code returned from HTTP Request");
            }
        }

        public bool Delete(string id)
        {
            Dictionary<string, string> results = this.Query(this.serverUrl + id, "DELETE");

            if (results.ContainsKey("status"))
            {
                string status = results["status"];

                if (status.Equals("200") || status.Equals("207") || status.Equals("204") ||  status.Equals("201") || status.ToLower().Equals("ok"))
                    return true;
            }

            return false;
        }

        public string Add(vCard vcard)
        {
            string vCardId = null;

            vCardId = this.GenerateVCardId();

            Dictionary<string, string> results = this.Query(this.serverUrl + vCardId, "PUT", vcard.ToString(), "text/vcard");

            if (results.ContainsKey("status"))
            {
                string status = results["status"];

                if (status.Equals("200") || status.Equals("207") || status.Equals("204") || status.Equals("201") || status.ToLower().Equals("ok"))
                {
                    return vCardId;
                }
                else
                {
                    throw new HTTPException("Whoops something went wrong! The server returned a status code of: " + status);
                }
            }
            else
            {
                throw new HTTPException("No status code returned from HTTP Request");
            }
        }

        public bool Update(vCard vcard, string id)
        {
            Dictionary<string, string> results = this.Query(this.serverUrl + id, "PUT", vcard.ToString(), "text/vcard");

            if (results.ContainsKey("status"))
            {
                string status = results["status"];

                if (status.Equals("200") || status.Equals("207") || status.Equals("204") || status.Equals("201") || status.ToLower().Equals("ok"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public bool CanConnect()
        {
            Dictionary<string, string> results = this.Query(this.serverUrl, "OPTIONS");

            if (results.ContainsKey("status"))
            {
                string status = results["status"];

                if (status.Equals("200"))
                    return true;
            }

            return false;
        }

        private Dictionary<string, string> Query(string url, string method, string content = null, string contentType = null)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            Dictionary<string, string> queryResponse = new Dictionary<string, string>();

            if(this.authentication != null)
            {
                request.UserAgent = UserAgent + Version;
                request.Method = method;
                request.PreAuthenticate = true;
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(this.authentication)));

                if (contentType != null)
                {
                    request.ContentType = contentType;
                }

                if (content != null)
                {
                    byte[] contentBytes = Encoding.UTF8.GetBytes(content);
                    request.ContentLength = contentBytes.Length;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(contentBytes, 0, contentBytes.Length);
                    requestStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                queryResponse.Add("status", response.StatusCode.ToString());

                Stream responseStream = response.GetResponseStream();

                byte[] buffer = new byte[2048];

                StringBuilder responseBuilder = new StringBuilder();

                int amountRead = 0;

                amountRead = responseStream.Read(buffer, 0, 2048);

                while (amountRead > 0)
                {
                    string tempString = Encoding.UTF8.GetString(buffer, 0, amountRead);
                    responseBuilder.Append(tempString);

                    amountRead = responseStream.Read(buffer, 0, 2048);
                }

                queryResponse.Add("response", responseBuilder.ToString());
            }
            else
            {
                throw new HTTPException("No authentication information provided");
            }

            return queryResponse;
        }

        private string GenerateVCardId()
        {
            string id = "";

            Random random = new Random();

            for (int i = 0; i <= 25; i++)
            {
                if (i == 8 || i == 17)
                {
                    id += "-";
                }
                else
                {
                    int randomCharacter = random.Next(0, vCardIdChars.Length - 1);
                    id += vCardIdChars[randomCharacter];
                }
            }

            try
            {
                Dictionary<string, string> response = this.Query(this.serverUrl + id + ".vcf", "GET");

                if (!response["status"].Equals("404"))
                {
                    id = this.GenerateVCardId();
                }

                return id;
            }
            catch (Exception e)
            {
                throw new HTTPException("Could not generate new vCard Id");
            }
           
        }

        private string cleanRawResponse(string response)
        {
            string results = response.Replace("D:", "").Replace("d:", "").Replace("C:", "").Replace("c:", "");

            return results;
        }

        private CardDavResponse SimplifyResponse(string results)
        {
            XmlDocument document = new XmlDocument();
            document.Load(new StringReader(results));

            XmlNodeList responses = document.GetElementsByTagName("response");
            CardDavResponse response = new CardDavResponse();
            Uri serverUri = new Uri(this.serverUrl);

            foreach (XmlNode node in responses)
            {
                string contentType = CardDavParser.GetNodeContents(node, "getcontenttype").FirstOrDefault();
                string href = CardDavParser.GetNodeContents(node, "href").FirstOrDefault();

                if (contentType.IndexOf("vcard") > -1 || href.IndexOf("vcf") > -1)
                {
                    //It is a vcard element

                    Uri hrefUri = new Uri("https://localhost/" + href);
                    string id = Path.GetFileName(hrefUri.LocalPath).Replace(".vcf", "");
                    string hrefWithoutIdWithoutVcf = href.Replace(id, "").Replace(".vcf", "");
                    Uri addressBookUrlForCard = new Uri(serverUri.Scheme + "://" + serverUri.Host + hrefWithoutIdWithoutVcf);

                    if (!String.IsNullOrEmpty(id))
                    {
                        CardElement card = new CardElement();

                        card.Id = id;
                        card.Url = addressBookUrlForCard;
                        card.eTag = CardDavParser.GetNodeContents(node, "getetag").FirstOrDefault().Replace("\"", "");
                        card.Modified = DateTime.Parse(CardDavParser.GetNodeContents(node, "getlastmodified").FirstOrDefault());

                        response.Cards.Add(card);
                    }
                }
                else
                {
                    //It is a address book element

                    if (!serverUri.PathAndQuery.Equals(href))
                    {
                        string url = serverUri.Scheme + "://" + serverUri.Host + href;

                        AddressBookElement addressBook = new AddressBookElement();

                        addressBook.Url = new Uri(url);
                        addressBook.DisplayName = CardDavParser.GetNodeContents(node, "displayname").FirstOrDefault();
                        addressBook.Modified = DateTime.Parse(CardDavParser.GetNodeContents(node, "getlastmodified").FirstOrDefault());

                        response.AddressBooks.Add(addressBook);
                    }
                }
            }

            return response;
        }
    }
}
