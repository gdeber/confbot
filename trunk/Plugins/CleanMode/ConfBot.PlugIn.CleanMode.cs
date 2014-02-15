/*
 * Creato da SharpDevelop.
 * Utente: Andre
 * Data: 12/12/2008
 * Ora: 19.55
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using ConfBot;
using ConfBot.PlugIns;
using ConfBot.Types;

namespace ConfBot.PlugIns
{
	struct ToBeABadBoy {
		public IJID			Boy;
		public DateTime	TimeFirst;
		public int			Count;
	}

	struct BadBoy {
		public IJID			Boy;
		public DateTime	TimeToSleep;
	}

	/// <summary>
	/// Description of CleanMode.
	/// </summary>
	[PlugInAttribute]
	public class CleanMode : PlugIn
	{
		private const int MaxBadWord			= 5;
		private const int MaxBadTime			= 10;
		private const int SleepBadBoyTime	= 5;

		private bool cleanMode			= false;
		private bool autoInsultMode	= true;
		private bool badBoysMode		= false;
		private string[] badDict;
		private string[] goodDict;
		
		private List<char>				charAlphaDict	= new List<char>();
		private List<string>				badWordDict	= new List<string>();
		private List<string>				goodWordDict	= new List<string>();
		private List<string>				insultDict		= new List<string>();
		private List<ToBeABadBoy>	badBoysToBe	= new List<ToBeABadBoy>();
		private List<BadBoy>			badBoys			= new List<BadBoy>();

		Timer autoInsult;
		
		#region Constructors
		public CleanMode(IJabberClient jabberClient,IConfigManager configManager, ILogger logger) : base(jabberClient, configManager, logger) {
			#region Command Initialization
			BotCmd tempCmd;
			// Cleanmode Command
			tempCmd.Command	= "cleanmode";
			tempCmd.Code	= Convert.ToInt32(Commands.CleanMode);
			tempCmd.Admin	= true;
			tempCmd.Help	= "[on|off] attiva/disattiva il cleanmode";
			listCmd.Add(tempCmd);
			// AutoInsult Command
			tempCmd.Command	= "autoinsult";
			tempCmd.Code	= Convert.ToInt32(Commands.AutoInsult);
			tempCmd.Admin	= true;
			tempCmd.Help	= "[on|off] attiva/disattiva l'insulto automatico";
			listCmd.Add(tempCmd);
			// AddInsult Command
			tempCmd.Command	= "addinsult";
			tempCmd.Code	= Convert.ToInt32(Commands.AddInsult);
			tempCmd.Admin	= true;
			tempCmd.Help	= "[insulto] aggiunge un insulto";
			listCmd.Add(tempCmd);
			// Insult Command
			tempCmd.Command	= "insult";
			tempCmd.Code	= Convert.ToInt32(Commands.Insult);
			tempCmd.Admin	= false;
			tempCmd.Help	= "[nickname] invia un insulto anonimo all'utente";
			listCmd.Add(tempCmd);
			// BadBoys Command
			tempCmd.Command	= "badboys";
			tempCmd.Code	= Convert.ToInt32(Commands.BadBoys);
			tempCmd.Admin	= true;
			tempCmd.Help	= "[on|off|list] attiva/disattiva la modalità BadBoys - elenca i BadBoys";
			listCmd.Add(tempCmd);
			// TimeLeft Command
			tempCmd.Command	= "timeleft";
			tempCmd.Code	= Convert.ToInt32(Commands.TimeLeft);
			tempCmd.Admin	= false;
			tempCmd.Help	= "In modalità BadBoys indica il tempo di ban rimanente";
			listCmd.Add(tempCmd);
			// badword command
			tempCmd.Command	= "badwords";
			tempCmd.Code	= Convert.ToInt32(Commands.BadWords);
			tempCmd.Admin	= true;
			tempCmd.Help	= "<add <badword>| remove <badword>| list> aggiunge, rimuove o elenca le parole sporche in uso";
			listCmd.Add(tempCmd);
			// goodwords command
			tempCmd.Command	= "goodwords";
			tempCmd.Code	= Convert.ToInt32(Commands.GoodWords);
			tempCmd.Admin	= true;
			tempCmd.Help	= "<add <goodword>| remove <goodword>| list> aggiunge, rimuove o elenca le parole buone in uso";
			listCmd.Add(tempCmd);
			#endregion

			char[] charAlpha = {'Q','W','E','R','T','Y','U','I','O','P','A','S','D','F','G','H','J','K','L','Z','X','C','V','B','N','M','ì','è','é','ò','à','ù'};
			charAlphaDict.AddRange(charAlpha);
			//clean Mode
			cleanMode	= (this._configManager.GetSetting("CleanMode") == "on");
			//autoinsult Mode
			autoInsultMode	= (this._configManager.GetSetting("AutoInsultMode") == "on");
			//badboys Mode
			badBoysMode	= (this._configManager.GetSetting("BadBoysMode") == "on");
			//bad Dictionary
			badDict		= this._configManager.GetSetting("BadDictionary").Split(',');
			foreach (var word in badDict) {
				if (!badWordDict.Exists(x=>x.Equals(word, StringComparison.InvariantCultureIgnoreCase)))
				{
					badWordDict.Add(word);
				}
			}
			//good Dictionary
			goodDict	= this._configManager.GetSetting("GoodDictionary").Split(',');
			foreach (var word in goodDict) {
				if (!goodWordDict.Exists(x=>x.Equals(word, StringComparison.InvariantCultureIgnoreCase)))
				{
					goodWordDict.Add(word);
				}
			}
			//insult Dictionary
			string[] insultList	= this._configManager.GetSetting("InsultDictionary").Split(',');
			for (int ndx = 0; ndx < insultList.Length; ndx++) {
				insultDict.Add(insultList[ndx]);
			}
		}
		#endregion

		#region Command Class override
		private enum Commands {
			CleanMode	= 1,
			AutoInsult	= 2,
			AddInsult	= 3,
			Insult		= 4,
			BadBoys		= 5,
			TimeLeft	= 6,
			BadWords	= 7,
			GoodWords	= 8
		}
		
		public override bool ExecCommand(IJID user, int CodeCmd, string Param) {
			switch((Commands)CodeCmd) {
				case Commands.CleanMode		:
					switch (Param) {
							case "on" 	:	cleanMode	= true;
							_jabberClient.SendMessage(user, "*CleanMode Activated*");
							//ora lo salvo
							_configManager.SetSetting("CleanMode", "on");
							break;
							case "off"	:	cleanMode	= false;
							_jabberClient.SendMessage(user, "*CleanMode Deactivated*");
							//ora lo salvo
							_configManager.SetSetting("CleanMode", "off");
							break;
							default		:	if (cleanMode) {
								_jabberClient.SendMessage(user, "_CleanMode is active_");
							} else {
								_jabberClient.SendMessage(user, "_CleanMode is not active_");
							}
							break;
					}
					break;
				case Commands.AutoInsult	:
					switch (Param) {
							case "on" 	:	autoInsultMode	= true;
							_jabberClient.SendMessage(user, "*AutoInsult Activated*");
							//ora lo salvo
							_configManager.SetSetting("AutoInsultMode", "on");
							break;
							case "off"	:	autoInsultMode	= false;
							_jabberClient.SendMessage(user, "*AutoInsult Deactivated*");
							//ora lo salvo
							_configManager.SetSetting("AutoInsultMode", "off");
							break;
							default		:	if (autoInsultMode) {
								_jabberClient.SendMessage(user, "_AutoInsult is active_");
							} else {
								_jabberClient.SendMessage(user, "_AutoInsult is not active_");
							}
							break;
					}
					break;
				case Commands.AddInsult		:
					if (Param.Trim() != "") {
						string newDict = "";
						insultDict.Add(Param);
						for(int iNdx = 0; iNdx < insultDict.Count; iNdx++) {
							newDict += insultDict[iNdx] + ',';
						}
						newDict.Remove(insultDict.Count - 1);
						this._configManager.SetSetting("InsultDictionary", newDict);
						_jabberClient.SendMessage(user, "insult _" + Param + "_ addedd");
					}
					break;
				case Commands.Insult		:
					if (Param.Trim() != "") {
						string destMsg = "";
						destMsg	= Param.ToUpper();
						
						IRosterItem userItem = _jabberClient.Roster[destMsg];
						if (userItem != null)
						{
							switch (userItem.status) {
									case UserStatus.Unknown		:	_jabberClient.SendMessage(user, "User _" + destMsg + "_ not exist.");
									break;
									case UserStatus.Away		:	_jabberClient.SendMessage(user, "User _" + destMsg + "_ is away.");
									break;
									case UserStatus.NotAvailable	:	_jabberClient.SendMessage(user, "User _" + destMsg + "_ is offline.");
									break;
									default						:	Random rnd = new Random(unchecked((int)DateTime.Now.Ticks));
									String insult	= insultDict[rnd.Next(insultDict.Count)];
									_jabberClient.SendMessage(userItem.JID, "_" + insult + "_");
									_jabberClient.SendMessage(user, Conference.botName + " send _" + insult + "_ to _" + Param + "_");
									break;
							}
						}
						else
						{
							_jabberClient.SendMessage(user, "User _" + destMsg + "_ not exist.");
						}
					}
					break;
				case Commands.BadBoys	:
					switch (Param) {
							case "on" 	:	badBoysMode	= true;
							_jabberClient.SendMessage(user, "*BadBoys Activated*");
							//ora lo salvo
							_configManager.SetSetting("BadBoysMode", "on");
							break;
							case "off"	:	badBoysMode	= false;
							_jabberClient.SendMessage(user, "*BadBoys Deactivated*");
							//ora lo salvo
							_configManager.SetSetting("BadBoysMode", "off");
							break;
							case "list"	:	badBoysMode	= false;
							string list	= "";
							foreach(BadBoy badBoy in badBoys) {
								list += badBoy.Boy.Bare + '\n';
							}
							if (list=="") {
								_jabberClient.SendMessage(user, "There are no BadBoys in the conference");
							} else {
								_jabberClient.SendMessage(user, "The BadBoys in conference are: \n" + list);
							}
							break;
							default		:	if (badBoysMode) {
								_jabberClient.SendMessage(user, "_BadBoys is active_");
							} else {
								_jabberClient.SendMessage(user, "_BadBoys is not active_");
							}
							break;
					}
					break;
				case Commands.TimeLeft	:
					if (!badBoysMode) {
						_jabberClient.SendMessage(user, "_BadBoys is not active_");
					} else {
						bool	found	= false;
						foreach(BadBoy boy in badBoys) {
							if (boy.Boy.Bare == user.Bare) {
								found	= true;
								if (boy.TimeToSleep < System.DateTime.Now) {
									int ndx = badBoys.IndexOf(boy);
									badBoys.RemoveAt(ndx);
									_jabberClient.SendMessage(user, "Great! The Exile is over!! For the future: be careful...");
									break;
								} else {
									_jabberClient.SendMessage(user, "You are a bad boy! Sleep until " + boy.TimeToSleep.ToString());
									break;
								}
							}
						}
						if (!found) {
							_jabberClient.SendMessage(user, "You are not a bad boy");
						}
					}
					break;
				case Commands.BadWords:
					var splittedParams = Param.Split(' ');
					if (splittedParams.Length > 0)
					{
						if (splittedParams[0].Equals("list",StringComparison.InvariantCultureIgnoreCase))
						{
							_jabberClient.SendMessage(user, "badwords currently in use: " + String.Join(",", badWordDict));
						}
						else if (splittedParams[0].Equals("add",StringComparison.InvariantCultureIgnoreCase)) {
							if (splittedParams.Length == 2)
							{
								if (!badWordDict.Exists(x=>x.Equals(splittedParams[1], StringComparison.InvariantCultureIgnoreCase)))
								{
									badWordDict.Add(splittedParams[1]);
									_configManager.SetSetting("BadDictionary", String.Join(",", badWordDict));
									_jabberClient.SendMessage(user, "badword added: " + splittedParams[1]);
								}
								else
								{
									_jabberClient.SendMessage(user, "badword already present!");
								}
							}
							else
							{
								_jabberClient.SendMessage(user, "usage add <badword>:");
							}
						}
						else if (splittedParams[0].Equals("remove",StringComparison.InvariantCultureIgnoreCase)) {
							if (splittedParams.Length == 2)
							{
								if (badWordDict.Exists(x=>x.Equals(splittedParams[1], StringComparison.InvariantCultureIgnoreCase)))
								{
									badWordDict.Remove(splittedParams[1]);
									_configManager.SetSetting("BadDictionary", String.Join(",", badWordDict));
									_jabberClient.SendMessage(user, "badword removed: " + splittedParams[1]);
								}
								else
								{
									_jabberClient.SendMessage(user, "badword not found!");
								}
							}
							else
							{
								_jabberClient.SendMessage(user, "usage remove <badword>:");
							}
						}
					}
					else
					{
						_jabberClient.SendMessage(user, "usage: /badwords [add <badword>| remove <badword>| list]");
					}
					break;
				case Commands.GoodWords:
					splittedParams = Param.Split(' ');
					if (splittedParams.Length > 0)
					{
						if (splittedParams[0].Equals("list",StringComparison.InvariantCultureIgnoreCase))
						{
							_jabberClient.SendMessage(user, "goodwords currently in use: " + String.Join(",", goodWordDict));
						}
						else if (splittedParams[0].Equals("add",StringComparison.InvariantCultureIgnoreCase)) {
							if (splittedParams.Length == 2)
							{
								if (!goodWordDict.Exists(x=>x.Equals(splittedParams[1], StringComparison.InvariantCultureIgnoreCase)))
								{
									goodWordDict.Add(splittedParams[1]);
									_configManager.SetSetting("GoodDictionary", String.Join(",", goodWordDict));
									_jabberClient.SendMessage(user, "goodword added: " + splittedParams[1]);
								}
								else
								{
									_jabberClient.SendMessage(user, "goodword already present!");
								}
							}
							else
							{
								_jabberClient.SendMessage(user, "usage add <goodword>:");
							}
						}
						else if (splittedParams[0].Equals("remove",StringComparison.InvariantCultureIgnoreCase)) {
							if (splittedParams.Length == 2)
							{
								if (goodWordDict.Exists(x=>x.Equals(splittedParams[1], StringComparison.InvariantCultureIgnoreCase)))
								{
									goodWordDict.Remove(splittedParams[1]);
									_configManager.SetSetting("GoodDictionary", String.Join(",", goodWordDict));
									_jabberClient.SendMessage(user, "goodword removed: " + splittedParams[1]);
								}
								else
								{
									_jabberClient.SendMessage(user, "goodword not found!");
								}
							}
							else
							{
								_jabberClient.SendMessage(user, "usage remove <goodword>:");
							}
						}
					}
					else
					{
						_jabberClient.SendMessage(user, "usage: /goodwords [add <goodword>| remove <goodword>| list]");
					}
					break;
			}
			return true;
		}
		#endregion

		
		private bool CleanText(ref string messageText, ref int badWords)
		{
			var bwc = new BadWordsCleaner(badWordDict, goodWordDict);
			var cleanedText = bwc.CleanText(messageText, out badWords);
			messageText = cleanedText;
			_logger.LogMessage(String.Format("Removed {0} badwords", badWords), LogLevel.Message);
			return (badWords > 0);
		}
		
		
		public override bool ElabMessage(ref IMessage msg, ref String newMsg) {
			
			bool modified = false;
			newMsg = msg.Body;
			if (cleanMode) {
				try {
					#region BadBoy?
					if (badBoysMode) {
						foreach(BadBoy boy in badBoys) {
							if (boy.Boy.Bare == msg.From.Bare) {
								if (boy.TimeToSleep < System.DateTime.Now) {
									int ndx = badBoys.IndexOf(boy);
									badBoys.RemoveAt(ndx);
									break;
								} else {
									_jabberClient.SendMessage(msg.From, "Be Quiet !!!");
									newMsg	= "";
									return true;
								}
							}
						}
					}
					#endregion
					string msgBody	= newMsg;
					int	badWords	= 0;
					CleanText(ref msgBody, ref badWords);
					#region BadBoy routine
					if (badBoysMode && (badWords > 0)) {
						bool	found = false;
						foreach(ToBeABadBoy thisItem in badBoysToBe) {
							if (thisItem.Boy.Bare == msg.From.Bare) {
								ToBeABadBoy	newItem = new ToBeABadBoy();
								newItem = thisItem;
								badBoysToBe.Remove(thisItem);
								if (newItem.TimeFirst < (System.DateTime.Now - (new TimeSpan(0, MaxBadTime, 0)))) {
									newItem.TimeFirst	= System.DateTime.Now;
									newItem.Count	= badWords;
								} else {
									newItem.Count	+= badWords;
								}
								badBoysToBe.Add(newItem);
								found = true;
								break;
							}
						}
						if (!found) {
							ToBeABadBoy	boy = new ToBeABadBoy();
							boy.Boy			= msg.From;
							boy.Count		= badWords;
							boy.TimeFirst	= System.DateTime.Now;
							badBoysToBe.Add(boy);
						}
						int ndxBad = (badBoysToBe.Count - 1);
						if (badBoysToBe[ndxBad].Count > MaxBadWord) {
							BadBoy	boy		= new BadBoy();
							boy.Boy				= badBoysToBe[ndxBad].Boy;
							boy.TimeToSleep	= DateTime.Now + (new TimeSpan(0, SleepBadBoyTime, 0));
							badBoys.Add(boy);
							badBoysToBe.RemoveAt(ndxBad);
							_jabberClient.SendMessage(msg.From, string.Format("You are a *Bad Boy*, be quiet for {0} minutes..", SleepBadBoyTime));
							msgBody	= "";
						}
					}
					#endregion
					if (msgBody.Trim() != "") {
						newMsg	= msgBody;
						if (newMsg != msg.Body) {
							_jabberClient.SendMessage(msg.From, msgBody);
							modified = true;
						}
					}
				}
				catch(Exception ex) {
					_logger.LogMessage(ex.Message, LogLevel.Error);
				}
			}
			return modified;
		}
		
		private void SendAutoInsult(object stateInfo) {
			int newTime = (new Random(unchecked((int)DateTime.Now.Ticks))).Next(300 * 1000, 3600 * 1000);
			autoInsult.Change(newTime, newTime);
			if (autoInsultMode) {
				Random rndUser		= new Random(unchecked((int)DateTime.Now.Ticks));
				Random rndMsg		= new Random(unchecked((int)DateTime.Now.Ticks));
				
				List<IJID> lstUsers	= new List<IJID>();
				foreach(IRosterItem user in _jabberClient.Roster) {
					lstUsers.Add(user.JID);
				}
				
				int userIdx = rndUser.Next(lstUsers.Count);
				
				if (_jabberClient.Roster[lstUsers[userIdx].Bare].status == UserStatus.OnLine)
				{
					_jabberClient.SendMessage(lstUsers[userIdx], Conference.botName + " ti manda un insulto gratuito: _" + insultDict[rndMsg.Next(insultDict.Count)] + "_");
				}
			}
		}
		
		#region Thread Section
		public override bool IsThread() {
			return true;
		}
		
		public override void StartThread() {
			autoInsult =  new Timer(this.SendAutoInsult, null, 60000, 60000);
		}

		public override void StopThread() {
			autoInsult.Dispose();
		}
		#endregion
		
	}
}
