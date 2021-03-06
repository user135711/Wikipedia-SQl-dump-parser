﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace WpSqlDumpParser
{
    public static class DumpsManager
    {
        private static readonly string SitematrixUrl = "http://en.wikipedia.org/w/api.php?action=sitematrix&format=xml";
        private static readonly string DumpsUrl = "http://dumps.wikimedia.org/";
        private static readonly string UserAgent = "[[User:Svick]] Wikipedia SQL dump parser";

        private static WebClient WC
        {
            get
            {
                var wc = new WebClient();
                wc.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                return wc;
            }
        }

        private static readonly Lazy<IEnumerable<string>> m_wikipedias = new Lazy<IEnumerable<string>>(GetWikipedias);

        public static IEnumerable<string> Wikipedias { get { return m_wikipedias.Value; } }

        private static IEnumerable<string> GetWikipedias()
        {
            XDocument doc;
            using (var sitemaxtrixStream = WC.OpenRead(SitematrixUrl))
            {
                doc = XDocument.Load(sitemaxtrixStream);
            }

            return from lang in doc.Element("api").Element("sitematrix").Elements("language")
                   from site in lang.Element("site").Elements("site")
                   where site.Attribute("closed") == null
                         && site.Attribute("code").Value == "wiki"
                   select lang.Attribute("code").Value;
        }

        public static DateTime GetLastDumpDate(string wiki)
        {
            string directoryListing = null;
            while (directoryListing == null)
            {
                try
                {
                    directoryListing = WC.DownloadString(DumpsUrl + wiki + '/');
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            directoryListing = directoryListing.Replace("&nbsp;", " ");

            XNamespace ns = "http://www.w3.org/1999/xhtml";

            // to make sure DTDs are not downloaded
            var xmlReader = XmlReader.Create(
                new StringReader(directoryListing),
                new XmlReaderSettings { XmlResolver = null, DtdProcessing = DtdProcessing.Ignore });

            XDocument doc = XDocument.Load(xmlReader);

            return (from elem in doc.Descendants(ns + "a")
                    select DateTimeExtensions.ParseDate(elem.Value)
                    into date
                    where date != null
                    select date.Value).Max();
        }
    }
}