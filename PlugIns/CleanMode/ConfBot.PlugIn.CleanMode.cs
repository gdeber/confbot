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
using System.Threading;
using jabber;
using jabber.protocol.client;
using jabber.connection;
using ConfBot;
using ConfBot.PlugIns;

namespace ConfBot.PlugIns
{
	/// <summary>
	/// Description of CleanMode.
	/// </summary>
	[PlugInAttribute]
	public class CleanMode : PlugIn
	{

		bool cleanMode = false;
		bool autoInsultMode = true;
		string[] badDict;
		string[] goodDict;
		
		List<char> charAlphaDict = new List<char>();
		List<string> badWordDict = new List<string>();
		List<string> insultDict = new List<string>();

		Timer autoInsult;
			
		public CleanMode(Conference confObj) : base(confObj) {
			char[] charAlpha = {'Q','W','E','R','T','Y','U','I','O','P','A','S','D','F','G','H','J','K','L','Z','X','C','V','B','N','M','ì','è','é','ò','à','ù'};
			charAlphaDict.AddRange(charAlpha);
			//clean Mode
			cleanMode	= (this.confObj.confConf.AppSettings.Settings["CleanMode"].Value == "on");
			//autoinsult Mode
			autoInsultMode	= (this.confObj.confConf.AppSettings.Settings["AutoInsultMode"].Value == "on");
			//bad Dictionary
			badDict		= this.confObj.confConf.AppSettings.Settings["BadDictionary"].Value.Split(',');
			for (int ndx = 0; ndx < badDict.Length; ndx++) {
				if (badWordDict.IndexOf(badDict[ndx].ToUpper()) < 0) {
					badWordDict.Add(badDict[ndx].ToUpper());
				}
			}
			//good Dictionary
			goodDict	= this.confObj.confConf.AppSettings.Settings["GoodDictionary"].Value.Split(',');
			//insult Dictionary
			string[] insultList	= this.confObj.confConf.AppSettings.Settings["InsultDictionary"].Value.Split(',');
			for (int ndx = 0; ndx < insultList.Length; ndx++) {
				insultDict.Add(insultList[ndx]);
			}
		}

		public bool CleanText(ref string messageText) {
			try {
				Random rnd = new Random(unchecked((int)DateTime.Now.Ticks));
				int startPos = 0;
				string tmpIn = messageText.ToUpper();
				string tmpOut = "";
				while (true) {
					while (startPos < tmpIn.Length) {
						if (charAlphaDict.IndexOf(tmpIn[startPos]) < 0)
							tmpOut	+= messageText[startPos];
						else {
							break;	
						}
						startPos++;
					}
					int endPos = startPos;
					if (startPos < tmpIn.Length) {
						while (endPos < tmpIn.Length) {
							if (charAlphaDict.IndexOf(tmpIn[endPos]) < 0) {
								endPos--;
								break;
							} else {
								endPos++;
							}
						}
						if (endPos == tmpIn.Length) {
							endPos--;
						}
						if (endPos > startPos) {
							if (badWordDict.IndexOf(tmpIn.Substring(startPos, endPos - startPos + 1)) >= 0) {
								tmpOut	+= ("_" + goodDict[rnd.Next(goodDict.Length)] + "_");
							} else {
								tmpOut	+= messageText.Substring(startPos, endPos - startPos + 1);
							}
						} else {
							tmpOut	+= messageText[startPos];
						}
					} else {
						break;
					}
					startPos = endPos + 1;
				}
				messageText = tmpOut;
				return true;
			} catch(Exception ex) {
				confObj.LogMessageToFile(ex.Message);
			}
			return false;
		}
		
