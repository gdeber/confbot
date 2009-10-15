/*
 * Created by SharpDevelop.
 * User: debe
 * Date: 19/09/2009
 * Time: 11.51
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using ConfBot.Types;
using jabber.client;

namespace ConfBot
{
	/// <summary>
	/// Description of ConfBot_Interfaces.
	/// </summary>
	public interface ILogger
	{
		void LogMessage(string message, LogLevel level);
	}
	
	public interface IConfigManager
	{
		string GetSetting(string settingName);
		
		bool SetSetting(string settingName, string val);
	}
	
	public interface IJabberClient
	{
		IRoster Roster
		{
			get;
		}
		
		
		IJID JID
		{
			get;
		}
		
		string Nickname
		{
			get;
			set;
		}
		
		string StatusMessage
		{
			get;
			set;
		}
		
		event ConfBot.Types.MessageHandler OnMessage;
		event ErrorMessageEventHandler OnError;
		
		void SendMessage(IRosterItem rosterItem, string message);
		void SendMessage(IJID user, string message);
		void SendMessage(string bare, string message);
		void Connect();
		void Close();
		
		
	}
	
	public interface IJID
	{
		string Bare
		{
			get;
		}
		string Resource
		{
			get;
			set;
		}
		string Server
		{
			get;
			set;
		}
		string User
		{
			get;
			set;
			
		}
	}
	
	public interface IRoster: ICollection<IRosterItem>
	{
		IRosterItem this[string user]
		{
			get;
			set;
		}
	}
	
	public interface IRosterItem
	{
		IJID JID
		{
			get;
		}
		UserStatus status
		{
			get;
		}
		string Nickname
		{
			get;
			set;
		}
		bool IsAdmin
		{
			get;
			set;
		}
	}
	
	public interface IMessage
	{
		IJID From
		{
			get;
			set;
		}
		
		IJID To
		{
			get;
			set;
		}
		
		MessageType Type
		{
			get;
			set;
		}
		
		string Body
		{
			get;
			set;
		}
	}
}
