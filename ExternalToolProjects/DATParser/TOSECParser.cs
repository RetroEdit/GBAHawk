﻿#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows.Forms;

namespace BizHawk.DATTool
{
	public class TOSECParser : DATParser
	{
		/// <summary>
		/// Required to generate a GameDB file
		/// </summary>
		public override SystemType SysType { get; set; }

		private List<XDocument> xmls = new List<XDocument>();

		public TOSECParser(SystemType type)
		{
			SysType = type;
		}

		/// <summary>
		/// Parses multiple DAT files and returns a single GamesDB format csv string
		/// </summary>
		public override string ParseDAT(string[] filePath)
		{
			foreach (var s in filePath)
			{
				try
				{
					xmls.Add(XDocument.Load(s));
				}
				catch
				{
					var res = MessageBox.Show("Could not parse document as valid XML:\n\n" + s + "\n\nDo you wish to continue any other processing?", "Parsing Error", MessageBoxButtons.YesNo);
					if (res != DialogResult.Yes)
						return "";
				}				
			}

			int startIndex = 0;

			// actual tosec parsing
			foreach (var obj in xmls)
			{
				startIndex = Data.Count > 0 ? Data.Count - 1 : 0;
				// get header info
				var header = obj.Root.Descendants("header").First();
				var category = header.Element("category").Value;
				var name = header.Element("name").Value;
				var version = header.Element("version").Value;
				var description = header.Element("description").Value;

				// start comment block
				List<string> comments = new List<string>
				{
					$"Type:\t{category}",
					$"Source:\t{description}",
					$"FileGen:\t{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} (UTC)",
				};

				AddCommentBlock(comments.ToArray());

				// process each entry
				var query = obj.Root.Descendants("game");
				foreach (var g in query)
				{
					GameDB item = new GameDB();
					item.Name = g.Value;
					item.SHA1 = g.Elements("rom").First().Attribute("sha1").Value.ToUpper();
					item.MD5 = g.Elements("rom").First().Attribute("md5").Value.ToUpper();
					item.System = GameDB.GetSystemCode(SysType);

					ParseTOSECFlags(item);

					Data.Add(item);
				}

				// add this file's data to the stringbuilder
				// first we will sort into various ROMSTATUS groups
				var working = Data.Skip(startIndex).ToList();

				var baddump = working.Where(st => st.Status == "B").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Bad Dumps");
				AppendCSVData(baddump);

				var hack = working.Where(st => st.Status == "H").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Hacks");
				AppendCSVData(hack);

				var over = working.Where(st => st.Status == "O").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Over Dumps");
				AppendCSVData(over);

				var trans = working.Where(st => st.Status == "T").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Translated");
				AppendCSVData(trans);

				var pd = working.Where(st => st.Status == "D").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Home Brew");
				AppendCSVData(pd);

				var good = working.Where(st => st.Status == "" || st.Status == null).OrderBy(na => na.Name).ToList();
				AddCommentBlock("Believed Good");
				AppendCSVData(good);
			}

			string result = sb.ToString();
			return sb.ToString();
		}