		public override bool msgCommand(ref Message msg, ref String newMsg, out bool command) {
			command	= false;
			newMsg = msg.Body;
			if (msg.Body.ToLower().StartsWith("/cleanmode")) {
				if (confObj.isAdmin(msg.From.Bare)) {
					//9 char + 1 space (optional)
					if (msg.Body.Length >= 10) {
						command = true;
						String tmpMsg = msg.Body.Remove(0, 10).Trim().ToLower();
						switch (tmpMsg) {
							case "on" 	:	cleanMode	= true;
											confObj.j.Message(msg.From, "*CleanMode Activated*");
											//ora lo salvo
											confObj.confConf.AppSettings.Settings["CleanMode"].Value = "on";
											confObj.confConf.Save();
											break;
							case "off"	:	cleanMode	= false;
											confObj.j.Message(msg.From, "*CleanMode Deactivated*");
											//ora lo salvo
											confObj.confConf.AppSettings.Settings["CleanMode"].Value = "off";
											confObj.confConf.Save();
											break;
							case "help"	:	String helpString = "*/cleanmode help*: aiuto\n";
											helpString += "*/cleanmode [on|off]*: attiva/disattiva il cleanmode\n";
											helpString += "*/insult [nickname]*: invia un insulto anonimo all'utente\n";
											helpString += "*/autoinsult [on|off]*: attiva/disattiva l'autoinsulto";
											confObj.j.Message(msg.From, helpString);																		
											break;
											
							default		:	if (cleanMode) {
												confObj.j.Message(msg.From, "_CleanMode is active_");
											} else {
												confObj.j.Message(msg.From, "_CleanMode is not active_");
											}
											break;
						}
					}
				} else {
					confObj.j.Message(msg.From, Conference.NOADMINMSG );
				}
				return true;
			} else if (msg.Body.ToLower().StartsWith("/autoinsult")) {
				if (confObj.isAdmin(msg.From.Bare)) {
					//11 char + 1 space (optional)
					if (msg.Body.Length >= 11) {
						command = true;
						String tmpMsg = msg.Body.Remove(0, 11).Trim().ToLower();
						switch (tmpMsg) {
							case "on" 	:	autoInsultMode	= true;
											confObj.j.Message(msg.From, "*AutoInsult Activated*");
											//ora lo salvo
											confObj.confConf.AppSettings.Settings["AutoInsultMode"].Value = "on";
											confObj.confConf.Save();
											break;
							case "off"	:	autoInsultMode	= false;
											confObj.j.Message(msg.From, "*AutoInsult Deactivated*");
											//ora lo salvo
											confObj.confConf.AppSettings.Settings["AutoInsultMode"].Value = "off";
											confObj.confConf.Save();
											break;
							default		:	if (autoInsultMode) {
												confObj.j.Message(msg.From, "_AutoInsult is active_");
											} else {
												confObj.j.Message(msg.From, "_AutoInsult is not active_");
											}
											break;
						}
					}
				}
				else {
					confObj.j.Message(msg.From, Conference.NOADMINMSG );
				}
				return true;
			} else if (msg.Body.ToLower().StartsWith("/addinsult")) {
				if (confObj.isAdmin(msg.From.Bare)) {
					command = true;
					//10 char + 1 space
					if (msg.Body.Length > 11) {
						string newInsult = msg.Body.Remove(0, 11).Trim();
						string newDict = "";
						insultDict.Add(newInsult);
						for(int iNdx = 0; iNdx < insultDict.Count; iNdx++) {
							newDict += insultDict[iNdx] + ',';
						}							
						newDict.Remove(insultDict.Count - 1);
						confObj.confConf.AppSettings.Settings["InsultDictionary"].Value = newDict;
						confObj.confConf.Save();
						confObj.j.Message(msg.From, "insult _" + newInsult + "_ addedd");
					}
					return true;
				} else {
					confObj.j.Message(msg.From, Conference.NOADMINMSG );
				}
			} else if (msg.Body.ToLower().StartsWith("/insult")) {
				command = true;
				string destName = "";
				string destMsg = "";
				//7 char + 1 space
				if (msg.Body.Length > 8) {
					destName = msg.Body.Remove(0, 8).Trim();
					destMsg	= destName.ToUpper();
					JID user;
					switch (confObj.GetUser(destMsg, out user)) {
							case UserStatus.Unknown		:	confObj.j.Message(msg.From, "User _" + destMsg + "_ not exist.");
															break;
							case UserStatus.Away		:	confObj.j.Message(msg.From, "User _" + destMsg + "_ is away.");
															break;
							case UserStatus.NotAvaiable	:	confObj.j.Message(msg.From, "User _" + destMsg + "_ is offline.");
															break;
							default						:	Random rnd = new Random(unchecked((int)DateTime.Now.Ticks));
															String insult	= insultDict[rnd.Next(insultDict.Count)];
															confObj.j.Message(user, "_" + insult + "_");
															confObj.j.Message(msg.From, Conference.botName + " send _" + insult + "_ to _" + destName + "_");
															break;
					}
				}
				return true;
			}

			if (cleanMode) {
				try {
					string msgBody	= newMsg;
					CleanText(ref msgBody);
					if (msgBody.Trim() != "") {
						newMsg	= msgBody;
						if (newMsg != msg.Body) {
							confObj.j.Message(msg.From, msgBody);
						}
					}
				}
				catch(Exception ex) {
					confObj.LogMessageToFile(ex.Message);
				}
			}
			return true;
		}
		
		public override string Help() {
			string helpString = "*/cleanmode help*: aiuto su cleanmode";
			return helpString;
		}
	
		private void SendAutoInsult(object stateInfo) {
			int newTime = (new Random(unchecked((int)DateTime.Now.Ticks))).Next(300 * 1000, 3600 * 1000);
			autoInsult.Change(newTime, newTime);
			if (autoInsultMode) {
				Random rndUser		= new Random(unchecked((int)DateTime.Now.Ticks));
				Random rndMsg		= new Random(unchecked((int)DateTime.Now.Ticks));
				List<JID> lstUsers	= new List<JID>();
				foreach(JID user in confObj.rm) {
					if (confObj.pm.IsAvailable(user)) {
						lstUsers.Add(user);
					}
				}
				if (lstUsers.Count > 0) {
					confObj.j.Message(lstUsers[rndUser.Next(lstUsers.Count)], Conference.botName + " ti manda un insulto gratuito: _" + insultDict[rndMsg.Next(insultDict.Count)] + "_");
				}
			}
		}
		
		public override bool IsThread() {
			return true;
		}
		
		public override void StartThread() {
			autoInsult =  new Timer(this.SendAutoInsult, null, 60000, 60000);
		}		

		public override void StopThread() {
			autoInsult.Dispose();
		}		

	}
}
