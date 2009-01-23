 /* Utente: Andre
 * Data: 16/12/2008
 * Ora: 15.05
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Threading;
using jabber;
using jabber.client;
using jabber.connection;
using System.IO;
using System.Configuration;

namespace ConfBot
{
	public enum UserStatus {
		Unknown = 0,
		NotAvaiable = 1,
		Away = 2,
		DoNotDisturb = 3,
		OnLine = 4		
	}
	
	/// <summary>
	/// Description of Conference.
	/// </summary>
	/// 
	public sealed class Conference
	{
		// we will wait on this event until we're done sending
		//public ManualResetEvent done = new ManualResetEvent(false);
		public JabberClient j = new JabberClient();
		public RosterManager rm= new RosterManager();
		public PresenceManager pm = new PresenceManager();
		public Configuration confConf;
		public static string botName = "ConfBot";
		string logFile;
		string[] admins;
		public const string NOADMINMSG = "Non sei admin...niente da fare!";
		public const string PLUGINDIR = ".\\PlugIns";
		static string BotVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		static PlugIns.PlugInMgr plugMgr;

		private bool terminate = false;
		
		#region Constructor
		public Conference(Configuration config)
		{
			confConf	= config;
			
			//administrators
			admins = confConf.AppSettings.Settings["Administrators"].Value.Split(',');

			//bot Name
			if (confConf.AppSettings.Settings["BotName"].Value.Trim() != "")
			{
				botName	= confConf.AppSettings.Settings["BotName"].Value;
			}
				
			//log File
			logFile = confConf.AppSettings.Settings["LogFile"].Value;
			// what user/pass to log in as
			j.User = confConf.AppSettings.Settings["Username"].Value;
			j.Server = confConf.AppSettings.Settings["Server"].Value; // use gmail.com for GoogleTalk
			j.Password = confConf.AppSettings.Settings["Password"].Value;
			j.NetworkHost = confConf.AppSettings.Settings["NetworkHost"].Value;
			j.Port = Int32.Parse(confConf.AppSettings.Settings["Port"].Value);

			//plugin manager
			
			plugMgr = new PlugIns.PlugInMgr(this, PLUGINDIR);
			
			//auth settings
			
			j.AutoStartTLS = true;
			//j.KeepAlive = 5;
			j.Resource = confConf.AppSettings.Settings["Resource"].Value;
			//j.Priority = 24;
			
			//Proxy settings
			if (confConf.AppSettings.Settings["ProxyHost"].Value.Trim() == "")
				j.Proxy	= ProxyType.None;
			else
			{
				j.Proxy	= ProxyType.Socks5;
				j.ProxyHost = confConf.AppSettings.Settings["ProxyHost"].Value;
				j.ProxyPort = Int32.Parse(confConf.AppSettings.Settings["ProxyPort"].Value);
			}
			
			// don't do extra stuff, please.
			j.AutoPresence = false;
			j.AutoRoster = true;
			j.AutoReconnect = 0;
			
			rm.Stream = j;
			pm.Stream = j;
			

			// listen for errors.  Always do this!
			j.OnError += new bedrock.ExceptionHandler(j_OnError);

			// what to do when login completes
			j.OnAuthenticate += new bedrock.ObjectHandler(j_OnAuthenticate);

			//listen for message
			j.OnMessage += new MessageHandler(j_OnMessage);

			// listen for XMPP wire protocol
			j.OnReadText += new bedrock.TextHandler(j_OnReadText);
			j.OnWriteText += new bedrock.TextHandler(j_OnWriteText);
			

			//Per non farsi rompere i maroni col certificato!!
			j.OnInvalidCertificate += new System.Net.Security.RemoteCertificateValidationCallback(j_OnInvalidCertificate);
		}
		#endregion
		
		void j_OnMessage(object sender, jabber.protocol.client.Message msg)
		{
			if (msg.Type == jabber.protocol.client.MessageType.error || msg.Body == null)
			{
				//per ora non faccio un tubo con gli errori
			}

			#region Admin commands
			else if (msg.Body.ToLower().StartsWith("/quit"))
			{
				if (isAdmin(msg.From.Bare))
				{
					//è la fine
					//done.Set();
					terminate	= true;
				}
				else
				{
					j.Message(msg.From, NOADMINMSG);
				}
			}
			else if (msg.Body.ToLower().StartsWith("/name"))
			{
				if (isAdmin(msg.From.Bare))
				{
					//imposta il nome del bot
					//la lunghezza di "/name" è 5 + 1 spazio
					String nameMsg = "";
					if (msg.Body.Length > 6)
					{
						nameMsg = msg.Body.Remove(0, 6).Trim();
						j.JID.User	= nameMsg;
	
						//ora lo salvo
						confConf.AppSettings.Settings["BotName"].Value = j.JID.User;
						confConf.Save();
						botName = nameMsg;
					}
					else
					{
						nameMsg = "";
					}
				}
				else
				{
					j.Message(msg.From, NOADMINMSG);
				}
			}
			else if (msg.Body.ToLower().StartsWith("/status"))
			{
				if (isAdmin(msg.From.Bare))
				{
					//imposta la frase di stato del bot
					//la lunghezza di "/status" è 7 + 1 spazio
					String statusMsg = "";
					if (msg.Body.Length > 7)
					{
						statusMsg = msg.Body.Remove(0, 7);
						statusMsg = statusMsg.Trim();
					}
					else
					{
						statusMsg = "";
					}
					j.Presence(jabber.protocol.client.PresenceType.available, statusMsg, "available", 0);

					//ora lo salvo
					confConf.AppSettings.Settings["StatusMessage"].Value = statusMsg;
					confConf.Save();
				}
				else
				{
					j.Message(msg.From, NOADMINMSG);
				}
			}
			else if (msg.Body.ToLower().StartsWith("/msg"))
			{
				if (isAdmin(msg.From.Bare))
				{
					//4 char + 1 space
					if (msg.Body.Length > 4)
					{
						String broadcastMsg = msg.Body.Remove(0, 4);
						broadcastMsg = broadcastMsg.Trim();
						foreach (JID user in rm)
						{
							j.Message(user, "*" + botName + ":* " + broadcastMsg);
						}
					}
				}
				else
				{
					j.Message(msg.From, NOADMINMSG);
				}
			}
			#endregion
			#region Other Commands
			else if (msg.Body.ToLower().StartsWith("/who"))
			{
				String WhoMsg = "";
				foreach (JID user in rm)
				{
					if (pm.IsAvailable(user))
					{
						jabber.protocol.client.Presence pres = pm[user];
						// prendo il nome giusto
						String NomeUtente = rm[user.Bare].Nickname;
						if (NomeUtente == null)
						{
							//se non c'è il nickname uso il nome utente
							NomeUtente = user.Bare;
						}
						switch (pres.Show)
						{
							case "dnd":
								WhoMsg = WhoMsg + "*"+ NomeUtente + "*" + "\n";
								break;
							case "away":
								WhoMsg = WhoMsg + "_"+ NomeUtente + "_" + "\n";
								break;
							default:
								WhoMsg = WhoMsg + NomeUtente + "\n";
								break;
						}
					}
				}
				j.Message(msg.From, WhoMsg);
			}
			else if (msg.Body.ToLower().Trim() == "/time")
			{
				DateTime Date = DateTime.Now;
				String timeString = Date.ToString("HH:mm:ss");
				foreach (JID user in rm)
				{
					j.Message(user, "*ConfBot:* qui a _" + botName + "_ sono le " + timeString);
				}
			}
			else if (msg.Body.ToLower().Trim() == "/ver")
			{
				j.Message(msg.From, "*ConfBot:* ver. *" + BotVer + "*");
			}
			else if (msg.Body.ToLower().Trim() == "/help")
			{
				String helpString = "*/help*: aiuto\n";
				helpString += "*/time*: ti dice l'ora\n";
				helpString += "*/who*: ti dice chi e' online\n";
				helpString += "*/quit*: spegne il bot\n";
				helpString += "*/status*: cambia il messaggio di stato\n";
				helpString += "*/msg*: annuncia al popolo\n";
				helpString += "*/ver*: versione del bot";
				string helpPlugIns = plugMgr.Help();
				if (helpPlugIns.Trim() != "") {
					helpString += ('\n' + helpPlugIns);
				}
				j.Message(msg.From, helpString);
			}
			#endregion
			else
			{
				string msgBody	= msg.Body;
				if (!plugMgr.msgCommand(ref msg, out msgBody))
				{
					if (msg.Body.ToLower().StartsWith("/")) {
						j.Message(msg.From, "unknown command");
					} else {
						foreach (JID user in rm)
						{
							//se non sono quello che l'ha mandato
							if (user.ToString() != msg.From.Bare)
							{
								//ottengo il roster item che è più informativo
								//jabber.protocol.iq.Item rosterItem = rm[user];
								//tipo il nickname
								String FromUserName = rm[msg.From.Bare].Nickname;
								if (FromUserName == null)
								{
									//se non c'è il nickname uso il nome utente
									FromUserName = msg.From.User;
								}
		
								//Andrew dice di mandare lo stesso...
								j.Message(user, "*" + FromUserName + ":* " + msgBody);
		
							}
						}
					}
				}			
			}
		}

		public bool isAdmin(string user)
		{
			foreach (string admin in admins)
			{
				if (user.Equals(admin))
				{
					return true;
				}
			}
			return false;
		}

		public UserStatus GetUser(string username, out JID userObj) {
			try {
				foreach (JID user in rm) {
					String Nick = rm[user.Bare].Nickname;
					if (Nick == null) {
						Nick = "";
					}
					if ((username.ToLower() == Nick.ToLower()) || (username.ToLower() == user.User.ToLower()) || (username.ToLower() == user.Bare)) {
						userObj = user;
						if (!pm.IsAvailable(user)) {
							return UserStatus.NotAvaiable;
						} else {
							switch (pm[user].Show) {
								case "dnd":
									return UserStatus.DoNotDisturb;
								case "away":
									return UserStatus.Away;
								default:
									return UserStatus.OnLine;
							}
						}
					}
				}
			}
			catch(Exception E) {
				LogMessageToFile(E.Message);
			}
			userObj = null;
			return UserStatus.Unknown;
		}
		
		#region Jabber events
		bool j_OnInvalidCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			//Sono malato , ho il certificato!
			return true;
		}
	
		
		void j_OnWriteText(object sender, string txt)
		{
			if (txt == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("SEND: " + txt);
			LogMessageToFile("SEND: " + txt);
			
		}

		void j_OnReadText(object sender, string txt)
		{
			if (txt == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("RECV: " + txt);
			LogMessageToFile("RECV: " + txt);
		}
		

		void j_OnAuthenticate(object sender)
		{
			//dico che sono online!!
			j.Presence(jabber.protocol.client.PresenceType.available, confConf.AppSettings.Settings["StatusMessage"].Value, "available", 0);
		}

		void j_OnError(object sender, Exception ex)
		{
			// There was an error!
			LogMessageToFile("Error: " + ex.ToString());

			// Shut down.
			//done.Set();
			terminate	= true;
		}
		#endregion
		
		public void LogMessageToFile(string msg)
		{
			try
			{
				System.IO.StreamWriter sw = System.IO.File.AppendText(logFile);
				String header = "[" + DateTime.Now.ToString() + "] ";
				sw.WriteLine(header + msg);
				sw.Close();
			}
			finally
			{
				
			}
		}
		
		#region Thread Method
		public void Run() {
			
			LogMessageToFile("******* CONNECT *******");
			// Set everything in motion
			j.Connect();

			// wait until sending a message is complete
			//done.WaitOne();
			while(!terminate) {
				Thread.Sleep(250);
			}

			plugMgr.Stop();
			
			// logout cleanly
			j.Close();
			
			//aspetta tutti i metodi...
			Thread.Sleep(3);

			LogMessageToFile("******* QUIT *******");
		}
		#endregion
	}
}
