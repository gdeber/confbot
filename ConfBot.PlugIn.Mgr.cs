/*
 * Creato da SharpDevelop.
 * Utente: Andre
 * Data: 14/12/2008
 * Ora: 11.38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Reflection;
using System.Threading;
using jabber;
using jabber.protocol.client;
using ConfBot.PlugIns;


//using PlugInList	= System.Collections.Generic.List<ConfBot.PlugIns.PlugIn>;

namespace ConfBot.PlugIns
{
	/// <summary>
	/// Description of ConfBot_PlugIn_Mgr.
	/// </summary>
	public class PlugInMgr
	{
		Conference	confObj;
		static System.Collections.Generic.List<PlugIn>	pluginList = new  System.Collections.Generic.List<PlugIn>();
		
		public PlugInMgr(Conference confObj, string dirPlugIns)
		{
			this.confObj = confObj;
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
				confObj.LogMessageToFile("No plugin directory");
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
						paramsPlug[0]	= confObj;
						pluginList.Add( (PlugIn)Activator.CreateInstance(Ty, paramsPlug));
					}                   
				}
			}
			catch (Exception Ex) {
				System.Diagnostics.Debug.WriteLine(Ex.Message);
			}
		}
		
		public bool msgCommand(ref Message msg, out String newMsg)
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
				confObj.LogMessageToFile(ex.Message);
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
		
		public string Help() {
			string tmp = "";
			try {
				for(int Ndx = 0; Ndx <= (pluginList.Count - 1); Ndx++)
				{
					string help = ((PlugIn) pluginList[Ndx]).Help();
					if (help.Trim() != "")
					{
						tmp += help + '\n';
					}
				}
			} catch(Exception ex) {
				confObj.LogMessageToFile(ex.Message);
			}
			return tmp;
		}
	}
}
