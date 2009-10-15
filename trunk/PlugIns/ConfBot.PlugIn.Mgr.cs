/*
 * Creato da SharpDevelop.
 * Utente: Andre
 * Data: 14/12/2008
 * Ora: 11.38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using ConfBot.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ConfBot.PlugIns;

//using PlugInList	= System.Collections.Generic.List<ConfBot.PlugIns.PlugIn>;

namespace ConfBot.PlugIns
{
	/// <summary>
	/// Description of ConfBot_PlugIn_Mgr.
	/// </summary>
	public class PlugInMgr
	{
		private ILogger	_logger;
		private Conference _confObj;
		private List<PlugIn>	pluginList = new List<PlugIn>();
		
		public PlugInMgr(ILogger logger, Conference confObj, string dirPlugIns)
		{
			this._logger = logger;
			_confObj = confObj;
			
			if (System.IO.Directory.Exists(dirPlugIns))
			{
				foreach (String fileName in System.IO.Directory.GetFiles(dirPlugIns, "*.dll")) {
					AnalyzeAssemblyFile(fileName);
				}
				
				for(int Ndx = 0; Ndx <= (pluginList.Count - 1); Ndx++)
				{
					if (((PlugIn) pluginList[Ndx]).IsThread()) {
						Thread thr = new Thread(((PlugIn) pluginList[Ndx]).StartThread);
						thr.Priority = ThreadPriority.BelowNormal;
						thr.Start();
					}
				}
			}
			else 
			{
				_logger.LogMessage("No plugin directory", LogLevel.Warning);
			}
		}
		
		private void AnalyzeAssemblyFile(String fileName)
		{
		
			try
			{
				Assembly asm = Assembly.LoadFrom(fileName);
				foreach (Type Ty in asm.GetTypes())
				{
					if (Ty.IsDefined(typeof(PlugInAttribute), false))
					{
						object[] paramsPlug = new object[1];
						paramsPlug[0]	= _confObj;
						pluginList.Add( (PlugIn)Activator.CreateInstance(Ty, paramsPlug));
					}                   
				}
			}
			catch (Exception Ex) {
				System.Diagnostics.Debug.WriteLine(Ex.Message);
			}
		}
		
		public bool msgCommand(ref IMessage msg, out String newMsg)
		{
			bool command = false;
			newMsg = msg.Body;
			try {
				for(int Ndx = 0; Ndx <= (pluginList.Count - 1); Ndx++)
				{
					((PlugIn) pluginList[Ndx]).msgCommand(ref msg, ref newMsg, out command);
					if (command)
					{
						return true;
					}
				}
				for(int Ndx = 0; Ndx <= (pluginList.Count - 1); Ndx++)
				{
					((PlugIn) pluginList[Ndx]).msgCommand(ref msg, ref newMsg, out command);
					msg.Body =  newMsg;
				}
			} catch(Exception ex) {
				_logger.LogMessage(ex.Message, LogLevel.Error);
			}
			return false;
		}
		
		public void Stop() {
			for(int Ndx = 0; Ndx <= (pluginList.Count - 1); Ndx++)
			{
				if (((PlugIn) pluginList[Ndx]).IsThread()) {
					((PlugIn) pluginList[Ndx]).StopThread();
				}
			}
		}
		
		public string Help(bool isAdmin) {
			string tmp = "";
			try {
				for(int Ndx = 0; Ndx <= (pluginList.Count - 1); Ndx++)
				{
					string help = ((PlugIn) pluginList[Ndx]).Help(isAdmin);
					if (help.Trim() != "")
					{
						tmp += help; // + '\n';
					}
				}
			} catch(Exception ex) {
				_logger.LogMessage(ex.Message, LogLevel.Error);
			}
			return tmp;
		}
		
		public List<PlugIn> PlugInsList {
			get {
				return pluginList;
			}
		}
	}
}
