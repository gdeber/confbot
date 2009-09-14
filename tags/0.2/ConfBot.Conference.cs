 /* Utente: Andre
 * Data: 16/12/2008
 * Ora: 15.05
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Threading;
using System.Collections.Generic;
using jabber;
using jabber.client;
using jabber.connection;
using System.IO;
using System.Net;
using System.Configuration;
using ConfBot.PlugIns;

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
	public sealed class Conference : ConfBot.Command
	{
		// we will wait on this event until we're done sending
		//public ManualResetEvent done = new ManualResetEvent(false);
		private JabberClient j = new JabberClient();
		public RosterManager rm= new RosterManager();
		public PresenceManager pm = new PresenceManager();
		private Configuration confConf;
		public static string botName = "ConfBot";
		private string logFile;
		private string[] admins;
		public const string NOADMINMSG = "Non sei admin...niente da fare!";
		public const string PLUGINDIR = "PlugIns";
		public string PluginDir;
		
		static string BotVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		static CmdMgr				cmdMgr;
		static PlugIns.PlugInMgr	plugMgr;

		private bool terminate = false;
		
		#region Constructor
		public Conference(Configuration config) : base()
		{
			#region Command Initialization
			BotCmd tempCmd;
			// Quit Command
			tempCmd.Command	= "quit";
			tempCmd.Code	= Convert.ToInt32(Commands.Quit);
			tempCmd.Admin	= true;
			tempCmd.Help	= "spegne il bot";
			listCmd.Add(tempCmd);
			// Name Command
			tempCmd.Command	= "name";
			tempCmd.Code	= Convert.ToInt32(Commands.Name);
			tempCmd.Admin	= true;
			tempCmd.Help	= "cambia il nome al bot";
			listCmd.Add(tempCmd);
			// Status Command
			tempCmd.Command	= "status";
			tempCmd.Code	= Convert.ToInt32(Commands.Status);
			tempCmd.Admin	= true;
			tempCmd.Help	= "cambia il messaggio di stato";
			listCmd.Add(tempCmd);
			// Msg Command
			tempCmd.Command	= "msg";
			tempCmd.Code	= Convert.ToInt32(Commands.Msg);
			tempCmd.Admin	= true;
			tempCmd.Help	= "annuncia al popolo";
			listCmd.Add(tempCmd);
			// Who Command
			tempCmd.Command	= "who";
			tempCmd.Code	= Convert.ToInt32(Commands.Who);
			tempCmd.Admin	= false;
			tempCmd.Help	= "ti dice chi e' online";
			listCmd.Add(tempCmd);
			// Time Command
			tempCmd.Command	= "time";
			tempCmd.Code	= Convert.ToInt32(Commands.Time);
			tempCmd.Admin	= false;
			tempCmd.Help	= "ti dice l'ora";
			listCmd.Add(tempCmd);
			// Ver Command
			tempCmd.Command	= "ver";
			tempCmd.Code	= Convert.ToInt32(Commands.Ver);
			tempCmd.Admin	= false;
			tempCmd.Help	= "versione del bot";
			listCmd.Add(tempCmd);
			// Help Command
			tempCmd.Command	= "help";
			tempCmd.Code	= Convert.ToInt32(Commands.Help);
			tempCmd.Admin	= true;
			tempCmd.Help	= "aiuto";
			listCmd.Add(tempCmd);
			// Help Command
			tempCmd.Command	= "nick";
			tempCmd.Code	= Convert.ToInt32(Commands.Nick);
			tempCmd.Admin	= true;
			tempCmd.Help	= "cambia il nickname di un utente";
			listCmd.Add(tempCmd);
			#endregion
			
			confConf	= config;
			
			//administrators
			admins = GetSetting("Administrators").Split(',');

			//bot Name
			if (GetSetting("BotName").Trim() != "")
			{
				botName	=GetSetting("BotName");
			}
				
			//log File
			logFile = GetSetting("LogFile");
			// what user/pass to log in as
			j.User = GetSetting("Username");
			j.Server = GetSetting("Server"); // use gmail.com for GoogleTalk
			j.Password = GetSetting("Password");
			j.NetworkHost = GetSetting("NetworkHost");
			j.Port = Int32.Parse(GetSetting("Port"));
			
			//pluginDir
			PluginDir = GetSetting("PluginDir");
			if (PluginDir.Length.Equals(0))
			{
				PluginDir = PLUGINDIR;
			}

			
			//auth settings
			
			j.AutoStartTLS = true;
			//j.KeepAlive = 5;
			j.Resource = GetSetting("Resource");
			//j.Priority = 24;
			
			//Proxy settings
			if (GetSetting("ProxyHost").Trim() == "")
				j.Proxy	= ProxyType.None;
			else
			{
				j.Proxy	= ProxyType.Socks5;
				j.ProxyHost = GetSetting("ProxyHost");
				j.ProxyPort = Int32.Parse(GetSetting("ProxyPort"));
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
		
			//command manager
			cmdMgr	= new CmdMgr(this);
			cmdMgr.AddCommands(this);
			//plugin manager
			plugMgr = new PlugInMgr(this, PluginDir);
			foreach(PlugIn pi in plugMgr.PlugInsList) {
				cmdMgr.AddCommands(pi);
			}
		}
		#endregion

		#region Command Class override
		private enum Commands {
			Quit	= 1,
			Name	= 2,
			Status	= 3,
			Msg		= 4,
			Who		= 5,
			Time	= 6,
			Ver		= 7,
			Help	= 8,
			Nick	= 9
		}
		
		public override bool ExecCommand(JID user, int CodeCmd, string Param) {
			switch((Commands)CodeCmd) {
				case Commands.Quit		:	
											terminate	= true;
											break;
				case Commands.Name		:	
											if (Param != "") {
												j.JID.User	= Param;
												//ora lo salvo
												SetSetting("BotName", Param);
												botName = Param;
												SendMessage(user, "BotName changed to : " + botName);
											}
											break;
				case Commands.Status	:	
											j.Presence(jabber.protocol.client.PresenceType.available, Param, "available", 0);
											//ora lo salvo
											SetSetting("StatusMessage", Param);
											break;
				case Commands.Msg		:	
											if (Param != "") {
												foreach (JID userObj in rm)
												{
													SendMessage(userObj, "*" + botName + ":* " + Param);
												}
											}
											break;
				case Commands.Who		:	
											String WhoMsg = "";
											foreach (JID userObj in rm)
											{
												if (pm.IsAvailable(userObj))
												{
													jabber.protocol.client.Presence pres = pm[userObj];
													// prendo il nome giusto
													String NomeUtente = rm[userObj.Bare].Nickname;
													if (NomeUtente == null)
													{
														//se non c'è il nickname uso il nome utente
														NomeUtente = userObj.Bare;
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
											SendMessage(user, WhoMsg);
											break;
				case Commands.Time		:	
											DateTime Date = DateTime.Now;
											String timeString = Date.ToString("HH:mm:ss");
											foreach (JID userObj in rm)
											{
												SendMessage(userObj, "Qui da _" + botName + "_ sono le " + timeString);
											}
											break;
				case Commands.Ver		:	
											SendMessage(user, "*ConfBot:* ver. *" + BotVer + "*");
											break;
				case Commands.Help		:	
											bool isAdm = isAdmin(user.Bare);
											string helpMsg = Help(isAdm);
											helpMsg	+= plugMgr.Help(isAdm);											
											SendMessage(user, helpMsg);
											break;
				case Commands.Nick		:	
											if (Param != "") {
												string[] nick	= Param.Split(' ');
												if (nick.Length == 2) {
													JID userObj;
													GetUser(nick[0], out userObj);
													if (userObj == null) {
														SendMessage(user, "L'utente *" + nick[0] + "* non esiste");
													} else {
														rm[userObj.Bare].Nickname	= nick[1];
														SendMessage(user, "Nickname cambiato con successo");
													}
												}
											}
											break;
			}
			return true;
		}
		#endregion
		
		#region OnMessage Event
		private void j_OnMessage(object sender, jabber.protocol.client.Message msg)
		{
			if (msg.Type == jabber.protocol.client.MessageType.error || msg.Body == null)
			{
				//per ora non faccio un tubo con gli errori
			} else {
				if (!cmdMgr.ExecCommand(msg.From, msg.Body)) {
					string msgBody	= msg.Body;
					/*if (!plugMgr.msgCommand(ref msg, out msgBody))
					{
						if (msg.Body.ToLower().StartsWith("/")) {
							SendMessage(msg.From, "unknown command");
						} else {*/
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
									SendMessage(user, "*" + FromUserName + ":* " + msgBody);
			
								}
							}
						//}
					//}			
				}
			}
