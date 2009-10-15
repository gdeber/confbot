 /* Utente: Andre
 * Data: 16/12/2008
 * Ora: 15.05
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using ConfBot.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using ConfBot.PlugIns;

namespace ConfBot
{
		
	
	/// <summary>
	/// Description of Conference.
	/// </summary>
	/// 
	public sealed class Conference : ConfBot.Command
	{
		// we will wait on this event until we're done sending
		public ManualResetEvent done = new ManualResetEvent(false);
		private IJabberClient _jabbberClient;
		private ILogger _logger;
		private IConfigManager confConf;
		
		public static string botName = "ConfBot";
		private string logFile;
		public const string NOADMINMSG = "Non sei admin...niente da fare!";
		public const string PLUGINDIR = "PlugIns";
		public string PluginDir;
		
		static string BotVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		static CmdMgr				cmdMgr;
		static PlugIns.PlugInMgr	plugMgr;

		//private bool terminate = false;
		
		#region Constructor
		public Conference(IConfigManager config, IJabberClient jabberClient, ILogger logger) : base()
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
			_logger = logger;
			_jabbberClient = jabberClient;
			_jabbberClient.OnMessage += new MessageHandler(_jabbberClient_OnMessage);
			_jabbberClient.OnError += new ErrorMessageEventHandler(_jabbberClient_OnError);

			//bot Name
			if (confConf.GetSetting("BotName").Trim() != "")
			{
				botName	=confConf.GetSetting("BotName");
			}
				
			//log File
			logFile = confConf.GetSetting("LogFile");
			// what user/pass to log in as

			
			//pluginDir
			PluginDir =confConf.GetSetting("PluginDir");
			if (PluginDir.Length.Equals(0))
			{
				PluginDir = PLUGINDIR;
			}

			

			//command manager
			cmdMgr	= new CmdMgr(_jabbberClient);
			cmdMgr.AddCommands(this);
			//plugin manager
			plugMgr = new PlugInMgr(_logger, this, PluginDir);
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
		
		public override bool ExecCommand(IJID user, int CodeCmd, string Param) {
			switch((Commands)CodeCmd) {
				case Commands.Quit		:	
											done.Set();
											//terminate	= true;
											break;
				case Commands.Name		:	
											if (Param != "") {
												_jabbberClient.Nickname	= Param;
												//ora lo salvo
												confConf.SetSetting("BotName", Param);
												botName = Param;
												_jabbberClient.SendMessage(user, "BotName changed to : " + botName);
											}
											break;
				case Commands.Status	:	
											_jabbberClient.StatusMessage = Param;
											//ora lo salvo
											confConf.SetSetting("StatusMessage", Param);
											break;
				case Commands.Msg		:	
											if (Param != "") {
												foreach (IRosterItem rosterItem in _jabbberClient.Roster)
												{
													_jabbberClient.SendMessage(rosterItem, "*" + botName + ":* " + Param);
												}
											}
											break;
				case Commands.Who		:	
											string OnLineMsg = "*Online:* \n";
											string OffLineMsg= "*Offline:* \n";
											foreach (IRosterItem rosterItem in _jabbberClient.Roster)
											{
												String NomeUtente = rosterItem.Nickname;
												if (NomeUtente == null)
												{
													//se non c'è il nickname uso il nome utente
													NomeUtente = rosterItem.JID.User;
												}
												switch (rosterItem.status)
												{
													case UserStatus.DoNotDisturb:
														OnLineMsg += "*"+ NomeUtente + "*" + "\n";
														break;
													case UserStatus.Away:
														OnLineMsg += "_"+ NomeUtente + "_" + "\n";
														break;
													case UserStatus.NotAvailable:
														OffLineMsg += NomeUtente + "\n";
														break;
													case UserStatus.OnLine:
														OnLineMsg += NomeUtente + "\n";
														break;														
													default:
														OnLineMsg += "?" + NomeUtente + "?" + "\n";
														break;
												}
											}
											_jabbberClient.SendMessage(user, OnLineMsg+ "\n"+ OffLineMsg);
											break;
				case Commands.Time		:	
											DateTime Date = DateTime.Now;
											String timeString = Date.ToString("HH:mm:ss");
											foreach (IRosterItem rosterItem in _jabbberClient.Roster)
											{
												_jabbberClient.SendMessage(rosterItem, "Qui da _" + botName + "_ sono le " + timeString);
											}
											break;
				case Commands.Ver		:	
											_jabbberClient.SendMessage(user, "*ConfBot:* ver. *" + BotVer + "*");
											break;
				case Commands.Help		:	
											bool isAdm = _jabbberClient.Roster[user.Bare].IsAdmin; //isAdmin(user.Bare);
											string helpMsg = Help(isAdm);
											helpMsg	+= plugMgr.Help(isAdm);											
											_jabbberClient.SendMessage(user, helpMsg);
											break;
				case Commands.Nick		:	
											if (Param != "") {
												string[] nick	= Param.Split(' ');
												if (nick.Length == 2) 
												{
													IRosterItem userObj;
													GetUser(nick[0], out userObj);
													if (userObj == null) {
														_jabbberClient.SendMessage(user, "L'utente *" + nick[0] + "* non esiste");
													} else {
														userObj.Nickname	= nick[1];
														_jabbberClient.SendMessage(user, "Nickname cambiato con successo");
													}
												}
											}
											break;
			}
			return true;
		}
		#endregion
		
		#region Public Methods
		public UserStatus GetUser(string username, out IRosterItem userObj) {
			try {
				foreach (IRosterItem rosterItem in _jabbberClient.Roster) {
					String Nick = rosterItem.Nickname;
					if (Nick == null) {
						Nick = "";
					}
					if ((username.ToLower() == Nick.ToLower()) || (username.ToLower() == rosterItem.JID.User.ToLower()) || (username.ToLower() == rosterItem.JID.Bare)) {
						userObj = rosterItem;
						return rosterItem.status;
					}
				}
			}
			catch(Exception E) {
				_logger.LogMessage(E.Message, LogLevel.Error);
			}
			userObj = null;
			return UserStatus.Unknown;
		}
		
		#endregion
		
		#region Jabber events
		void _jabbberClient_OnMessage(object sender, IMessage message)
		{
			ThreadPool.QueueUserWorkItem(this.ThrMessageProc, message);
		}
		
		void _jabbberClient_OnError(object sender, Exception ex)
		{
			_logger.LogMessage("Error!! " +ex.Message, LogLevel.Error);
			done.Set();
		}
		#endregion
		
		#region Thread Method
		public void Run() {
			
			_logger.LogMessage("******* CONNECT *******", LogLevel.Message);
			// Set everything in motion
			_jabbberClient.Connect();

			
			done.WaitOne();
//			while(!terminate) {
//				Thread.Sleep(250);
//			}

			plugMgr.Stop();
			
			// logout cleanly
			_jabbberClient.Close();
			
			//aspetta tutti i metodi...
			Thread.Sleep(3);

			_logger.LogMessage("******* QUIT *******", LogLevel.Message);
		}
		
		private void ThrMessageProc(Object stateInfo)
		{
			IMessage message = (stateInfo as IMessage);
			
			if (message.Type == MessageType.error|| message.Body == null)
			{
				_logger.LogMessage(String.Format("Message error. Body: {0} From:{1}", 
				                                 message.Body == null? "null": message.Body.ToString(), message.From.Bare)  , LogLevel.Warning);
			}
			else
			{
				if (!cmdMgr.ExecCommand(message.From, message.Body)) {
					string msgBody	= message.Body;
					foreach (IRosterItem rosterItem in _jabbberClient.Roster)
					{
						//se non sono quello che l'ha mandato
						if (!rosterItem.JID.Bare.Equals(message.From.Bare))
						{
							//ottengo il roster item che è più informativo
							//jabber.protocol.iq.Item rosterItem = rm[user];
							//tipo il nickname
							
							String FromUserName = _jabbberClient.Roster[message.From.Bare].Nickname;
							if (FromUserName == null)
							{
								//se non c'è il nickname uso il nome utente
								FromUserName = rosterItem.JID.User;
							}
							
							//Andrew dice di mandare lo stesso...
							_jabbberClient.SendMessage(rosterItem.JID, "*" + FromUserName + ":* " + msgBody);
							
						}
					}
				}
			}
		}
		
		#endregion
	}
}
