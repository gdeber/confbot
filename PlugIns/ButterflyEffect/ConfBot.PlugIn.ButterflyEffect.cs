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
		private string[] vofocafalifi = new string[5]{"fa","fe","fi","fo","fu"};
		private bool Active = false;
		
		private string farfallizza(string msg)
		{
			string farfalledMsg = "";
			for (int idx=0; idx < vocali.Length; idx++)
			{
				farfalledMsg = msg.Replace(vocali[idx],vofocafalifi[idx]);
			}
			
			return farfalledMsg;
		}

		public ButterflyEffect(Conference confObj) : base(confObj){

		}

		public override bool msgCommand(ref Message msg, ref string newMsg, out bool command)
		{
			command = false;
			//potrebbe essere un comando
			if (msg.Body.ToLower().StartsWith("/butterfly"))
			{
				command = true;
				if (confObj.isAdmin(msg.From))
				{
					//se più lungo significa che c'è il parametro del comando
					if (msg.Body.Length >= 11)
					{
						String tmpMsg = msg.Body.Remove(0, 11).Trim().ToLower();
						switch (tmpMsg) {
							case "on": 	Active = true;
										confObj.j.Message(msg.From, "*Butterfly Activated*");
										break;

							case "off":	Active = false;
										confObj.j.Message(msg.From, "*Butterfly DeActivated*");
										break;
										
							case "help":String helpString = "*butterfly* riscrive il tuo messaggio in alfabeto farfallino";
										helpString += "*/Butterfly help*: aiuto\n";
										helpString += "*/cleanmode [on|off]*: attiva/disattiva il cleanmode\n";
										confObj.j.Message(msg.From, helpString);																		
										break; 
										
							default:	if (Active) {
											confObj.j.Message(msg.From, "_Butterfly is active_");
										} else {
											confObj.j.Message(msg.From, "_Butterfly is not active_");
										}
										break;
						}
					}
				}
				else
				{
					confObj.j.Message(msg.From, Conference.NOADMINMSG);
				}
				
			}
			else
			{
				//non è un comando quindi devo crittografare il msg se il plugin è attivo
				if (Active)
				{
					newMsg= msg.Body;
					newMsg = farfallizza(newMsg);
				}
			}
			
			return true;

		}

		public override string Help()
		{
			return "*/butterfly help*: aiuto su butterfly";
		}
	}
}
