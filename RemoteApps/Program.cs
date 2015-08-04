using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Web.Security;

namespace RemoteApps
{
    class RemoteApp
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string PngURI { get; set; }
        public string RdpURI { get; set; }
    }

    static class Program
    {
        static Uri remoteResources;
        static string xmlns = "http://schemas.microsoft.com/ts/2007/05/tswf";
        static string raPath = "C:\\RA\\";

        static void Main(string[] args)
        {
            Console.Write("uri: ");
            remoteResources = new Uri(Console.ReadLine());
            Console.Write("user: ");
            string user = Console.ReadLine();
            Console.Write("password: ");
            string pass = Console.ReadLine();
            Console.Write("domain: ");
            string domain = Console.ReadLine();

            //RemoteApp and Desktop Connections uses HTTPS to connect to the server. 
            //In order to connect properly, the client operating system must trust the SSL certificate of the RD Web Access server. 
            //Also, the server name in the URL must match the one in the server’s SSL certificate

            // User credentials to access the connection. Fill in <username>, <password>, <domainname> with your user credentials.
            NetworkCredential networkCredential = new NetworkCredential(user, pass, domain);

            string cookie = GetFormsAuthenticationCookie(remoteResources.ToString(), networkCredential);
            string xml = GetConnectionXml(remoteResources.ToString(), cookie);

            //Fill in your code to parse the connection, and to download resource files and associated icon & image files. Use earlier cache cookie for authentication. 
            XmlDocument xd = new XmlDocument();
            xd.LoadXml(xml);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xd.NameTable);
            nsmgr.AddNamespace("res", xmlns);

            var apps = xd.SelectNodes("//res:Resource[contains(@Type,'RemoteApp')]", nsmgr);

            foreach (XmlNode app in apps)
            {
                RemoteApp ra = new RemoteApp
                {
                    Id = app.Attributes["ID"].InnerText,
                    Title = app.Attributes["Title"].InnerText,
                    PngURI = app.SelectSingleNode("res:Icons/res:Icon32[contains(@FileType,'Png')]", nsmgr).Attributes["FileURL"].InnerText,
                    RdpURI = app.SelectSingleNode("res:HostingTerminalServers/res:HostingTerminalServer/res:ResourceFile[contains(@FileExtension,'.rdp')]", nsmgr).Attributes["URL"].InnerText
                };
                // place the rdp file + icon in the directory based on id
                var path = Path.Combine(raPath, ra.Id);
                var ipath = Path.Combine(path, ra.Id + ".png");
                var rpath = Path.Combine(path, ra.Id + ".rdp");
                Directory.CreateDirectory(path);

                WriteFile(new Uri(remoteResources, ra.PngURI).ToString(), cookie, ipath);
                WriteFile(new Uri(remoteResources, ra.RdpURI).ToString(), cookie, rpath);
            }

            Console.ReadLine();
        }

        private static string GetFormsAuthenticationCookie(string connectionUrl, NetworkCredential networkCredential)
        {
            //
            // Request connection page is protected by Forms Authentication Cookie. So making a request to that page will be redirected to login page
            // The login page is
            //
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(connectionUrl);
            CredentialCache credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(connectionUrl), "Negotiate", networkCredential);
            httpWebRequest.Credentials = credentialCache;
            httpWebRequest.AllowAutoRedirect = true;

            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string formsAuthenticationCookie;

            using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                formsAuthenticationCookie = streamReader.ReadToEnd();
                streamReader.Close();
            }

            return formsAuthenticationCookie;
        }

        private static string GetConnectionXml(string connectionUrl, string formsAuthenticationCookie)
        {
            //
            // Request connection page
            //
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(connectionUrl);
            httpWebRequest.CookieContainer = new CookieContainer();

            //
            // Set Froms Authentication Cookie
            //
            httpWebRequest.CookieContainer.Add(new Cookie(FormsAuthentication.FormsCookieName, formsAuthenticationCookie, "/", httpWebRequest.RequestUri.Host));

            // Get response
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string connectionXml;

            using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                connectionXml = streamReader.ReadToEnd();
            }

            return connectionXml;
        }

        private static void WriteFile(string connectionUrl, string formsAuthenticationCookie, string path)
        {
            //
            // Request connection page
            //
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(connectionUrl);
            httpWebRequest.CookieContainer = new CookieContainer();

            //
            // Set Froms Authentication Cookie
            //
            httpWebRequest.CookieContainer.Add(new Cookie(FormsAuthentication.FormsCookieName, formsAuthenticationCookie, "/", httpWebRequest.RequestUri.Host));

            // Get response
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            using (Stream s = httpWebResponse.GetResponseStream())
            {
                File.WriteAllBytes(path, s.ReadFully());
            }
        }

        public static byte[] ReadFully(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
