/*
 * Created by SharpDevelop.
 * User: Debe
 * Date: 30/12/2013
 * Time: 20:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

using HtmlAgilityPack;

namespace ConfBot.PlugIns
{
	/// <summary>
	/// Description of GoogleImageScraper.
	/// </summary>
	public static class GoogleImageScraper
	{
		
		//private const string googleQueryUrl = "http://www.google.it/search?tbm=isch&q={0}&oq={0}&safe=off";
		private const string googleQueryUrl = "http://www.google.it/search?tbm=isch&q={0}&safe=off&ijn=1&start={1}";
		private const string userAgent = @"Mozilla/5.0 (Windows NT 6.2; rv:22.0) Gecko/20130405 Firefox/23.0";
		
		public static IEnumerable<Uri> GetImages(string keyword, int iterations = 7)
		{
			ServicePointManager.ServerCertificateValidationCallback = Validator;
			
			for (int i = 0; i < iterations; i+=1) {
				var searchQueryUrl = string.Format(googleQueryUrl, HttpUtility.HtmlEncode(keyword), (i*100));
				var oReq = (HttpWebRequest)WebRequest.Create(searchQueryUrl);
				oReq.UserAgent = userAgent;
				var resp = (HttpWebResponse)oReq.GetResponse();
				var resultStream = resp.GetResponseStream();
				
				var doc = new HtmlDocument();
				doc.Load(resultStream);
				foreach (HtmlNode link in doc.DocumentNode.SelectNodes(@"//a[@href]"))
				{
					HtmlAttribute att = link.Attributes["href"];
					if (att == null) continue;
					
					if (att.Value.Contains("imgurl"))
					{
						//string queryString = new System.Uri(att.Value).Query;
						var decodedString = HttpUtility.HtmlDecode(att.Value);
						var queryDictionary = HttpUtility.ParseQueryString(decodedString, Encoding.UTF8);
						var imgUrl = queryDictionary["imgurl"];
						yield return new Uri(imgUrl);
					}
				}
			}
		}
		
		public static bool Validator (object sender, X509Certificate certificate, X509Chain chain,
		                              SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}
	}
}
