/*
 * Created by SharpDevelop.
 * User: debe
 * Date: 19/09/2009
 * Time: 11.54
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ConfBot.Types
{
	public enum LogLevel{
		Message = 2,
		Warning = 1,
		Error = 0,
	}
	
	public enum UserStatus {
		Unknown = 0,
		NotAvailable = 1,
		Away = 2,
		DoNotDisturb = 3,
		OnLine = 4		
	}
	
	public enum MessageType {
		chat = 0,
		error = 1,
	}
	
	public delegate void ErrorMessageEventHandler(object sender, Exception ex);
	public delegate void MessageHandler(object sender, IMessage message);
}