//			#region Admin commands
//			else if (msg.Body.ToLower().StartsWith("/quit"))
//			{
//				if (isAdmin(msg.From.Bare))
//				{
//					//è la fine
//					//done.Set();
//					terminate	= true;
//				}
//				else
//				{
//					SendMessage(msg.From, NOADMINMSG);
//				}
//			}
//			else if (msg.Body.ToLower().StartsWith("/name"))
//			{
//				if (isAdmin(msg.From.Bare))
//				{
//					//imposta il nome del bot
//					//la lunghezza di "/name" è 5 + 1 spazio
//					String nameMsg = "";
//					if (msg.Body.Length > 6)
//					{
//						nameMsg = msg.Body.Remove(0, 6).Trim();
//						j.JID.User	= nameMsg;
//	
//						//ora lo salvo
//						confConf.AppSettings.Settings["BotName"].Value = j.JID.User;
//						confConf.Save();
//						botName = nameMsg;
//						SendMessage(msg.From, "BotName changed to : " + botName);
//					}
//					else
//					{
//						nameMsg = "";
//					}
//				}
//				else
//				{
//					SendMessage(msg.From, NOADMINMSG);
//				}
//			}
//			else if (msg.Body.ToLower().StartsWith("/status"))
//			{
//				if (isAdmin(msg.From.Bare))
//				{
//					//imposta la frase di stato del bot
//					//la lunghezza di "/status" è 7 + 1 spazio
//					String statusMsg = "";
//					if (msg.Body.Length > 7)
//					{
//						statusMsg = msg.Body.Remove(0, 7);
//						statusMsg = statusMsg.Trim();
//					}
//					else
//					{
//						statusMsg = "";
//					}
//					j.Presence(jabber.protocol.client.PresenceType.available, statusMsg, "available", 0);
//
//					//ora lo salvo
//					confConf.AppSettings.Settings["StatusMessage"].Value = statusMsg;
//					confConf.Save();
//				}
//				else
//				{
//					SendMessage(msg.From, NOADMINMSG);
//				}
//			}
//			else if (msg.Body.ToLower().StartsWith("/msg"))
//			{
//				if (isAdmin(msg.From.Bare))
//				{
//					//4 char + 1 space
//					if (msg.Body.Length > 4)
//					{
//						String broadcastMsg = msg.Body.Remove(0, 4);
//						broadcastMsg = broadcastMsg.Trim();
//						foreach (JID user in rm)
//						{
//							SendMessage(user, "*" + botName + ":* " + broadcastMsg);
//						}
//					}
//				}
//				else
//				{
//					SendMessage(msg.From, NOADMINMSG);
//				}
//			}
//			#endregion
//			#region Other Commands
//			else if (msg.Body.ToLower().StartsWith("/who"))
//			{
//				String WhoMsg = "";
//				foreach (JID user in rm)
//				{
//					if (pm.IsAvailable(user))
//					{
//						jabber.protocol.client.Presence pres = pm[user];
//						// prendo il nome giusto
//						String NomeUtente = rm[user.Bare].Nickname;
//						if (NomeUtente == null)
//						{
//							//se non c'è il nickname uso il nome utente
//							NomeUtente = user.Bare;
//						}
//						switch (pres.Show)
//						{
//							case "dnd":
//								WhoMsg = WhoMsg + "*"+ NomeUtente + "*" + "\n";
//								break;
//							case "away":
//								WhoMsg = WhoMsg + "_"+ NomeUtente + "_" + "\n";
//								break;
//							default:
//								WhoMsg = WhoMsg + NomeUtente + "\n";
//								break;
//						}
//					}
//				}
//				SendMessage(msg.From, WhoMsg);
//			}
//			else if (msg.Body.ToLower().Trim() == "/time")
//			{
//				DateTime Date = DateTime.Now;
//				String timeString = Date.ToString("HH:mm:ss");
//				foreach (JID user in rm)
//				{
//					SendMessage(user, "Qui da _" + botName + "_ sono le " + timeString);
//				}
//			}
//			else if (msg.Body.ToLower().Trim() == "/ver")
//			{
//				SendMessage(msg.From, "*ConfBot:* ver. *" + BotVer + "*");
//			}
//			else if (msg.Body.ToLower().Trim() == "/help")
//			{
//				String helpString = "*/help*: aiuto\n";
//				helpString += "*/time*: ti dice l'ora\n";
//				helpString += "*/who*: ti dice chi e' online\n";
//				helpString += "*/ver*: versione del bot";
//				if (isAdmin(msg.From.Bare))
//				{
//					helpString += "\n";
//					helpString += "*/quit*: spegne il bot\n";
//					helpString += "*/status*: cambia il messaggio di stato\n";
//					helpString += "*/msg*: annuncia al popolo\n";
//					helpString += "*/name*: cambia il nome al bot";
//				}
//				string helpPlugIns = plugMgr.Help();
//				if (helpPlugIns.Trim() != "") {
//					helpString += ('\n' + helpPlugIns);
//				}
//				SendMessage(msg.From, helpString);
//			}
//			#endregion
//			else
//			{
//				string msgBody	= msg.Body;
//				if (!plugMgr.msgCommand(ref msg, out msgBody))
//				{
//					if (msg.Body.ToLower().StartsWith("/")) {
//						SendMessage(msg.From, "unknown command");
//					} else {
//						foreach (JID user in rm)
//						{
//							//se non sono quello che l'ha mandato
//							if (user.ToString() != msg.From.Bare)
//							{
//								//ottengo il roster item che è più informativo
//								//jabber.protocol.iq.Item rosterItem = rm[user];
//								//tipo il nickname
//								String FromUserName = rm[msg.From.Bare].Nickname;
//								if (FromUserName == null)
//								{
//									//se non c'è il nickname uso il nome utente
//									FromUserName = msg.From.User;
//								}
//		
//								//Andrew dice di mandare lo stesso...
//								SendMessage(user, "*" + FromUserName + ":* " + msgBody);
//		
//							}
//						}
//					}
//				}			
//			}
		}
		#endregion
		
		#region Public Methods
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
		
		#region SendMessage
		public bool SendMessage(JID userObj, string mex) {
			this.j.Message(userObj, mex);
			return true;
		}

		public bool SendMessage(string username, string mex) {
			JID userObj;
			GetUser(username, out userObj);
			return SendMessage(userObj, mex);
		}
		#endregion

		public WebProxy GetProxy() {
			switch (j.Proxy) {
				case ProxyType.None		:	return null;
				case ProxyType.HTTP		:	return new WebProxy(j.ProxyHost, j.ProxyPort);
				case ProxyType.Socks5	:	return new WebProxy(j.ProxyHost.ToString() + ':' + j.ProxyPort.ToString(), false, null, new NetworkCredential(j.ProxyUsername, j.ProxyPassword));
				default					:	return null;
			}
		}
		
		#region Settings Methods
		public string GetSetting(string setting) {
			try {
				return confConf.AppSettings.Settings[setting].Value;				
			}
			catch (Exception E) {
				LogMessageToFile("GetSetting " + E.Message);
			}
			return "";
		}

		public bool SetSetting(string setting, string strValue) {
			try {
				confConf.AppSettings.Settings[setting].Value = strValue;
				confConf.Save();
				return true;
			}
			catch (Exception E) {
				LogMessageToFile("SetSetting " + E.Message);
			}
			return false;
		}
		#endregion
		
		#region Jabber events
		private bool j_OnInvalidCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			//Sono malato , ho il certificato!
			return true;
		}
	
		
		private void j_OnWriteText(object sender, string txt)
		{
			if (txt == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("SEND: " + txt);
			LogMessageToFile("SEND: " + txt);
			
		}

		private void j_OnReadText(object sender, string txt)
		{
			if (txt == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("RECV: " + txt);
			LogMessageToFile("RECV: " + txt);
		}
		

		private void j_OnAuthenticate(object sender)
		{
			//dico che sono online!!
			j.Presence(jabber.protocol.client.PresenceType.available, confConf.AppSettings.Settings["StatusMessage"].Value, "available", 0);
		}

		private void j_OnError(object sender, Exception ex)
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
		#endregion
		
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
