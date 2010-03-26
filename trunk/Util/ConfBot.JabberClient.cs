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

using agsXMPP;
using agsXMPP.protocol.client;
using ConfBot.Types;
using agsXMPP.protocol.iq.roster;

/*using jabber.client;
using jabber.connection;
using jabber.protocol.client;
using jabber.protocol.iq;
 */

namespace ConfBot
{
	/// <summary>
	/// Description of ConfBot_JabberClient.
	/// </summary>
	public class JabberClient : IJabberClient
	{
		private XmppClientConnection _xmppConn = new XmppClientConnection();
		
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
			
			_xmppConn.OnError += new ErrorHandler(_xmppConn_OnError);
			_xmppConn.OnLogin += new ObjectHandler(_xmppConn_OnLogin);
			_xmppConn.OnMessage += new agsXMPP.protocol.client.MessageHandler(_xmppConn_OnMessage);
			_xmppConn.ClientSocket.OnValidateCertificate += new RemoteCertificateValidationCallback(_xmppConn_ClientSocket_OnValidateCertificate);
			_xmppConn.OnPresence += new PresenceHandler(_xmppConn_OnPresence);
			_xmppConn.OnRosterItem += new XmppClientConnection.RosterHandler(_xmppConn_OnRosterItem);
			_xmppConn.OnRosterEnd += new ObjectHandler(_xmppConn_OnRosterEnd);
			_xmppConn.OnSocketError += new ErrorHandler(_xmppConn_OnSocketError);
			
			//for debug purpose
			_xmppConn.OnReadXml += new XmlHandler(_xmppConn_OnReadXml);
			_xmppConn.OnWriteXml += new XmlHandler(_xmppConn_OnWriteXml);
			
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
			_xmppConn.Send(new Message(new Jid(rosterItem.JID.Bare), agsXMPP.protocol.client.MessageType.chat, message));
		}
		
		public void SendMessage(IJID user, string message)
		{
			_xmppConn.Send(new Message(new Jid(user.Bare), agsXMPP.protocol.client.MessageType.chat, message));
		}
		
		public void SendMessage(string bare, string message)
		{
			_xmppConn.Send(new Message(new Jid(bare), agsXMPP.protocol.client.MessageType.chat, message));
		}
		
		public void Connect()
		{
			_xmppConn.Open();
		}
		
		public void Close()
		{
			_xmppConn.Close();
		}
		
		#endregion
		
		#region PRIVATE METHOD
		private void InitConnection()
		{
			_xmppConn.Username = _configMgr.GetSetting("Username");
			_xmppConn.Server = _configMgr.GetSetting("Server"); // use gmail.com for GoogleTalk
			_xmppConn.Password = _configMgr.GetSetting("Password");
			_xmppConn.ConnectServer = _configMgr.GetSetting("NetworkHost");
			_xmppConn.Port = Int32.Parse(_configMgr.GetSetting("Port"));
			_xmppConn.Resource = _configMgr.GetSetting("Resource");
			
			if (_configMgr.GetSetting("StatusMessage").Trim() != "")
			{
				_statusMessage =_configMgr.GetSetting("StatusMessage");
			}
			
//			//Proxy settings
//			if (_configMgr.GetSetting("ProxyHost").Trim() == "")
//				_xmppCon
//				_jabberClient.Proxy	= ProxyType.None;
//			else
//			{
//				string proxyType;
//				if ( (proxyType = _configMgr.GetSetting("ProxyType").Trim()) == "")
//				{
//					_jabberClient.Proxy	= ProxyType.Socks5;
//				}
//				else
//				{
//					if (proxyType.Equals("Socks5", StringComparison.InvariantCultureIgnoreCase))
//					{
//						_jabberClient.Proxy = ProxyType.Socks5;
//					}
//					if (proxyType.Equals("Socks4", StringComparison.InvariantCultureIgnoreCase))
//					{
//						_jabberClient.Proxy = ProxyType.Socks4;
//					}
//					if (proxyType.Equals("HTTP", StringComparison.InvariantCultureIgnoreCase))
//					{
//						_jabberClient.Proxy = ProxyType.HTTP;
//					}
//				}
//				_jabberClient.ProxyHost = _configMgr.GetSetting("ProxyHost");
//				_jabberClient.ProxyPort = Int32.Parse(_configMgr.GetSetting("ProxyPort"));
//			}
			
			// don't do extra stuff, please.
			_xmppConn.AutoPresence = false;
			_xmppConn.AutoRoster = true;
			_xmppConn.UseSSL = true;
			_xmppConn.AutoResolveConnectServer = false;
			_xmppConn.KeepAlive = true;
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
		bool _xmppConn_ClientSocket_OnValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}
		