		/// <summary>
		/// Parses all the weird TOSEC flags within the game field
		/// Detailed info here: https://www.tosecdev.org/tosec-naming-convention
		/// Guts of this has been reused from here: https://github.com/Asnivor/MedLaunch/blob/master/MedLaunch/_Debug/DATDB/Platforms/TOSEC/StringConverterToSec.cs
		/// </summary>
		private void ParseTOSECFlags(GameDB g)
		{
			string nameString = g.Name;

			// remove uninteresting options
			string a = RemoveUnneededOptions(nameString);

			// process data contained in ()
			string[] d = a.ToString().Split('(', ')');

			if (d.Length > 0)
			{
				// name field
			}

			if (d.Length > 1)
			{
				if (d[1].Length > 3)
				{
					// year field
				}
			}

			if (d.Length > 3)
			{
				// publisher field
			}

			// public domain
			if (nameString.Contains("(PD)"))
			{
				g.Status = "D";
			}

			if (d.Length > 4)
			{
				// parse all other () fields
				// because these are not mandatory this can be a confusing process
				for (int i = 4; i < d.Length; i++)
				{
					string f = d[i];

					// system field
					if (f == "Aladdin Deck Enhancer" ||
						f == "PlayChoice-10" ||
						f == "VS DualSystem" ||
						f == "VS UniSystem")
					{
						// ignore for now (not currently implemented)
						continue;
					}

					// country flag(s)
					if (IsCountryFlag(f) == true)
					{
						g.Region = f;
						continue;
					}

					// language - if present add to notes
					if (IsLanguageFlag(f) == true)
					{
						g.Notes = f;
						continue;
					}

					// check copyright status (not currently implemented)
					if (IsCopyrightStatus(f) == true)
					{
						continue;
					}

					// check development status (not currently implemented)
					if (IsDevelopmenttStatus(f) == true)
					{
						continue;
					}

					

					// Media Type - ignore for now
					// Media Label - ignore for now
				}

				// process dump info flags and other info contained in []
				if (nameString.Contains("[") && nameString.Contains("]"))
				{
					var e = nameString.Split('[', ']')
						.Skip(1) // remove first entry (this is the bit before the [] entries start)
						.Where(s => !string.IsNullOrWhiteSpace(s)) // remove empty entries
						.Distinct()
						.ToList();

					if (e.Count > 0)
					{
						// bizhawk currently only has a few different RomStatus values (not as many as TOSEC anyway)
						// Parsing priority will be:
						//	RomStatus.BadDump
						//	RomStatus.Hack
						//	RomStatus.Overdump
						//	RomStatus.GoodDump
						//	RomStatus.TranslatedRom
						//	everything else
						// all tosec cr, h, t etc.. will fall under RomStatus.Hack

						if (e.Where(str => 
						// bad dump
						str == "b" || str.StartsWith("b ") ||
						// virus
						str == "v" || str.StartsWith("v ") ||
						// under dump
						str == "u" || str.StartsWith("u ")).ToList().Count > 0)
						{
							// RomStatus.BadDump
							g.Status = "B";
						}							
						else if (e.Where(str => 
						// cracked
						str == "cr" || str.StartsWith("cr ") ||
						// fixed
						str == "f" || str.StartsWith("f ") ||
						// hack
						str == "h" || str.StartsWith("h ") ||
						// modified
						str == "m" || str.StartsWith("m ") ||
						// pirated
						str == "p" || str.StartsWith("p ") ||
						// trained
						str == "t" || str.StartsWith("t ")
						).ToList().Count > 0)
						{
							// RomStatus.Hack
							g.Status = "H";
						}
						else if (e.Where(str =>
						// over dump
						str == "o" || str.StartsWith("o ")).ToList().Count > 0)
						{
							// RomStatus.Overdump
							g.Status = "O";
						}
						else if (e.Where(str =>
						// known verified dump
						str == "!").ToList().Count > 0)
						{
							// RomStatus.GoodDump
							g.Status = "";
						}
						else if (e.Where(str =>
						// translated
						str == "tr" || str.StartsWith("tr ")).ToList().Count > 0)
						{
							// RomStatus.TranslatedRom
							g.Status = "T";
						}
					}
				}
			}
		}

		public static bool IsDevelopmenttStatus(string s)
		{
			List<string> DS = new List<string>
			{
				"alpha", "beta", "preview", "pre-release", "proto"
			};

			bool b = DS.Any(s.Contains);
			return b;
		}

		public static bool IsCopyrightStatus(string s)
		{
			List<string> CS = new List<string>
			{
				"CW", "CW-R", "FW", "GW", "GW-R", "LW", "PD", "SW", "SW-R"
			};

			bool b = CS.Any(s.Contains);
			return b;
		}

		public static bool IsLanguageFlag(string s)
		{
			List<string> LC = new List<string>
			{
				"ar", "bg", "bs", "cs", "cy", "da", "de", "el", "en", "eo", "es", "et", "fa", "fi", "fr", "ga",
				"gu", "he", "hi", "hr", "hu", "is", "it", "ja", "ko", "lt", "lv", "ms", "nl", "no", "pl", "pt",
				"ro", "ru", "sk", "sl", "sq", "sr", "sv", "th", "tr", "ur", "vi", "yi", "zh", "M1", "M2", "M3",
				"M4", "M5", "M6", "M7", "M8", "M9"
			};

			bool b = false;

			if (!s.Contains("[") && !s.Contains("]"))
			{
				foreach (var x in LC)
				{
					if (s == x || s.StartsWith(x) || s.EndsWith(x))
					{
						b = true;
						break;
					}
				}

				//b = LC.Any(s.Contains);
			}

			return b;
		}

		public static bool IsCountryFlag(string s)
		{
			List<string> CC = new List<string>
			{
				"AE", "AL", "AS", "AT", "AU", "BA", "BE", "BG", "BR", "CA", "CH", "CL", "CN", "CS", "CY", "CZ",
				"DE", "DK", "EE", "EG", "EU", "ES", "FI", "FR", "GB", "GR", "HK", "HR", "HU", "ID", "IE", "IL",
				"IN", "IR", "IS", "IT", "JO", "JP", "KR", "LT", "LU", "LV", "MN", "MX", "MY", "NL", "NO", "NP",
				"NZ", "OM", "PE", "PH", "PL", "PT", "QA", "RO", "RU", "SE", "SG", "SI", "SK", "TH", "TR", "TW",
				"US", "VN", "YU", "ZA"
			};

			bool b = false;

			if (!s.Contains("[") && !s.Contains("]"))
			{
				foreach (var x in CC)
				{
					if (s == x || s.StartsWith(x) || s.EndsWith(x))
					{
						b = true;
						break;
					}
				}

				//b = CC.Any(s.Contains);
			}

			return b;
		}

		public static string RemoveUnneededOptions(string nameString)
		{
			// Remove unneeded entries
			string n = nameString
				.Replace(" (demo) ", " ")
				.Replace(" (demo-kiosk) ", " ")
				.Replace(" (demo-playable) ", " ")
				.Replace(" (demo-rolling) ", " ")
				.Replace(" (demo-slideshow) ", " ");

			return n;
		}
	}
}
