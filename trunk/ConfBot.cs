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
using jabber;
using jabber.client;
using jabber.connection;
using System.IO;
using System.Configuration;
using ConfBot;

namespace ConfBot
{
	class ConfBot
	{
		static Conference conf;
		
		const string CONFIGFILE	= "ConfBot.config";

		static void Main(string[] args)
		{
			ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
			configFile.ExeConfigFilename = args.Length > 0 ? args[0]: (".\\" + CONFIGFILE) ;
			conf = new Conference(ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None));
			Thread confThread = new Thread(conf.Run);
			confThread.Start();
			
			while (!confThread.IsAlive);
			confThread.Join();
		}

	}

}