		void _xmppConn_OnWriteXml(object sender, string xml)
		{
			if (xml == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("SEND: " + txt);
			_logger.LogMessage("SEND: " + xml, LogLevel.Message);
		}

		void _xmppConn_OnReadXml(object sender, string xml)
		{
			if (xml == " ") return;  // ignore keep-alive spaces
			//Console.WriteLine("RECV: " + txt);
			_logger.LogMessage("RECV: " + xml, LogLevel.Message);
		}
		
		void _xmppConn_OnMessage(object sender, Message msg)
		{
			if (this.OnMessage != null)
			{
				this.OnMessage(sender,new JabberMessage(msg));
			}
		}

		void _xmppConn_OnLogin(object sender)
		{
			_logger.LogMessage("Authenticated!" , LogLevel.Message);
		}

		void _xmppConn_OnError(object sender, Exception ex)
		{
			if (this.OnError != null)
			{
				this.OnError(sender, ex);
			}
		}
		
		void _xmppConn_OnPresence(object sender, Presence pres)
		{
			try {
				_logger.LogMessage("Presence from: " + pres.From.Bare, LogLevel.Message);
				//sono io?
				if (pres.From.Bare != this._xmppConn.MyJID.Bare.ToLowerInvariant())
				{
					string bareNameInv = pres.From.Bare.ToLowerInvariant();
					if (friendList[bareNameInv] != null)
					{
						//c'è l'utente
						friendList[bareNameInv].UpdatePresence(pres);
					}
					else
					{
						_logger.LogMessage(String.Format("Presence from user not in roster...mah! {0}", pres.From.ToString()), LogLevel.Warning);
					}
				}
			} catch (Exception ex) {
				_logger.LogMessage("error on presence: " + ex.Message, LogLevel.Error);
			}
		}
		
		void _xmppConn_OnRosterItem(object sender, RosterItem item)
		{
			try {
				_logger.LogMessage("Roster Item: " + item.Jid.Bare + " Subscription: " + item.Subscription.ToString() , LogLevel.Message);
				if (item.Subscription == SubscriptionType.remove)
				{
					friendList.Remove(item.Jid.Bare.ToLowerInvariant());
				}
				else
				{
					if (!friendList.ContainsKey(item.Jid.Bare.ToLowerInvariant()))
					{
						RosterItemWrapper friend = new RosterItemWrapper(item);
						friend.IsAdmin = this.isAdmin(friend.JID.Bare.ToLowerInvariant());
						friendList.Add(item.Jid.Bare.ToLowerInvariant(), friend);
					}
				}
			} catch (Exception ex) {
				_logger.LogMessage(String.Format("error on Roster Item {0}: {1} ", item.Jid.Bare, ex.Message), LogLevel.Error);
			}
		}
		
		void _xmppConn_OnRosterEnd(object sender)
		{
			//now send presence
			_logger.LogMessage("Send Presence..." , LogLevel.Message);
			_xmppConn.Status = _statusMessage;
			_xmppConn.Show = ShowType.NONE;
			_xmppConn.SendMyPresence();
		}
		
		void _xmppConn_OnSocketError(object sender, Exception ex)
		{
			_logger.LogMessage("Socket Error...", LogLevel.Error);
			if (this.OnError != null)
			{
				this.OnError(sender, ex);
			}
		}
		
		#endregion
		
		public IJID JID {
			get {
				return (IJID)_xmppConn.MyJID;
			}
		}
		
		public string Nickname {
			get {
				return _xmppConn.Username;
			}
			set {
				_xmppConn.Username = value;
			}
		}
		
		public string StatusMessage {
			get{
				return _statusMessage;
			}
			set {
				_statusMessage = value;
				_xmppConn.Status = _statusMessage;
				_xmppConn.SendMyPresence();
			}
		}
	}
	
	internal class JabberMessage : IMessage
	{
		private Message _message;
		
		public JabberMessage(Message message)
		{
			_message = message;
		}
		
		public IJID From {
			get {
				return new JIDWrapper(_message.From);
			}
			set {
				_message.From = new Jid(value.Bare);
			}
		}
		
		public IJID To {
			get {
				return new JIDWrapper(_message.To);
			}
			set {
				_message.To = new Jid(value.Bare);
			}
		}
		
