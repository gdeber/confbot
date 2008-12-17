/*
 * Creato da SharpDevelop.
 * Utente: Andre
 * Data: 12/12/2008
 * Ora: 19.40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Globalization;
using jabber.client;
using jabber.protocol.client;
using ConfBot;

namespace ConfBot.PlugIns
{
	/// <summary>
	/// Description of ConfBot_PlugIn.
	/// </summary>
	public abstract class PlugIn
	{
		protected Conference confObj;
		
		public PlugIn (Conference confObj) {
			this.confObj = confObj;
		}
		
		public abstract bool msgCommand(ref Message msg, ref String newMsg, out bool command);
		public abstract string Help();
		
		public virtual bool IsThread() {
			return false;
		}
		
		public virtual void StartThread() {
			return;
		}
		
		public virtual void StopThread() {
			return;
		}
	}
	
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
	public sealed class PlugInAttribute : Attribute
	{
		//
	}
}
