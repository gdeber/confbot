using System;
using System.Collections.Generic;
using System.Text;
using jabber;
using jabber.protocol.client;
using jabber.connection;
using ConfBot;
using ConfBot.PlugIns;

namespace ConfBot.PlugIns
{
	[PlugInAttribute]
	public class ButterflyEffect: PlugIn
	{
		private string[] vocali = new string[5] {"a", "e", "i","o","u"};
		private string[] vofocafalifi = new string[5]{"afa","efe","ifi","ofo","ufu"};
		private bool Active = false;
		
		private string farfallizza(ref string msg)
		{
			string farfalledMsg = msg;
			for (int idx=0; idx < vocali.Length; idx++)
			{
				farfalledMsg = farfalledMsg.Replace(vocali[idx],vofocafalifi[idx]);
			}
			
			return farfalledMsg;
		}

		public ButterflyEffect(Conference confObj) : base(confObj){
			#region Command Initialization
			BotCmd tempCmd;
			// Butterfly Command
			tempCmd.Command	= "butterfly";
			tempCmd.Code	= Convert.ToInt32(Commands.Butterfly);
			tempCmd.Admin	= true;
			tempCmd.Help	= "[on|off] attiva/disattiva il Butterfly Effect";
			listCmd.Add(tempCmd);
			#endregion
			Active	= (this.confObj.GetSetting("Butterfly") == "on");
		}

		#region Command Class override
		private enum Commands {
			Butterfly	= 1		
		}
		
		public override bool ExecCommand(JID user, int CodeCmd, string Param) {
			switch((Commands)CodeCmd) {
				case Commands.Butterfly		: 
												switch (Param) {
													case "on" 	:	Active	= true;
																	confObj.SendMessage(user, "*Butterfly Activated*");
																	//ora lo salvo
																	confObj.SetSetting("Butterfly", "on");
																	break;
													case "off"	:	Active	= false;
																	confObj.SendMessage(user, "*Butterfly Deactivated*");
																	//ora lo salvo
																	confObj.SetSetting("Butterfly", "off");
																	break;
													case "help"	:	String helpString = "*butterfly* riscrive il tuo messaggio in alfabeto farfallino\n";
																	helpString += "*/Butterfly help*: aiuto\n";
																	confObj.SendMessage(user, helpString);
																	break;
													default		:	if (Active) {
																		confObj.SendMessage(user, "Butterfly is active_");
																	} else {
																		confObj.SendMessage(user, "Butterfly is not active_");
																	}
																	break;
												}
												break;
			}
			return true;
		}
		#endregion

		public override bool msgCommand(ref Message msg, ref string newMsg, out bool command)
		{
			command	= false;
			newMsg = msg.Body;

			if (Active) {
				try {
					string msgBody	= newMsg;
					farfallizza(ref msgBody);
					if (msgBody.Trim() != "") {
						newMsg	= msgBody;
						if (newMsg != msg.Body) {
							confObj.SendMessage(msg.From, msgBody);
						}
					}
				}
				catch(Exception ex) {
					confObj.LogMessageToFile(ex.Message);
				}
			}
			return true;

//			command = false;
//			try {
//				
//				//potrebbe essere un comando
//				if (msg.Body.ToLower().StartsWith("/butterfly"))
//				{
//					command = true;
//					if (confObj.isAdmin(msg.From.Bare))
//					{
//						//se più lungo significa che c'è il parametro del comando
//						if (msg.Body.Length >= 10)
//						{
//							String tmpMsg = msg.Body.Remove(0, 10).Trim().ToLower();
//							switch (tmpMsg) {
//									case "on": 	Active = true;
//												confObj.SendMessage(msg.From, "*Butterfly Activated*");
//												break;
//
//									case "off":	Active = false;
//												confObj.SendMessage(msg.From, "*Butterfly DeActivated*");
//												break;
//									
//									case "help":String helpString = "*butterfly* riscrive il tuo messaggio in alfabeto farfallino\n";
//												helpString += "*/Butterfly help*: aiuto\n";
//												confObj.SendMessage(msg.From, helpString);
//												break;
//									
//									default:	if (Active) {
//													confObj.SendMessage(msg.From, "_Butterfly is active_");
//												} else {
//													confObj.SendMessage(msg.From, "_Butterfly is not active_");
//												}
//												break;
//							}
//						}
//						else{
//							confObj.SendMessage(msg.From, "Che dice?!");
//						}
//					}
//					else
//					{
//						confObj.SendMessage(msg.From, Conference.NOADMINMSG);
//					}
//					
//				}
//				else
//				{
//					//non è un comando quindi devo crittografare il msg se il plugin è attivo
//					if (Active)
//					{
//						newMsg= msg.Body;
//						newMsg = farfallizza(newMsg);
//					}
//				}
//				
//				return true;
//				
//			} catch (Exception e) {
//				confObj.LogMessageToFile(e.Message);
//				return false;
//			}
			
		}

//		public string Help()
//		{
//			return "*/butterfly help*: aiuto su butterfly";
//		}
	}
}