		public ConfBot.Types.MessageType Type {
			get {
				switch (_message.Type) {
					case agsXMPP.protocol.client.MessageType.error:
						return ConfBot.Types.MessageType.error;
					case agsXMPP.protocol.client.MessageType.chat:
						return ConfBot.Types.MessageType.chat;
					default:
						return ConfBot.Types.MessageType.chat;
				}
			}
			set {
				switch (value) {
					case ConfBot.Types.MessageType.error:
						_message.Type = agsXMPP.protocol.client.MessageType.error;
						break;
					case ConfBot.Types.MessageType.chat:
						_message.Type = agsXMPP.protocol.client.MessageType.chat;
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
		
		IError IMessage.Error {
			get {
				if (_message.Error != null)
				{
					return new ErrorWrapper(_message.Error);
				}
				else
				{
					return null;
				}
			}
		}
	}
	
	internal class ErrorWrapper : IError
	{
		private agsXMPP.protocol.client.Error _error;
		
		public ErrorWrapper (agsXMPP.protocol.client.Error error)
		{
			_error = error;
		}
		
		ConfBot.Types.ErrorCode IError.Code {
			get {
				return (ConfBot.Types.ErrorCode)_error.Code;
			}
		}
	}
	
	internal class JIDWrapper :  IJID
	{
		private agsXMPP.Jid _jid;
		
		public JIDWrapper(agsXMPP.Jid jid)
		{
			_jid = jid;
		}
		
		public string Bare {
			get {
				return _jid.Bare;
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
		
		private agsXMPP.protocol.iq.roster.RosterItem _item;
		private List<agsXMPP.protocol.client.Presence> _presences = new List<agsXMPP.protocol.client.Presence>();
		private bool _isAdmin = false;
		
		public RosterItemWrapper(agsXMPP.protocol.iq.roster.RosterItem rosterItem)
		{
			_item = rosterItem;
		}
		
		public IJID JID {
			get {
				return new JIDWrapper(_item.Jid);
			}
		}
		
		public agsXMPP.protocol.iq.roster.RosterItem item
		{
			get{
				return _item;
			}
		}
		
//		public List<agsXMPP.protocol.client.Presence> presences {
//			get{
//				return _presences;
//			}
//		}
		
		public UserStatus status {
			get {
				if (_presences == null || this.PresencesSameType(PresenceType.unavailable)
				    || _presences.Count == 0)
				{
					return UserStatus.NotAvailable;
				}
				else
				{
					if (this.PresenceSameShow(ShowType.dnd))
					{
						return UserStatus.DoNotDisturb;
					}
					if (this.PresenceSameShow(ShowType.away) || this.PresenceSameShow(ShowType.xa))
					{
						return UserStatus.Away;
					}
					
					return UserStatus.OnLine;
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
				return _item.Name;
			}
			set {
				_item.Name = value;
			}
		}
		
		public void UpdatePresence(agsXMPP.protocol.client.Presence recvPres)
		{
			for (int presIdx = 0; presIdx<_presences.Count ; presIdx++ )
			{
				if (_presences[presIdx].From.Resource.Equals(recvPres.From.Resource))
				{
					//trovata presenza da aggiornare
					if (recvPres.Type == PresenceType.unavailable)
					{
						_presences.Remove(_presences[presIdx]);
						return;
					}
					else
					{
						_presences[presIdx] = recvPres;
						return;
					}
				}
			}
			
			//non ho trovato presenza
			if (recvPres.Type != PresenceType.unavailable)
			{
				_presences.Add(recvPres);
			}
		}
		
		private bool PresencesSameType(PresenceType type)
		{
			if (_presences.Count == 0)
			{
				return false;
			}
			
			foreach(Presence presItem in _presences)
			{
				if (presItem.Type != type)
				{
					return false;
				}
			}
			
			return true;
		}
		
		private bool PresenceSameShow(ShowType show)
		{
			if (_presences.Count == 0)
			{
				return false;
			}
			
			foreach(Presence presItem in _presences)
			{
				if (presItem.Show != show)
				{
					return false;
				}
			}
			
			return true;
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
					return null;
				}
			}
//			set {
//				IRosterItem item = this.findItem(user);
//				if (item != null)
//				{
//					item = value;
//				}
//				else
//				{
//					throw new ArgumentOutOfRangeException();
//				}
//			}
		}
		
		private IRosterItem findItem(string user)
		{
			try {
				foreach (IRosterItem item in this)
				{
					if ( (item.JID.Bare.Equals(user, StringComparison.InvariantCultureIgnoreCase)) ||
					    (item.Nickname.Equals(user, StringComparison.InvariantCultureIgnoreCase)) )
					{
						return item;
					}
				}
				return null;
			} catch (Exception) {
				return null;
			}
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
