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
	
	/// <summary>
	/// The legacy Error Code
	/// </summary>
	public enum ErrorCode
	{		
		/// <summary>
		/// Bad request
		/// </summary>
		BadRequest				= 400,
		/// <summary>
		/// Unauthorized
		/// </summary>
		Unauthorized			= 401,
		/// <summary>
		/// Payment required
		/// </summary>
		PaymentRequired			= 402,
		/// <summary>
		/// Forbidden
		/// </summary>
		Forbidden				= 403,
		/// <summary>
		/// Not found
		/// </summary>
		NotFound				= 404,
		/// <summary>
		/// Not allowed
		/// </summary>
		NotAllowed				= 405,
		/// <summary>
		/// Not acceptable
		/// </summary>
		NotAcceptable			= 406,
		/// <summary>
		/// Registration required 
		/// </summary>
		RegistrationRequired	= 407,
		/// <summary>
		/// Request timeout
		/// </summary>
		RequestTimeout			= 408,
		/// <summary>
		/// Conflict
		/// </summary>
		Conflict                = 409,
		/// <summary>
		/// Internal server error
		/// </summary>
		InternalServerError		= 500,
		/// <summary>
		/// Not implemented
		/// </summary>
		NotImplemented			= 501,
		/// <summary>
		/// Remote server error
		/// </summary>
		RemoteServerError		= 502,
		/// <summary>
		/// Service unavailable
		/// </summary>
		ServiceUnavailable		= 503,
		/// <summary>
		/// Remote server timeout
		/// </summary>
		RemoteServerTimeout		= 504,
		/// <summary>
		/// Disconnected
		/// </summary>
		Disconnected            = 510
	}
	
	public delegate void ErrorMessageEventHandler(object sender, Exception ex);
	public delegate void MessageHandler(object sender, IMessage message);
}
