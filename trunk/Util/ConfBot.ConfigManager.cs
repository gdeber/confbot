/*
 * Created by SharpDevelop.
 * User: debe
 * Date: 19/09/2009
 * Time: 11.43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using ConfBot.Types;
using System;
using System.Configuration;

namespace ConfBot
{
	/// <summary>
	/// Description of ConfigManager.
	/// </summary>
	public class ConfigManager : IConfigManager
	{
		private Configuration _config;
		private ILogger _log;
		
		public ConfigManager(Configuration config, ILogger log)
		{
			_config = config;
			_log = log;
		}
		public ConfigManager(Configuration config): this(config, null)
		{
		
		}
		
		public string GetSetting(string settingName)
		{
			try 
			{
				return _config.AppSettings.Settings[settingName].Value;				
			}
			catch (Exception E) 
			{
				if (_log != null)
				{
					_log.LogMessage("GetSetting " + E.Message, LogLevel.Error);
				}
			}
			return "";
		}
		
		public bool SetSetting(string settingName, string val)
		{
			try {
				_config.AppSettings.Settings[settingName].Value = val;
				_config.Save();
				return true;
			}
			catch (Exception E) {
				if (_log != null)
				{
					_log.LogMessage("SetSetting " + E.Message, LogLevel.Error);
				}
			}
			return false;
		}
	}
}
