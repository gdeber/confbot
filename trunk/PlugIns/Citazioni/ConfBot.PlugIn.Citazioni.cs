/*
 * Creato da SharpDevelop.
 * Utente: Andre
 * Data: 18/12/2008
 * Ora: 10.20
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Net;
using System.IO;
using System.Threading;
using jabber;
using jabber.connection;
using jabber.protocol.client;
using ConfBot.PlugIns;
using ConfBot.Lib;

namespace ConfBot.PlugIns
{
	/// <summary>
	/// Description of ConfBot_PlugIn_Citazioni.
	/// </summary>
	[PlugInAttribute]
	public class Citazioni : PlugIn
	{
		 
		const string WEBRIF = "http://mercurio.citazioni.tk/leggi/?cs=1&t=1";
		const string PATTERNSTART = "innerHTML='";
		const string PATTERNEND	= "';var";
		const string QUOTESTART = "<span class=\"ctkc\">";
		const string QUOTEEND = "</span>";
		const string AUTHSTART = "class=\"ctkaut\">";
		const string AUTHEND = "</a> <span class=\"ctkd\">";
			
		bool autoQuoteMode = true;
		Timer autoQuote;

		public Citazioni(Conference confObj) : base(confObj) {
			//autoQuuote Mode
			//autoQuoteMode	= (this.confObj.confConf.AppSettings.Settings["AutoQuoteMode"].Value == "on");
		}
		
		public override bool msgCommand(ref Message msg, ref String newMsg, out bool command) {
			command	= false;
			newMsg = msg.Body;
			if (msg.Body.ToLower().StartsWith("/citazione")) {
				string author = "";
				string citazione = '\n' + "_" + getCitazione(ref author) + "_";
				if (author.Trim() == "")
					confObj.j.Message(msg.From, citazione);
				else {
					confObj.j.Message(msg.From, citazione + '\n' + "*_" + author + "_*");
				}
				command = true;
			}
			return true;
		}
		
		public override string Help() {
			string helpString = "*/citazione help*: aiuto su citazioni";
			return helpString;
		}
		
		public string getCitazione(ref string Author) {
			string	lsText = "";
			Author = "";
			try {
				HttpWebRequest	loRequest	= (HttpWebRequest) WebRequest.Create(WEBRIF);
				HttpWebResponse	loResponse;
				Stream			loStream;
				loRequest.Method = "GET";
				
				switch (confObj.j.Proxy) {
					case ProxyType.None		:	break;
					case ProxyType.HTTP		:	loRequest.Proxy = new WebProxy(confObj.j.ProxyHost, confObj.j.ProxyPort);
												break;
					case ProxyType.Socks5	:	loRequest.Proxy = new WebProxy(confObj.j.ProxyHost.ToString() + ':' + confObj.j.ProxyPort.ToString(), false, null, new System.Net.NetworkCredential(confObj.j.ProxyUsername, confObj.j.ProxyPassword));
												break;
					default					:	break;
				}
				loResponse	= (HttpWebResponse) loRequest.GetResponse();
				loStream	= loResponse.GetResponseStream();
				StreamReader loRead = new StreamReader(loStream);
				lsText	= loRead.ReadToEnd();
				if (lsText.Trim() != "") {
					int liStart = lsText.IndexOf(PATTERNSTART) + PATTERNSTART.Length; 
					int liEnd	= lsText.IndexOf(PATTERNEND, liStart);
					lsText  = lsText.Substring(liStart, lsText.Length - liStart - (lsText.Length - liEnd));
					lsText	= lsText.Replace("\\x", "%");
					lsText	= lsText.Substring(1, lsText.Length - 3);
					lsText	= Uri.UnescapeDataString(lsText);
					liStart = lsText.IndexOf(QUOTESTART) + QUOTESTART.Length; 
					liEnd	= lsText.IndexOf(QUOTEEND, liStart);
					string lsCitazione = lsText.Substring(liStart, lsText.Length - liStart - (lsText.Length - liEnd));
					liStart = lsText.IndexOf(AUTHSTART) + AUTHSTART.Length; 
					liEnd	= lsText.IndexOf(AUTHEND, liStart);
					if ((liStart > 1) && (liEnd > 1)) {
						Author = lsText.Substring(liStart, lsText.Length - liStart - (lsText.Length - liEnd));
						Author = TextLib.ReplaceHTMLCode(Author);
					}
					lsText = TextLib.ReplaceHTMLCode(lsCitazione);
				}
			}
			catch (Exception ex) {
				lsText	= ex.Message;
				//confObj.LogMessageToFile(ex.Message);
			}
			return lsText;
		}

		private void SendAutoQuote(object stateInfo) {
			int newTime = (new Random(unchecked((int)DateTime.Now.Ticks))).Next(300 * 1000, 3600 * 1000);
			autoQuote.Change(newTime, newTime);
			if (autoQuoteMode) {
				string author = "";
				string citazione = '\n' + "_" + getCitazione(ref author) + "_";
				if (author.Trim() != "") {
					citazione += '\n' + "*_" + author + "_*";
				}

				foreach(JID user in confObj.rm) {
					if (confObj.pm.IsAvailable(user)) {
						confObj.j.Message(user, Conference.botName + " propone una perla: " + citazione);
					}
				}
			}
		}

		public override bool IsThread() {
			return true;
		}
		
		public override void StartThread() {
			autoQuote =  new Timer(this.SendAutoQuote, null, 60000, 60000);
		}		

		public override void StopThread() {
			autoQuote.Dispose();
		}		

	}
}
