/*
 * Created by SharpDevelop.
 * User: Debe
 * Date: 30/12/2013
 * Time: 18:49
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using System.Threading;

using ConfBot.Types;

namespace ConfBot.PlugIns
{
	[PlugInAttribute]
	public class ImageScraper : PlugIn
	{
		private bool Active = false;
		private Timer scrapeTimer;
		private List<string> queryKeywords;
		/// <summary>
		/// time between automatic image scraping (in minutes)
		/// </summary>
		private TimeSpan period;
		private bool alternative = false;
		
		public ImageScraper(IJabberClient jabberClient,IConfigManager configManager, ILogger logger) : base(jabberClient, configManager, logger)
		{
			//init command
			#region Command Initialization
			BotCmd tempCmd;
			// ImageScraper Command
			tempCmd.Command	= "imagescraper";
			tempCmd.Code	= Convert.ToInt32(Commands.ImageScraper);
			tempCmd.Admin	= true;
			tempCmd.Help	= "[on|off] attiva/disattiva lo scraper di immagini";
			listCmd.Add(tempCmd);
			tempCmd.Command	= "scrape";
			tempCmd.Code	= Convert.ToInt32(Commands.Scrape);
			tempCmd.Admin	= false;
			tempCmd.Help	= "fornisce un link di un'immagine";
			listCmd.Add(tempCmd);
			tempCmd.Command	= "scrapekeywords";
			tempCmd.Code	= Convert.ToInt32(Commands.ScrapeKeywords);
			tempCmd.Admin	= true;
			tempCmd.Help	= "[add <keyword>| remove <keyword>| list] aggiunge, rimuove o elenca le keywords in uso";
			listCmd.Add(tempCmd);
			#endregion
			Active	= (this._configManager.GetSetting("ImageScraper") == "on");
			var periodStr = this._configManager.GetSetting("ImageScraperPeriod");
			int periodInt;
			if (periodStr != string.Empty && Int32.TryParse(periodStr, out periodInt))
			{
				period = new TimeSpan(0, periodInt, 0);
				logger.LogMessage("Period set to: " + period.Minutes.ToString() + " Minutes" , LogLevel.Message);
			}
			else
			{
				period = new TimeSpan(0, 60, 0); ; //one image every hour
				this._configManager.SetSetting("ImageScraperPeriod", period.Minutes.ToString());
			}
			var queryKeywordsCSV = (this._configManager.GetSetting("ImageScraperKeywords"));
			if (queryKeywordsCSV != String.Empty)
			{
				this.queryKeywords = queryKeywordsCSV.Split(',').ToList();
				if (queryKeywords.Count == 0)
				{
					logger.LogMessage("Keyword list empty!", LogLevel.Warning);
					Active = false;
				}
			}
		}
		
		#region Command Class override
		private enum Commands {
			ImageScraper	= 1,
			Scrape			= 2,
			ScrapeKeywords	= 3
		}
		
		public override bool ExecCommand(IJID user, int CodeCmd, string Param) {
			switch((Commands)CodeCmd) {
				case Commands.ImageScraper		:
					switch (Param) {
							case "on" 	:	Active = false;
							_jabberClient.SendMessage(user, "*ImageScraper Activated*");
							//ora lo salvo
							_configManager.SetSetting("ImageScraper", "on");
							scrapeTimer = new Timer(this.scrapeImage, null, period.Milliseconds, period.Milliseconds);
							break;
							case "off"	:	Active	= false;
							scrapeTimer = new Timer(this.scrapeImage, null, Timeout.Infinite, Timeout.Infinite);
							_jabberClient.SendMessage(user, "*ImageScraper Deactivated*");
							//ora lo salvo
							_configManager.SetSetting("ImageScraper", "off");
							break;
							case "help"	:	String helpString = "*ImageScraper* pesca in automatico delle immagini da google/bing e te le posta\n";
							helpString += "*/ImageScraper help*: aiuto\n";
							_jabberClient.SendMessage(user, helpString);
							break;
							default		:	if (Active) {
								_jabberClient.SendMessage(user, "ImageScraper is _active_");
							} else {
								_jabberClient.SendMessage(user, "ImageScraper is _not active_");
							}
							break;
					}
					break;
				case Commands.Scrape:
					if (!this.doScraping())
					{
						_jabberClient.SendMessage(user, "Cannot scrape image, sorry");
					}
					break;
				case Commands.ScrapeKeywords:
					var splittedParams = splitWhilePreservingQuotedValues(Param, ' ');
					if (splittedParams.Length > 0)
					{
						if (splittedParams[0].Equals("list",StringComparison.InvariantCultureIgnoreCase))
						{
							_jabberClient.SendMessage(user, "Keywords currently in use: " + String.Join(",", queryKeywords));
						}
						else if (splittedParams[0].Equals("add",StringComparison.InvariantCultureIgnoreCase)) {
							if (splittedParams.Length == 2)
							{
								if (!queryKeywords.Exists(x=>x.Equals(splittedParams[1], StringComparison.InvariantCultureIgnoreCase)))
								{
									queryKeywords.Add(splittedParams[1]);
									_configManager.SetSetting("ImageScraperKeywords", String.Join(",", queryKeywords));
									_jabberClient.SendMessage(user, "keyword added: " + splittedParams[1]);
								}
								else
								{
									_jabberClient.SendMessage(user, "keyword already present!");
								}
							}
							else
							{
								_jabberClient.SendMessage(user, "usage add <keyword>:");
							}
						}
						else if (splittedParams[0].Equals("remove",StringComparison.InvariantCultureIgnoreCase)) {
							if (splittedParams.Length == 2)
							{
								if (queryKeywords.Exists(x=>x.Equals(splittedParams[1], StringComparison.InvariantCultureIgnoreCase)))
								{
									queryKeywords.Remove(splittedParams[1]);
									_configManager.SetSetting("ImageScraperKeywords", String.Join(",", queryKeywords));
									_jabberClient.SendMessage(user, "keyword removed: " + splittedParams[1]);
								}
								else
								{
									_jabberClient.SendMessage(user, "keyword not found!");
								}
							}
							else
							{
								_jabberClient.SendMessage(user, "usage remove <keyword>:");
							}
						}
					}
					else
					{
						_jabberClient.SendMessage(user, "usage: /scrapekeywords [add <keyword>| remove <keyword>| list]");
					}
					break;
					
			}
			return true;
		}
		#endregion
		
		#region Thread Stuff
		public override bool IsThread() {
			return true;
		}
		
		public override void StartThread() {
			scrapeTimer = new Timer(this.scrapeImage, null, period.Milliseconds, period.Milliseconds);
		}

		public override void StopThread() {
			scrapeTimer.Dispose();
		}
		#endregion Thread Stuff
		
		private void scrapeImage(object stateInfo) {
			if (Active)
			{
				this.doScraping();
			}
		}
		
		private bool doScraping()
		{
			if (queryKeywords.Count != 0)
			{
				var imgUri = this.getImageUri();
				if (imgUri != String.Empty)
				{
					foreach(IRosterItem user in _jabberClient.Roster) {
						if (user.status == UserStatus.OnLine) {
							_jabberClient.SendMessage(user.JID, Conference.botName + " propone un immagine: " + imgUri);
						}
					}
					return true;
				}
			}
			
			return false;
			
		}
		
		private string getImageUri()
		{
			//get random keyword
			var rnd = new Random();
			var rndIdx = rnd.Next(0,queryKeywords.Count);
			var keyword = queryKeywords.ElementAt(rndIdx);
			List<Uri> imgLinks;
			if (alternative)
			{
				_logger.LogMessage("Scrape Google Image With Keyword: " + keyword, LogLevel.Message);
				imgLinks = GoogleImageScraper.GetImages(keyword).ToList();
			}
			else
			{
				_logger.LogMessage("Scrape Bing Image With Keyword: " + keyword, LogLevel.Message);
				imgLinks = BingImageScraper.GetImages(keyword).ToList();
			}
			
			alternative = !alternative;
			
			if (imgLinks.Count > 0 )
			{
				
				rndIdx = rnd.Next(0, imgLinks.Count);
				var imgUri = imgLinks.ElementAt(rndIdx);
				
				return imgUri.AbsoluteUri;
			}
			else
			{
				return string.Empty;
			}
		}
		
		private string[] splitWhilePreservingQuotedValues(string value, char delimiter)
		{
			Regex csvPreservingQuotedStrings = new Regex(string.Format("(\"[^\"]*\"|[^{0}])+", delimiter));
			var values =
				csvPreservingQuotedStrings.Matches(value)
				.Cast<Match>()
				.Select(m => m.Value.Trim().Replace(@"""", string.Empty))
				.Where(v => !string.IsNullOrWhiteSpace(v));
			return values.ToArray();
		}
	}
}