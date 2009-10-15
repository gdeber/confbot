using System;
using System.Collections.Generic;
using System.Text;
using ConfBot;
using ConfBot.PlugIns;
using ConfBot.Types;

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

		public ButterflyEffect(IJabberClient jabberClient,IConfigManager configManager, ILogger logger) : base(jabberClient, configManager, logger){
			#region Command Initialization
			BotCmd tempCmd;
			// Butterfly Command
			tempCmd.Command	= "butterfly";
			tempCmd.Code	= Convert.ToInt32(Commands.Butterfly);
			tempCmd.Admin	= true;
			tempCmd.Help	= "[on|off] attiva/disattiva il Butterfly Effect";
			listCmd.Add(tempCmd);
			#endregion
			Active	= (this._configManager.GetSetting("Butterfly") == "on");
		}

		#region Command Class override
		private enum Commands {
			Butterfly	= 1		
		}
		
		public override bool ExecCommand(IJID user, int CodeCmd, string Param) {
			switch((Commands)CodeCmd) {
				case Commands.Butterfly		: 
												switch (Param) {
													case "on" 	:	Active	= true;
																	_jabberClient.SendMessage(user, "*Butterfly Activated*");
																	//ora lo salvo
																	_configManager.SetSetting("Butterfly", "on");
																	break;
													case "off"	:	Active	= false;
																	_jabberClient.SendMessage(user, "*Butterfly Deactivated*");
																	//ora lo salvo
																	_configManager.SetSetting("Butterfly", "off");
																	break;
													case "help"	:	String helpString = "*butterfly* riscrive il tuo messaggio in alfabeto farfallino\n";
																	helpString += "*/Butterfly help*: aiuto\n";
																	_jabberClient.SendMessage(user, helpString);
																	break;
													default		:	if (Active) {
																		_jabberClient.SendMessage(user, "Butterfly is active_");
																	} else {
																		_jabberClient.SendMessage(user, "Butterfly is not active_");
																	}
																	break;
												}
												break;
			}
			return true;
		}
		#endregion

		public override bool msgCommand(ref IMessage msg, ref string newMsg, out bool command)
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
							_jabberClient.SendMessage(msg.From, msgBody);
						}
					}
				}
				catch(Exception ex) {
					_logger.LogMessage(ex.Message, LogLevel.Error);
				}
			}
			return true;
	
		}
	}
}
