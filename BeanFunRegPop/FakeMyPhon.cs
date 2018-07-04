using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BeanFunRegPop
{
    public partial class FakeMyPhone : WebClient
    {
        public System.Net.CookieContainer CookieContainer;
        public Uri ResponseUri;
        public string errmsg;
        public string webtoken;
        public List<AccountList> accountList;
        bool redirect;
        public const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";

        public class AccountList
        {
            public string sacc;
            public string sotp;
            public string sname;
            public string screatetime;

            public AccountList()
            { this.sacc = null; this.sotp = null; this.sname = null; this.screatetime = null; }
            public AccountList(string sacc, string sotp, string sname, string screatetime = null)
            { this.sacc = sacc; this.sotp = sotp; this.sname = sname; this.screatetime = screatetime; }
        }

        public FakeMyPhone()
        {
            this.redirect = true;
            this.CookieContainer = new System.Net.CookieContainer();
            this.Headers.Set("User-Agent", userAgent);
            this.ResponseUri = null;
            this.errmsg = null;
            this.webtoken = null;
            this.accountList = new List<AccountList>();
            this.Encoding = Encoding.UTF8;
        }

        public string DownloadString(string Uri, Encoding Encoding)
        {
            var ret = (Encoding.GetString(base.DownloadData(Uri)));
            return ret;
        }

        public string DownloadString(string Uri)
        {
            this.Headers.Set("User-Agent", userAgent);
            var ret = base.DownloadString(Uri);
            return ret;
        }

        public byte[] UploadValues(string skey, NameValueCollection payload)
        {
            this.Headers.Set("User-Agent", userAgent);
            return base.UploadValues(skey, payload);
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest webRequest = base.GetWebRequest(address);
            HttpWebRequest request2 = webRequest as HttpWebRequest;

            if (request2 != null)
            {
                request2.CookieContainer = this.CookieContainer;
                request2.AllowAutoRedirect = this.redirect;
            }
            return webRequest;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse webResponse = base.GetWebResponse(request);
            this.ResponseUri = webResponse.ResponseUri;
            return webResponse;
        }

        public string GetCookie(string cookieName)
        {
            foreach (Cookie cookie in this.CookieContainer.GetCookies(new Uri("https://www.fakemyphone.com.tw/")))
            {
                if (cookie.Name == cookieName)
                {
                    return cookie.Value;
                }
            }
            return null;
        }
    }
}
