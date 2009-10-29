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
		private string _logLocation;
		private bool _isFile = false;
		private bool _isDir = false;
		private string _errorFileName = "";
		private string _warningFileName = "";
		private string _infoFileName = "";
		
		public Logger(string logLocation)
		{
			_logLocation = logLocation;
			
			if (_logLocation.Trim() != "" && _logLocation.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
			{
				_isDir = true;
				try
				{
					System.IO.Directory.CreateDirectory(_logLocation);
					_errorFileName = _logLocation + "error.log";
					_warningFileName = _logLocation + "warning.log";
					_infoFileName = _logLocation + "info.log";
				}
				catch (Exception e)
				{
					Console.WriteLine("Error on create dir: " + e.Message);
				}
			}
			
			if (_logLocation.Trim() != "" && !_logLocation.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
			{
				_isFile = true;
			}
		}
		
		public void LogMessage(string message, ConfBot.Types.LogLevel level)
		{
			try
			{
				String header = "[" + DateTime.Now.ToString() + "]";
				string levelStr = "";
						
				switch (level) 
				{
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
				
				if (_logLocation.Trim() != "")
				{
					if (_isFile)
					{
						System.IO.StreamWriter sw = System.IO.File.AppendText(_logLocation);
						
						sw.WriteLine(header + levelStr +": "+ message);
						sw.Close();
						
					}
					else
					{
						//è una directory!
						
						System.IO.StreamWriter sw = null;
						
						switch (level) 
						{
							case ConfBot.Types.LogLevel.Error:
								sw = System.IO.File.AppendText(_errorFileName);
								break;
							case ConfBot.Types.LogLevel.Warning:
								sw = System.IO.File.AppendText(_warningFileName);
								break;
							case ConfBot.Types.LogLevel.Message:
								sw = System.IO.File.AppendText(_infoFileName);
								break;
						}
							
						sw.WriteLine(header +": "+ message);
						sw.Close();
						
					}
				}
				else
				{
					//log to console
					Console.WriteLine(header + levelStr +": "+ message);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error On Logging! "+ e.Message);
			}
		}
	}
}
