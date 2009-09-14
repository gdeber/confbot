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
			temp = temp.Replace("à", "&agrave;");
			temp = temp.Replace("è", "&egrave;");
			temp = temp.Replace("é", "&eacute;");
			temp = temp.Replace("ì", "&igrave;");
			temp = temp.Replace("ò", "&ograve;");
			temp = temp.Replace("ù", "&ugrave;");
			temp = temp.Replace("à", "&Agrave;");
			temp = temp.Replace("È", "&Egrave;");
			temp = temp.Replace("é", "&Eacute;");
			temp = temp.Replace("ì", "&Igrave;");
			temp = temp.Replace("ò", "&Ograve;");
			temp = temp.Replace("ù", "&Ugrave;");
			temp = temp.Replace("&", "&amp;");
			temp = temp.Replace("<", "&lt;");
			temp = temp.Replace(">", "&gt;");
			temp = temp.Replace("©", "&copy;");
			temp = temp.Replace("°", "&deg;");
			temp = temp.Replace("¢", "&cent;");
			temp = temp.Replace("÷", "&divide;");
			temp = temp.Replace("×", "&times;");
			temp = temp.Replace("¿", "&iquest;");
			return temp;
		}		
		
		public static string ReplaceHTMLCode(string source) {
			string temp = source;
			temp = temp.Replace("&agrave;", "à");
			temp = temp.Replace("&egrave;", "è");
			temp = temp.Replace("&eacute;", "é");
			temp = temp.Replace("&igrave;", "ì");
			temp = temp.Replace("&ograve;", "ò");
			temp = temp.Replace("&ugrave;", "ù");
			temp = temp.Replace("&Agrave;", "à");
			temp = temp.Replace("&Egrave;", "È");
			temp = temp.Replace("&Eacute;", "é");
			temp = temp.Replace("&Igrave;", "ì");
			temp = temp.Replace("&Ograve;", "ò");
			temp = temp.Replace("&Ugrave;", "ù");
			temp = temp.Replace("&amp;", "&");
			temp = temp.Replace("&lt;", "<");
			temp = temp.Replace("&gt;", ">");
			temp = temp.Replace("&copy;", "©");
			temp = temp.Replace("&deg;", "°");
			temp = temp.Replace("&cent;", "¢");
			temp = temp.Replace("&divide;", "÷");
			temp = temp.Replace("&times;", "×");
			temp = temp.Replace("&iquest;", "¿");
			return temp;
		}		
	}
}
