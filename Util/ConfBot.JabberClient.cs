/*
 * Created by SharpDevelop.
 * User: debe
 * Date: 19/09/2009
 * Time: 14.10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

using bedrock;
using ConfBot.Types;
using jabber.client;
using jabber.connection;
using jabber.protocol.client;
using jabber.protocol.iq;

namespace ConfBot
{
	/// <summary>
	/// Description of ConfBot_JabberClient.
	/// </summary>
	public class JabberClient : IJabberClient
	{
		private jabber.client.JabberClient _jabberClient= new jabber.client.JabberClient();
		private jabber.client.RosterManager _rosterManager = new jabber.client.RosterManager();
		private jabber.client.PresenceManager _presenceManager = new jabber.client.PresenceManager();
		private ILogger _logger;
		private IConfigManager _configMgr;
		private string _statusMessage = "";
		private string[] admins;
		
		private Dictionary<string, RosterItemWrapper> friendList = new Dictionary<string, RosterItemWrapper>();
		
		#region Constructor
		public JabberClient(IConfigManager configMgr, ILogger logger)
		{
			_configMgr = configMgr;
			_logger = logger;
			
			//administrators
			admins = _configMgr.GetSetting("Administrators").Split(',');
			
			_jabberClient.OnError += new ExceptionHandler(_jabberClient_OnError);
			_jabberClient.OnAuthenticate += new ObjectHandler(_jabberClient_OnAuthenticate);
			_jabberClient.OnMessage	+= new jabber.client.MessageHandler(_jabberClient_OnMessage);
			_jabberClient.OnReadText += new TextHandler(_jabberClient_OnReadText);
			_jabberClient.OnWriteText += new TextHandler(_jabberClient_OnWriteText);
			_jabberClient.OnInvalidCertificate += new RemoteCertificateValidationCallback(_jabberClient_OnInvalidCertificate);
			_jabberClient.OnPresence	+= new PresenceHandler(_jabberClient_OnPresence);
			_rosterManager.OnRosterItem += new RosterItemHandler(_rosterManager_OnRosterItem);
			_rosterManager.OnRosterEnd += new ObjectHandler(_rosterManager_OnRosterEnd);
			
			this.InitConnection();
			
		}

		#endregion
		
		#region EVENT
		public event ConfBot.Types.MessageHandler OnMessage;
		public event ErrorMessageEventHandler OnError;
		#endregion
		
		#region PUBLIC METHOD
		public IRoster Roster {
			get {
				RosterCollection tempFriendList = new RosterCollection();
				foreach (RosterItemWrapper item in friendList.Values)
				{
					tempFriendList.Add((item as IRosterItem));
				}
				
				return tempFriendList;
			}
		}
		
		public void SendMessage(IRosterItem rosterItem, string message)
		{
			_jabberClient.Message(rosterItem.JID.Bare, message);
		}
		
		public void SendMessage(IJID user, string message)
		{
			_jabberClient.Message(user.Bare, message);
		}
		
		public void SendMessage(string bare, string message)
		{
			_jabberClient.Message(bare, message);
		}
		
		public void Connect()
		{
			_jabberClient.Connect();
		}
		
		public void Close()
		{
			_jabberClient.Close();
		}
		
		#endregion
		
		#region PRIVATE METHOD
		private void InitConnection()
		{
			_jabberClient.User = _configMgr.GetSetting("Username");
			_jabberClient.Server = _configMgr.GetSetting("Server"); // use gmail.com for GoogleTalk
			_jabberClient.Password = _configMgr.GetSetting("Password");
			_jabberClient.NetworkHost = _configMgr.GetSetting("NetworkHost");
			_jabberClient.Port = Int32.Parse(_configMgr.GetSetting("Port"));
			//auth settings
			
			_jabberClient.AutoStartTLS = true;
			_jabberClient.KeepAlive = 5;
			_jabberClient.Resource = _configMgr.GetSetting("Resource");
			//j.Priority = 24;
			
			//Proxy settings
			if (_configMgr.GetSetting("ProxyHost").Trim() == "")
				_jabberClient.Proxy	= ProxyType.None;
			else
			{
				string proxyType;
				if ( (proxyType = _configMgr.GetSetting("ProxyType").Trim()) == "")
				{
					_jabberClient.Proxy	= ProxyType.Socks5;
				}
				else
				{
					if (proxyType.Equals("Socks5", StringComparison.InvariantCultureIgnoreCase))
					{
						_jabberClient.Proxy = ProxyType.Socks5;
					}
					if (proxyType.Equals("Socks4", StringComparison.InvariantCultureIgnoreCase))
					{
						_jabberClient.Proxy = ProxyType.Socks4;
					}
					if (proxyType.Equals("HTTP", StringComparison.InvariantCultureIgnoreCase))
					{
						_jabberClient.Proxy = ProxyType.HTTP;
					}
				}
				_jabberClient.ProxyHost = _configMgr.GetSetting("ProxyHost");
				_jabberClient.ProxyPort = Int32.Parse(_configMgr.GetSetting("ProxyPort"));
			}
			
			// don't do extra stuff, please.
			_jabberClient.AutoPresence = false;
			_jabberClient.AutoRoster = true;
			_jabberClient.AutoReconnect = 0;
			
			_rosterManager.Stream = _jabberClient;
			_presenceManager.Stream = _jabberClient;
		}
		
		private bool isAdmin(string user)
		{
			foreach (string admin in admins)
			{
				if (user.Equals(admin))
				{
					return true;
				}
			}
			return false;
		}
		
		#endregion
		
		#region EVENT HANDLER
		bool _jabberClient_OnInvalidCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		void _jabberClient_OnWriteText(object sender, string txt)
		{
			if (txt == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("SEND: " + txt);
			_logger.LogMessage("SEND: " + txt, LogLevel.Message);
		}

		void _jabberClient_OnReadText(object sender, string txt)
		{
			if (txt == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("RECV: " + txt);
			_logger.LogMessage("RECV: " + txt, LogLevel.Message);
		}

		void _jabberClient_OnMessage(object sender, jabber.protocol.client.Message msg)
		{
			if (this.OnMessage != null)
			{
				this.OnMessage(sender,new JabberMessage(msg));
			}
		}

		void _jabberClient_OnAuthenticate(object sender)
		{
			_logger.LogMessage("Authenticated!" , LogLevel.Message);
		}

		void _jabberClient_OnError(object sender, Exception ex)
		{
			if (this.OnError != null)
			{
				this.OnError(sender, ex);
			}
		}
		
		void _jabberClient_OnPresence(object sender, Presence pres)
		{
			try {
				_logger.LogMessage("Presence from: " + pres.From.Bare, LogLevel.Message);
				if (pres.From.Bare != this._jabberClient.JID.Bare)
				{
					if (friendList[pres.From.Bare] != null)
					{
						friendList[pres.From.Bare].pres = pres;
					}
				}
			} catch (Exception ex) {
				_logger.LogMessage("error on presence" + ex.Message, LogLevel.Error);
			}
			
		}
		
		void _rosterManager_OnRosterItem(object sender, Item ri)
		{
			try {
				_logger.LogMessage("Roster Item: " + ri.JID.Bare + " Subscription: " + ri.Subscription.ToString() , LogLevel.Message);
				if (ri.Subscription == Subscription.remove)
				{
					friendList.Remove(ri.JID.Bare);
				}
				else
				{
					RosterItemWrapper friend = new RosterItemWrapper(ri);
					friend.IsAdmin = this.isAdmin(friend.JID.Bare);
					friendList.Add(ri.JID.Bare, friend);
				}
			} catch (Exception ex) {
				_logger.LogMessage("error on Roster Item" + ex.Message, LogLevel.Error);
			}
			
		}
		
		void _rosterManager_OnRosterEnd(object sender)
		{
			//now send presence
			_logger.LogMessage("Send Presence..." , LogLevel.Message);
			_jabberClient.Presence(PresenceType.available, _statusMessage, "available", 0);
		}
		
		#endregion
		
		public IJID JID {
			get {
				return (IJID)_jabberClient.JID;
			}
		}
		
		public string Nickname {
			get {
				return _jabberClient.User;
			}
			set {
				_jabberClient.User = value;
			}
		}
		
		public string StatusMessage {
			get{
				return _statusMessage;
			}
			set {
				_jabberClient.Presence(jabber.protocol.client.PresenceType.available, value, "available", 0);
				_statusMessage = value;
			}
		}
	}
	
	internal class JabberMessage : IMessage
	{
		private jabber.protocol.client.Message _message;
		
		public JabberMessage(jabber.protocol.client.Message message)
		{
			_message = message;
		}
		
		public IJID From {
			get {
				return new JIDWrapper(_message.From.Bare);
			}
			set {
				_message.From = new jabber.JID(value.Bare);
			}
		}
		
		public IJID To {
			get {
				return new JIDWrapper(_message.To);
			}
			set {
				_message.To = new jabber.JID(value.Bare);
			}
		}
		
		public ConfBot.Types.MessageType Type {
			get {
				switch (_message.Type) {
					case jabber.protocol.client.MessageType.error:
						return ConfBot.Types.MessageType.error;
					case jabber.protocol.client.MessageType.chat:
						return ConfBot.Types.MessageType.chat;
					default:
						return ConfBot.Types.MessageType.chat;
				}
			}
			set {
				switch (value) {
					case ConfBot.Types.MessageType.error:
						_message.Type = jabber.protocol.client.MessageType.error;
						break;
					case ConfBot.Types.MessageType.chat:
						_message.Type = jabber.protocol.client.MessageType.chat;
						break;
				}
			}
		}
		
		public string Body {
			get {
				return _message.Body;
			}
			set {
				_message.Body = value;
			}
		}
	}
	
	internal class JIDWrapper :  IJID
	{
		private jabber.JID _jid;
		
		public JIDWrapper(jabber.JID jid)
		{
			_jid = jid;
		}
		
		public string Bare {
			get {
				return _jid;
			}
		}
		
		public string Resource {
			get {
				return _jid.Resource;
			}
			set {
				_jid.Resource = value;
			}
		}
		
		public string Server {
			get {
				return _jid.Server;
			}
			set {
				_jid.Server = value;
			}
		}
		
		public string User {
			get {
				return _jid.User;
			}
			set {
				_jid.User = value;
			}
		}
	}
	
	internal class RosterItemWrapper : IRosterItem
	{
		
		private jabber.protocol.iq.Item _item;
		private jabber.protocol.client.Presence _presence = null;
		private bool _isAdmin = false;
		
		public RosterItemWrapper(jabber.protocol.iq.Item rosterItem)
		{
			_item = rosterItem;
		}
		
		public IJID JID {
			get {
				return new JIDWrapper(_item.JID);
			}
		}
		
		public Item item
		{
			get{
				return _item;
			}
		}
		
		public Presence pres {
			get{
				return _presence;
			}
			
			set{
				_presence = value;
			}
		}
		
		public UserStatus status {
			get {
				if (_presence == null || _presence.Type == PresenceType.unavailable)
				{
					return UserStatus.NotAvailable;
				}
				else
				{
					switch (_presence.Show) {
						case "dnd":
							return UserStatus.DoNotDisturb;
							
						case "away" :
						case "xa" :
							return UserStatus.Away;
							
						case "chat":
						default:
							return UserStatus.OnLine;
					}
				}
			}
		}
		
		public bool IsAdmin {
			get {
				return _isAdmin;
			}
			set {
				_isAdmin = value;
			}
		}
		
		public string Nickname {
			get {
				return _item.Nickname;
			}
			set {
				_item.Nickname = value;
			}
		}
	}
	
	internal class RosterCollection : Collection<IRosterItem>, IRoster
	{
		
		
		public IRosterItem this[string user] {
			get {
				IRosterItem item = this.findItem(user);
				if (item != null)
				{
					return item;
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
			set {
				IRosterItem item = this.findItem(user);
				if (item != null)
				{
					item = value;
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}
		
		private IRosterItem findItem(string user)
		{
			foreach (IRosterItem item in this)
			{
				if ( (item.JID.Bare.Equals(user, StringComparison.InvariantCultureIgnoreCase)) ||
				    (item.Nickname.Equals(user, StringComparison.InvariantCultureIgnoreCase)) )
				{
					return item;
				}
			}
			return null;
		}
		
		public bool IsReadOnly {
			get {
				return false;
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return base.GetEnumerator();
		}
	}
}
