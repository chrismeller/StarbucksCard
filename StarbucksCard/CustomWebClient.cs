using System;
using System.Net;

namespace StarbucksCard
{
    class CustomWebClient : WebClient
    {

        public CookieContainer Cookies = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.CookieContainer = Cookies;
                webRequest.AllowAutoRedirect = true;
            }

            return request;
        }

    }
}