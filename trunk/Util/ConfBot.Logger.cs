/*
 * Created by SharpDevelop.
 * User: debe
 * Date: 19/09/2009
 * Time: 18.10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ConfBot
{
	/// <summary>
	/// Description of ConfBot_Logger.
	/// </summary>
	public class Logger : ILogger
	{
		private string _logFile;
		
		public Logger(string logFile)
		{
			_logFile = logFile;
		}
		
		public void LogMessage(string message, ConfBot.Types.LogLevel level)
		{
			String header = "[" + DateTime.Now.ToString() + "]";
			string levelStr = "";
			switch (level) {
				case ConfBot.Types.LogLevel.Error:
					levelStr = "[EE]";
					break;
				case ConfBot.Types.LogLevel.Warning:
					levelStr = "[WW]";
					break;
				case ConfBot.Types.LogLevel.Message:
					levelStr = "[II]";
					break;
			}
			
			if (_logFile.Trim() != "")
			{
				try
				{
					System.IO.StreamWriter sw = System.IO.File.AppendText(_logFile);
					
					sw.WriteLine(header + levelStr +": "+ message);
					sw.Close();
				}
				catch
				{
					
				}
			}
			else
			{
				//log to console
				Console.WriteLine(header + levelStr +": "+ message);
			}
		}
	}
}
