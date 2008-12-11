/*
 * Created by SharpDevelop.
 * User: debe
 * Date: 20/09/2008
 * Time: 16.39
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
	class ConfBot
	{
		// we will wait on this event until we're done sending
		static ManualResetEvent done = new ManualResetEvent(false);
		static JabberClient j = new JabberClient();
		static RosterManager rm= new RosterManager();
		static PresenceManager pm = new PresenceManager();
		static Configuration config;
		static string logFile;
		static string[] admins;
		const string NOADMINMSG = "Non sei admin...niente da fare!";
		const string CONFIGFILE	= "ConfBot.config";
		//static string BotVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

		static void Main(string[] args)
		{
			ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
			configFile.ExeConfigFilename = args.Length > 0 ? args[0]: (".\\" + CONFIGFILE) ;
			config = ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None);

			//administrators
			admins = config.AppSettings.Settings["Administrators"].Value.Split(',');

			//log File
			logFile = config.AppSettings.Settings["LogFile"].Value;
			// what user/pass to log in as
			j.User = config.AppSettings.Settings["Username"].Value;
			j.Server = config.AppSettings.Settings["Server"].Value; // use gmail.com for GoogleTalk
			j.Password = config.AppSettings.Settings["Password"].Value;
			j.NetworkHost = config.AppSettings.Settings["NetworkHost"].Value;
			j.Port = Int32.Parse(config.AppSettings.Settings["Port"].Value);

			//auth settings
			
			j.AutoStartTLS = true;
			//j.KeepAlive = 5;
			j.Resource = config.AppSettings.Settings["Resource"].Value;
			//j.Priority = 24;
			
			//Proxy settings
			j.Proxy = ProxyType.Socks5;
			j.ProxyHost = config.AppSettings.Settings["ProxyHost"].Value;
			j.ProxyPort = Int32.Parse(config.AppSettings.Settings["ProxyPort"].Value);
			
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

			LogMessageToFile("******* CONNECT *******");
			// Set everything in motion
			j.Connect();

			// wait until sending a message is complete
			done.WaitOne();

			// logout cleanly
			j.Close();
			
			//aspetta tutti i metodi...
			Thread.Sleep(3);

			LogMessageToFile("******* QUIT *******");
		}

		static void j_OnMessage(object sender, jabber.protocol.client.Message msg)
		{
			if (msg.Type == jabber.protocol.client.MessageType.error || msg.Body == null)
			{
				//per ora non faccio un tubo con gli errori
			}

			#region Admin command
			else if (msg.Body.StartsWith("/quit"))
			{
				if (isAdmin(msg.From.Bare))
				{
					//è la fine
					done.Set();
				}
				else
				{
					j.Message(msg.From, NOADMINMSG);
				}
			}

			else if (msg.Body.StartsWith("/status"))
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
					config.AppSettings.Settings["StatusMessage"].Value = statusMsg;
					config.Save();
				}
				else
				{
					j.Message(msg.From, NOADMINMSG);
				}
			}
			else if (msg.Body.StartsWith("/msg"))
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
							j.Message(user, "*AutmConference:* " + broadcastMsg);
						}
					}
				}
				else
				{
					j.Message(msg.From, NOADMINMSG);
				}
			}
			#endregion

			else if (msg.Body.StartsWith("/who"))
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
			
			else if (msg.Body.StartsWith("/time"))
			{
				DateTime Date = DateTime.Now;
				String timeString = Date.ToString("HH:mm:ss");
				foreach (JID user in rm)
				{
					j.Message(user, "*AutmConference:* qui a _RadioDite_ sono le " + timeString);
				}
			}

			else if (msg.Body.StartsWith("/help"))
			{
				String helpString = "*/help*: aiuto\n";
				helpString += "*/time*: ti dice l'ora\n";
				helpString += "*/who*: ti dice chi e' online\n";
				helpString += "*/quit*: spegne il bot\n";
				helpString += "*/status*: cambia il messaggio di stato\n";
				helpString += "*/msg*: annuncia al popolo";
				j.Message(msg.From, helpString);
			}

			else
			{
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
						j.Message(user, "*" + FromUserName + ":* " + msg.Body);

						/*
						//il presence mi serve per sapere...
						jabber.protocol.client.Presence pres = pm[user];

						
						//se l'utente è online
						if (pm.IsAvailable(user))
						{
							//ok è disponibile 
							j.Message(user, "*" + FromUserName + ":* " + msg.Body);

							
							//ma magari non vuole essere disturbato dnd = Do not disturb
							if (pres.Show != "dnd")
							{
								//message in a bottle!
								j.Message(user, "*" + FromUserName + ":* " + msg.Body);
							}
							 
						}
						else
						{
							//nulla
						}
						*/
					}
				}
			}			
		}

		private static bool isAdmin(string user)
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

		static bool j_OnInvalidCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			//Sono malato , ho il certificato!
			return true;
		}
	
		
		static void j_OnWriteText(object sender, string txt)
		{
			if (txt == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("SEND: " + txt);
			LogMessageToFile("SEND: " + txt);
			
		}

		static void j_OnReadText(object sender, string txt)
		{
			if (txt == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("RECV: " + txt);
			LogMessageToFile("RECV: " + txt);
		}
		

		static void j_OnAuthenticate(object sender)
		{
			//dico che sono online!!
			j.Presence(jabber.protocol.client.PresenceType.available, config.AppSettings.Settings["StatusMessage"].Value, "available", 0);
		}

		static void j_OnError(object sender, Exception ex)
		{
			// There was an error!
			LogMessageToFile("Error: " + ex.ToString());

			// Shut down.
			done.Set();
		}

		static void LogMessageToFile(string msg)
		{
			System.IO.StreamWriter sw = System.IO.File.AppendText(logFile);
			String header = "[" + DateTime.Now.ToString() + "] ";

			try
			{
				sw.WriteLine(header + msg);
			}
			finally
			{
				sw.Close();
			}
		}
	}

}
