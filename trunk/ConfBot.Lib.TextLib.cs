/*
 * Creato da SharpDevelop.
 * Utente: Andre
 * Data: 19/12/2008
 * Ora: 14.53
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

namespace ConfBot.Lib
{
	/// <summary>
	/// Description of ConfBot_Lib_Text.
	/// </summary>
	public class TextLib
	{
		public TextLib(){
		}
				
		public static string PlaceHTMLCode(string source) {
			string temp = source;
			temp = temp.Replace("�", "&agrave;");
			temp = temp.Replace("�", "&egrave;");
			temp = temp.Replace("�", "&eacute;");
			temp = temp.Replace("�", "&igrave;");
			temp = temp.Replace("�", "&ograve;");
			temp = temp.Replace("�", "&ugrave;");
			temp = temp.Replace("�", "&Agrave;");
			temp = temp.Replace("�", "&Egrave;");
			temp = temp.Replace("�", "&Eacute;");
			temp = temp.Replace("�", "&Igrave;");
			temp = temp.Replace("�", "&Ograve;");
			temp = temp.Replace("�", "&Ugrave;");
			temp = temp.Replace("&", "&amp;");
			temp = temp.Replace("<", "&lt;");
			temp = temp.Replace(">", "&gt;");
			temp = temp.Replace("�", "&copy;");
			temp = temp.Replace("�", "&deg;");
			temp = temp.Replace("�", "&cent;");
			temp = temp.Replace("�", "&divide;");
			temp = temp.Replace("�", "&times;");
			temp = temp.Replace("�", "&iquest;");
			return temp;
		}		
		
		public static string ReplaceHTMLCode(string source) {
			string temp = source;
			temp = temp.Replace("&agrave;", "�");
			temp = temp.Replace("&egrave;", "�");
			temp = temp.Replace("&eacute;", "�");
			temp = temp.Replace("&igrave;", "�");
			temp = temp.Replace("&ograve;", "�");
			temp = temp.Replace("&ugrave;", "�");
			temp = temp.Replace("&Agrave;", "�");
			temp = temp.Replace("&Egrave;", "�");
			temp = temp.Replace("&Eacute;", "�");
			temp = temp.Replace("&Igrave;", "�");
			temp = temp.Replace("&Ograve;", "�");
			temp = temp.Replace("&Ugrave;", "�");
			temp = temp.Replace("&amp;", "&");
			temp = temp.Replace("&lt;", "<");
			temp = temp.Replace("&gt;", ">");
			temp = temp.Replace("&copy;", "�");
			temp = temp.Replace("&deg;", "�");
			temp = temp.Replace("&cent;", "�");
			temp = temp.Replace("&divide;", "�");
			temp = temp.Replace("&times;", "�");
			temp = temp.Replace("&iquest;", "�");
			return temp;
		}		
	}
}
