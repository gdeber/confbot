/*
 * Created by SharpDevelop.
 * User: debe
 * Date: 20/09/2008
 * Time: 16.39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */ 

using System;
using System.Threading;
using System.IO;
using System.Configuration;
using ConfBot;

namespace ConfBot
{
	class ConfBot
	{
		static Conference conf;
		static JabberClient _jabberClient;
	 	static Logger _logger;
		static ConfigManager _configMgr;
		
		const string CONFIGFILE	= "ConfBot.config";

		static void Main(string[] args)
		{
			ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
			configFile.ExeConfigFilename = args.Length > 0 ? args[0]: (CONFIGFILE) ;
			_configMgr = new ConfigManager(ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None));
			_logger = new Logger(_configMgr.GetSetting("LogFile"));
			_jabberClient = new JabberClient(_configMgr, _logger);
			conf = new Conference(_configMgr,_jabberClient,_logger);
			
			Thread confThread = new Thread(conf.Run);
			confThread.Start();
			
			while (!confThread.IsAlive);
			confThread.Join();
		}

	}

}
