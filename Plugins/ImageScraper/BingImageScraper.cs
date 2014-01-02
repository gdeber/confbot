/*
 * Created by SharpDevelop.
 * User: Debe
 * Date: 30/12/2013
 * Time: 19:59
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;

namespace ConfBot.PlugIns
{
	/// <summary>
	/// Description of BingImageScraper.
	/// </summary>
	public static class BingImageScraper
	{
		private const string bingQueryUrl = @"http://it.bing.com/images/search?q={0}&SafeSearch=off";
		private const string userAgent = @"Mozilla/5.0 (Windows NT 6.2; rv:22.0) Gecko/20130405 Firefox/23.0";
		private const string imgurlRegex = @"\bimgurl:""(?<url>.+?)"",";
		
		public static IEnumerable<Uri> GetImages(string query)
		{
			ServicePointManager.ServerCertificateValidationCallback = Validator;
			var searchQueryUrl = string.Format(bingQueryUrl, HttpUtility.HtmlEncode(query));
			var oReq = (HttpWebRequest)WebRequest.Create(searchQueryUrl);
			oReq.UserAgent = userAgent;
			var resp = (HttpWebResponse)oReq.GetResponse();
			var resultStream = resp.GetResponseStream();
			
			var doc = new HtmlDocument();
			doc.Load(resultStream);
			foreach (HtmlNode link in doc.DocumentNode.SelectNodes(@"//a[@href]"))
			{
				HtmlAttribute att = link.Attributes["m"];
				if (att == null) continue;
				
				if (att.Value.Contains("imgurl"))
				{
					var decodedAtt = HttpUtility.HtmlDecode(att.Value);
					var queryDictionary = HttpUtility.ParseQueryString(decodedAtt, Encoding.UTF8);
					var match = Regex.Match(decodedAtt, imgurlRegex);
					
					if (match != null)
					{
						var imgUrl = match.Groups["url"].Value;
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
