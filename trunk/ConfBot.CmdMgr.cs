/*
 * Creato da SharpDevelop.
 * Utente: Andre
 * Data: 17/02/2009
 * Ora: 14.35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using ConfBot;
using ConfBot.PlugIns;

namespace ConfBot
{

	public struct BotCmd {
		public string Command;
		public bool Admin;
		public string Help;
		public int Code;
	}
	
	public class Command {
		protected List<BotCmd> listCmd = new List<BotCmd>();
		
		public Command() {
			//
		}
		
		public List<BotCmd> GetCommands() {
			return listCmd;
		}
		
		public virtual bool ExecCommand(IJID user, int CodeCmd, string Param) {
			return false;
		}

		public string Help(bool isAdmin) {
			string helpMsg = "";
			foreach(BotCmd cmd in listCmd) {
				if ((cmd.Admin && isAdmin) || (!cmd.Admin)) {
					helpMsg += "*/" + cmd.Command + "* ";
					if (cmd.Help.Trim() != "") {
						helpMsg += ": " + cmd.Help;
					}
					helpMsg += "\n";
				}
			}
			return helpMsg;
		}
		
	}
	
	/// <summary>
	/// Description of ConfBot_CmdMgr.
	/// </summary>
	public class CmdMgr
	{
		private struct botCommand {
			public bool Admin;
			public string Help;
			public int Code;
			public Command CmdClass;
		}

		private Dictionary<string, botCommand> cmdDict = new Dictionary<string, botCommand>();
		private IJabberClient _jabberClient;
		
		public CmdMgr(IJabberClient jabberClient)
		{
			_jabberClient = jabberClient;
		}
		
		public void AddCommand(BotCmd command, Command refCmdClass) {
			botCommand temp = new botCommand();
			temp.Admin		= command.Admin;
			temp.Code		= command.Code;
			temp.Help		= command.Help;
			temp.CmdClass	= refCmdClass;
			cmdDict.Add(command.Command.ToLower(), temp);
		}

		public void AddCommands(Command refCmdClass) {
			foreach(BotCmd command in refCmdClass.GetCommands()) {
				AddCommand(command, refCmdClass);
			}
		}

		public bool ExecCommand(IJID user, string Message) {
			
			if (Message.Trim().StartsWith("/")) {
				string lsTemp	= Message.Trim().ToLower();
				int liPos	= Message.IndexOf(' ' );
				string lsCommand = "";
				string lsParam	= "";
				if (liPos == -1) {
					lsCommand = Message.Substring(1);
				} else {
					lsCommand = Message.Substring(1, liPos - 1);
					lsParam	= Message.Substring(liPos + 1, lsTemp.Length - liPos - 1);
				}
				botCommand cmd;
				if (cmdDict.TryGetValue(lsCommand, out cmd)) {
					if (cmd.Admin) { 
						if (!_jabberClient.Roster[user.Bare].IsAdmin) {
							_jabberClient.SendMessage(user, Conference.NOADMINMSG);
							return true;
						}
					}
					cmd.CmdClass.ExecCommand(user, cmd.Code, lsParam);
				} else {
					_jabberClient.SendMessage(user, "Unknown Command");
				}
				return true;
			} else {
				return false;
			}
		}
	}
}
