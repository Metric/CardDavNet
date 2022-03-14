﻿using System;
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
        private string oAuth2AccessToken;

        private const string authorizationHeaderName = "Authorization";
        private const string oAuth2TokenType = "Bearer";

        public Client() { }

        public Client(string url)
        {
            SetServer(url);
        }

        /// <summary>
        /// Creates a new instance for connecting to a CardDAV endpoint, which uses OAuth 2.0 authorization.
        /// </summary>
        /// <param name="url">CardDAV endpoint URL</param>
        /// <param name="oAuth2AccessToken">OAuth 2.0 access token</param>
        public Client(string url, string oAuth2AccessToken)
        {
            SetServer(url);
            this.oAuth2AccessToken = oAuth2AccessToken;
        }

        public Client(string url, string username, string password)
        {
            SetServer(url);
            SetAuthentication(username, password);
        }

        public void SetAuthentication(string username, string password)
        {
            this.username = username;
            this.password = password;
            this.authentication = this.username + ":" + this.password;
        }

        public void SetServer(string url)
        {
            // make sure the url always ends with /
            this.serverUrl = url.EndsWith("/") ? url : url + "/";
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

                if (status.Equals("200") || status.Equals("207") || status.ToLower().Equals("ok"))
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

                if (status.Equals("200") || status.ToLower().Equals("ok"))
                    return true;
            }

            return false;
        }

        private Dictionary<string, string> Query(string url, string method, string content = null, string contentType = null)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(url);
            Dictionary<string, string> queryResponse = new Dictionary<string, string>();

            if(!string.IsNullOrEmpty(authentication) || !string.IsNullOrEmpty(oAuth2AccessToken))
            {
                request.UserAgent = UserAgent + Version;
                request.Method = method;
                request.PreAuthenticate = true;
                //request.Headers.Add(authorizationHeaderName, "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(this.authentication)));
                if (!string.IsNullOrEmpty(authentication))
                    request.Credentials = new NetworkCredential(this.username, this.password);
                else if (!string.IsNullOrEmpty(oAuth2AccessToken))
                    // if the CardDAV endpoint uses OAuth 2.0 authorization, set the appropriate header
                    request.Headers.Add(authorizationHeaderName, string.Format("{0} {1}", oAuth2TokenType, oAuth2AccessToken));

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
            catch (Exception)
            {
                throw new HTTPException("Could not generate new vCard Id");
            }
           
        }

        private string cleanRawResponse(string response)
        {
            return Regex.Replace(response, "((?<=</?)\\w+:(?<elem>\\w+)|\\w+:(?<elem>\\w+)(?==\"))","${elem}");
        }

        public string ServerAddress {
            get
            {
                Uri serverUri = new Uri(this.serverUrl);
                if (serverUri.Port != 80)
                {
                    return serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port;
                }

                return serverUri.Scheme + "://" + serverUri.Host;
            }
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
                XmlNode addressBookNode = CardDavParser.GetNodesByTagName(node, "addressbook").FirstOrDefault();
                string href = CardDavParser.GetNodeContents(node, "href").FirstOrDefault();

                if (!String.IsNullOrEmpty(contentType) && !String.IsNullOrEmpty(href) && (contentType.IndexOf("vcard") > -1 || href.IndexOf("vcf") > -1))
                {
                    //It is a vcard element

                    Uri hrefUri = new Uri("https://localhost/" + href);
                    string id = Path.GetFileName(hrefUri.LocalPath).Replace(".vcf", "");
                    string idWithExtension = Path.GetFileName(hrefUri.LocalPath);
                    string hrefWithoutIdWithoutVcf = href.Replace(id, "").Replace(".vcf", "");
                    Uri addressBookUrlForCard = new Uri(this.ServerAddress + hrefWithoutIdWithoutVcf);

                    if (!String.IsNullOrEmpty(id))
                    {
                        CardElement card = new CardElement();

                        card.Id = id;
                        card.DavName = idWithExtension;
                        card.Url = addressBookUrlForCard;

                        string etag = CardDavParser.GetNodeContents(node, "getetag").FirstOrDefault();

                        if(!String.IsNullOrEmpty(etag))
                            card.eTag = etag.Replace("\"", "");

                        string dateString = CardDavParser.GetNodeContents(node, "getlastmodified").FirstOrDefault();

                        if (!String.IsNullOrEmpty(dateString))
                            card.Modified = DateTime.Parse(dateString);
                        else
                            card.Modified = DateTime.Now;

                        response.Cards.Add(card);
                    }
                }
                else if(addressBookNode != null && !String.IsNullOrEmpty(href))
                {
                    //It is a address book element
                    string url = this.ServerAddress + href;

                    AddressBookElement addressBook = new AddressBookElement();

                    addressBook.Url = new Uri(url);
                    addressBook.DisplayName = CardDavParser.GetNodeContents(node, "displayname").FirstOrDefault();

                    string dateString = CardDavParser.GetNodeContents(node, "getlastmodified").FirstOrDefault();

                    if (!String.IsNullOrEmpty(dateString))
                        addressBook.Modified = DateTime.Parse(dateString);
                    else
                        addressBook.Modified = DateTime.Now;

                    response.AddressBooks.Add(addressBook);
                }
            }

            return response;
        }
    }
}
