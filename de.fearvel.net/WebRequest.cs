﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace de.fearvel.net
{
    public static class WebRequest
    {
        public static MemoryStream Download(string url)
        {
            MemoryStream ms;
            WebClient client = new WebClient();
            try
            {
                ms = new MemoryStream(client.DownloadData(url));
            }
            finally
            {
                client.Dispose();
            }
            return ms;
        }

        public static void DownloadToFile(string url, string path)
        {
            var ms = Download(url);
            var f = File.OpenWrite(path);
            ms.CopyTo(f);
            ms.Close();
            f.Close();
        }

        public static string SendPostRequest(Dictionary<string, string> data, string serverUrl, bool trustedCertificateOnly = true)
        {
            using (WebClient client = new WebClient())
            {
                if (!trustedCertificateOnly)
                    ServicePointManager.ServerCertificateValidationCallback = TrustCertificate;
                NameValueCollection postData = new NameValueCollection();
                foreach (var itm in data)
                {
                    postData.Add(itm.Key, itm.Value);
                }
                return Encoding.UTF8.GetString(client.UploadValues(serverUrl, postData));
            }
        }

        private static bool TrustCertificate(object sender, X509Certificate x509Certificate,
            X509Chain x509Chain, SslPolicyErrors sslPolicyErrors) => true; // all Certificates are accepted
    }
}