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
		
		private string farfallizza(string msg)
		{
			string farfalledMsg = msg;
			for (int idx=0; idx < vocali.Length; idx++)
			{
				farfalledMsg = farfalledMsg.Replace(vocali[idx],vofocafalifi[idx]);
			}
			
			return farfalledMsg;
		}

		public ButterflyEffect(Conference confObj) : base(confObj){

		}

		public override bool msgCommand(ref Message msg, ref string newMsg, out bool command)
		{
			command = false;
			try {
				
				//potrebbe essere un comando
				if (msg.Body.ToLower().StartsWith("/butterfly"))
				{
					command = true;
					if (confObj.isAdmin(msg.From.Bare))
					{
						//se più lungo significa che c'è il parametro del comando
						if (msg.Body.Length >= 10)
						{
							String tmpMsg = msg.Body.Remove(0, 10).Trim().ToLower();
							switch (tmpMsg) {
									case "on": 	Active = true;
												confObj.j.Message(msg.From, "*Butterfly Activated*");
												break;

									case "off":	Active = false;
												confObj.j.Message(msg.From, "*Butterfly DeActivated*");
												break;
									
									case "help":String helpString = "*butterfly* riscrive il tuo messaggio in alfabeto farfallino\n";
												helpString += "*/Butterfly help*: aiuto\n";
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
						else{
							confObj.j.Message(msg.From, "Che dice?!");
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
				
			} catch (Exception e) {
				confObj.LogMessageToFile(e.Message);
				return false;
			}
			
		}

		public override string Help()
		{
			return "*/butterfly help*: aiuto su butterfly";
		}
	}
}
