/*
 * Created by SharpDevelop.
 * User: Gualtiero
 * Date: 24/09/2009
 * Time: 12.28
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using NUnit.Framework;
using System.Configuration;

namespace ConfBot
{
	[TestFixture]
	public class TestConfigManager
	{
		[Test]
		public void TestMethod()
		{
			ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
			configFile.ExeConfigFilename = "AutmConference.config" ;
			ConfigManager _configMgr = new ConfigManager(ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None));
			
			Assert.AreEqual(_configMgr.GetSetting("LogFile"), "./AutmConferenceMono.log");
		}
	}
}
