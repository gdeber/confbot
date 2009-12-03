 /* Utente: Andre
 * Data: 16/12/2008
 * Ora: 15.05
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;

using ConfBot.PlugIns;
using ConfBot.Types;

namespace ConfBot
{
	
	/// <summary>
	/// Description of Conference.
	/// </summary>
	/// 
	public sealed class Conference : ConfBot.Command
	{
		// we will wait on this event until we're done sending
		private ManualResetEvent done = new ManualResetEvent(false);
		private IJabberClient _jabbberClient;
		private ILogger _logger;
		private IConfigManager confConf;
		
		public static string botName = "ConfBot";
		 
		private string logFile;
		public const string NOADMINMSG = "Non sei admin...niente da fare!";
		private const string PLUGINDIR = "PlugIns";
		private const int FLOODING_SLEEP_TIME=3000;
		private const int USERS_PER_SENDING_BLOCK = 3;
		private string PluginDir;
		
		static string BotVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		static CmdMgr				cmdMgr;
		static PlugIns.PlugInMgr	plugMgr;

		private List<string>	paBanned	= new List<string>();
		private List<string>	paNoReply	= new List<string>();
		
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
			// Ban Command
			tempCmd.Command	= "ban";
			tempCmd.Code			= Convert.ToInt32(Commands.Ban);
			tempCmd.Admin		= true;
			tempCmd.Help			= "butta fuori un utente dalla conference";
			listCmd.Add(tempCmd);
			// UnBan Command
			tempCmd.Command	= "unban";
			tempCmd.Code			= Convert.ToInt32(Commands.UnBan);
			tempCmd.Admin		= true;
			tempCmd.Help			= "riammette un utente nella conference";
			listCmd.Add(tempCmd);
			// NoReply Command
			tempCmd.Command	= "noreply";
			tempCmd.Code			= Convert.ToInt32(Commands.NoReply);
			tempCmd.Admin		= true;
			tempCmd.Help			= "impone la modalità ReadOnly ad un utente";
			listCmd.Add(tempCmd);
			// Reply Command
			tempCmd.Command	= "reply";
			tempCmd.Code			= Convert.ToInt32(Commands.Reply);
			tempCmd.Admin		= true;
			tempCmd.Help			= "toglie dalla modalità ReadOnly un utente";
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
			cmdMgr	= new CmdMgr(_jabbberClient, _logger);
			cmdMgr.AddCommands(this);
			//plugin manager
			plugMgr = new PlugInMgr(_jabbberClient, confConf, _logger, PluginDir);
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
			Nick	= 9,
			Ban	= 10,
			UnBan	= 11,
			NoReply	= 12,
			Reply	= 13
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
												this.sendToAll("*" + botName + ":* " + Param);
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
											this.sendToAll("Qui da _" + botName + "_ sono le " + timeString);
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
				case Commands.Ban		:
											if (Param != "") {
												IRosterItem userObj;
												string	lsNick	= Param;
												GetUser(lsNick, out userObj);
												if (userObj == null) {
													_jabbberClient.SendMessage(user, "L'utente *" + Param + "* non esiste");
												} else {
													if (userObj.IsAdmin)
													{
														_jabbberClient.SendMessage(user, "Ehi! questo bot non è una monarchia, non bannare un admin!");
													}
													else
													{
														if (paBanned.IndexOf(userObj.JID.User) >= 0) 
														{
															_jabbberClient.SendMessage(user, "*" + lsNick + "* risulta già bannato");
														} 
														else
														{
															paBanned.Add(userObj.JID.User);
															_jabbberClient.SendMessage(user, "*" + lsNick + "* bannato con successo");
														}
													}
												}
											}
											break;
				case Commands.UnBan		:
											if (Param != "") {
												IRosterItem userObj;
												string	lsNick	= Param;
												GetUser(lsNick, out userObj);
												if (userObj == null) {
													_jabbberClient.SendMessage(user, "L'utente *" + Param + "* non esiste");
												} else {
													if (paBanned.Remove(userObj.JID.User)) {
														_jabbberClient.SendMessage(user, "*" + lsNick + "* riabilitato con successo");
													} else {
														_jabbberClient.SendMessage(user, "*" + lsNick + "* non risulta bannato");
													}
												}
											}
											break;
				case Commands.NoReply		:
											if (Param != "") {
												IRosterItem userObj;
												string	lsNick	= Param;
												GetUser(lsNick, out userObj);
												if (userObj == null) {
													_jabbberClient.SendMessage(user, "L'utente *" + Param + "* non esiste");
												} else {
													if (userObj.IsAdmin)
													{
														_jabbberClient.SendMessage(user, "Ehi! questo bot non è una monarchia, non zittire un admin!");
													}
													else
													{
														if (paNoReply.IndexOf(userObj.JID.User) >= 0) {
															_jabbberClient.SendMessage(user, "*" + lsNick + "* risulta già in modalità ReadOnly");
														} else {
															paNoReply.Add(userObj.JID.User);
															_jabbberClient.SendMessage(user, "*" + lsNick + "* è ora in modallità ReadOnly");
														}
													}
												}
											}
											break;
				case Commands.Reply		:
											if (Param != "") {
												IRosterItem userObj;
												string	lsNick	= Param;
												GetUser(lsNick, out userObj);
												if (userObj == null) {
													_jabbberClient.SendMessage(user, "L'utente *" + Param + "* non esiste");
												} else {
													if (paNoReply.Remove(userObj.JID.User)) {
														_jabbberClient.SendMessage(user, "*" + lsNick + "* tolto dalla modalità ReadOnly");
													} else {
														_jabbberClient.SendMessage(user, "*" + lsNick + "* non risulta in modalità ReadOnly");
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
			
			if (message.Body == null)
			{
				return;
			}
			
			if (message.Type == MessageType.error)
			{
				IRosterItem utenteSorgente = _jabbberClient.Roster[message.From.Bare];
				
				if (utenteSorgente == null)
				{
					//e chi minchia è?
				}
				else
				{
				
					if ( (message.Error.Code == ErrorCode.ServiceUnavailable) &&
					    (utenteSorgente.status == UserStatus.OnLine) )
				    	{
				    		//l'utente è online ma mi risponde che è irraggiungibile?!?
				    		_logger.LogMessage(String.Format("L'utente {0} risulta irraggiungibile ma è ONLINE!!", message.From.Bare), LogLevel.Warning);
				    		_jabbberClient.SendMessage(message.From, message.Body);
				    	}
				    
				}	
				
				_logger.LogMessage(String.Format("Message error. Body: {0} From:{1}", 
				               message.Body == null? "null": message.Body.ToString(), message.From.Bare)  , LogLevel.Warning);
			}
			else
			{
				
				// se l'utente è bannato o in modalità ReadOnly
				if ((paBanned.IndexOf(message.From.User) >= 0) || (paNoReply.IndexOf(message.From.User) >= 0)) {
					_jabbberClient.SendMessage(message.From, "Sorry, you are unable to send messages.");
				} else {
					if (!cmdMgr.ExecCommand(message.From, message.Body)) {
						string msgBody;
						if (!plugMgr.ElabMessage(message, out msgBody))
						{
							msgBody	= message.Body;
						}
						
						if (msgBody.Trim() != "") {
							string FromUserName = _jabbberClient.Roster[message.From.Bare].Nickname;
							
							if (FromUserName == null)
							{
								//se non c'è il nickname uso il nome utente
								FromUserName = _jabbberClient.Roster[message.From.Bare].JID.User;
							}
							
							sendToAll("*" + FromUserName + ":* " + msgBody, _jabbberClient.Roster[message.From.Bare]);
							
	//						foreach (IRosterItem rosterItem in _jabbberClient.Roster)
	//						{
	//							//se non sono quello che l'ha mandato
	//							if (!rosterItem.JID.Bare.Equals(message.From.Bare))
	//							{
	//								// se l'utente non è bannato
	//								if (paBanned.IndexOf(rosterItem.JID.User) < 0) {
	//									
	//									//Andrew dice di mandare lo stesso...
	//									_jabbberClient.SendMessage(rosterItem.JID, "*" + FromUserName + ":* " + msgBody);
	//									//evitiamo di farci bloccare come spam
	//									Thread.Sleep(FLOODING_SLEEP_TIME);
	//								}
	//							}
	//						}
						}
					}
				}
			}
		}
		
		#endregion
		
		#region PRIVATE METHODS
		private void sendToAll(string msg, IRosterItem exceptUser)
		{
			ArrayList items = new ArrayList();
			Random rnd = new Random(DateTime.Now.Millisecond);
			//int blockCounter = 0;
			
			//creo una lista riempita in ordine casuale
			foreach (IRosterItem rosterItem in _jabbberClient.Roster)
			{
				items.Insert(rnd.Next(0,(items.Count+1)), rosterItem);
			}
			
			foreach (IRosterItem rosterItem in items)
			{
				//viva le mappe di karnaugh!
				if ((exceptUser == null) ||
				    (!rosterItem.Equals(exceptUser)))
				{
					if (paBanned.IndexOf(rosterItem.JID.User) < 0)
					{
						_jabbberClient.SendMessage(rosterItem, msg);
						/*
						blockCounter++;
						if (blockCounter >= USERS_PER_SENDING_BLOCK)
						{
							Thread.Sleep(FLOODING_SLEEP_TIME);
							blockCounter = 0;
						}
						*/
					}
				}
			}
		}
		
		private void sendToAll(string msg)
		{
			this.sendToAll(msg, null);
		}
		#endregion
	}
}
