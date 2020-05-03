using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace SkillReplay
{
	[Serializable]
	public class SkillReplayConfig
	{
		private const string ConfigXML = "SkillReplay.config.xml";

		private bool isDurty = false;

		private string _Language;
		public string Language
		{
			get { return _Language; }
			set
			{
				if( Language != value )
				{
					_Language = value;
					isDurty = true;
				}
			}
		}


		private string _FFLogsURL;
		public string FFLogsURL
		{
			get { return _FFLogsURL; }
			set
			{
				if( _FFLogsURL != value )
				{
					_FFLogsURL = value;
					isDurty = true;
				}
			}
		}

		private int _Port = 10601;
		public int Port
		{
			get { return _Port; }
			set
			{
				if( _Port != value )
				{
					_Port = value;
					isDurty = true;
				}
			}
		}

		private int _AutoReplay = 1;
		public int AutoReplay
		{
			get { return _AutoReplay; }
			set
			{
				if( _AutoReplay != value )
				{
					_AutoReplay = value;
					isDurty = true;
				}
			}
		}

		private int _AutoStop = 0;
		public int AutoStop
		{
			get { return _AutoStop; }
			set
			{
				if( _AutoStop != value )
				{
					_AutoStop = value;
					isDurty = true;
				}
			}
		}

		private int _AutoStopTime = 10;
		public int AutoStopTime
		{
			get { return _AutoStopTime; }
			set
			{
				if( _AutoStopTime != value )
				{
					_AutoStopTime = value;
					isDurty = true;
				}
			}
		}

		[NonSerialized]
		public Dictionary<string, string> LanguageList = new Dictionary<string, string>()
		{
			//{ "ja-JP","日本語" },
			//{ "en-US","英語" }
		};

		private static SkillReplayConfig instance;

		public static SkillReplayConfig Instance
		{
			get
			{
				if( instance == null )
				{
					Load();
				}
				return instance;
			}
		}

		SkillReplayConfig()
		{
			var asm = Assembly.GetExecutingAssembly();
			var name = Assembly.GetExecutingAssembly().GetName().Name;
			using( var stream = asm.GetManifestResourceStream($"{name}.g.resources") )
			{
				if( stream != null )
				{
					using( var rr = new ResourceReader(stream) )
					{
						var dict = rr.GetEnumerator();
						while( dict.MoveNext() )
						{
							var m = Regex.Match((string)dict.Key, "resources/strings\\.(.*)\\.xaml");
							if( m.Success )
							{
								var culturee = m.Groups[1].Value.ToLower();
								CultureInfo info = new CultureInfo(culturee);
								LanguageList.Add(culturee, info.NativeName);
							}
						}
					}
				}
			}

		}

		public void Save()
		{
			if( isDurty )
			{
				string path = GetConfigPath();
				using( var stream = new FileStream(path, FileMode.Create, FileAccess.Write) )
				{
					try
					{
						XmlSerializer serializer = new XmlSerializer(typeof(SkillReplayConfig));
						serializer.Serialize(stream, instance);
					}
					catch( Exception )
					{
					}
				}
				isDurty = false;
			}
		}

		public static void Load()
		{
			string path = GetConfigPath();
			if( File.Exists(path) )
			{
				try
				{
					XmlDocument doc = new XmlDocument();
					doc.PreserveWhitespace = true;
					doc.Load(path);
					using( var stream = new XmlNodeReader(doc.DocumentElement) )
					{
						try
						{
							XmlSerializer serializer = new XmlSerializer(typeof(SkillReplayConfig));
							instance = (SkillReplayConfig)serializer.Deserialize(stream);
							instance.isDurty = false;
						}
						catch( Exception )
						{
						}
					}
				}
				catch( Exception )
				{
				}
			}
			if( instance == null )
			{
				var culture = CultureInfo.CurrentCulture.Name.ToLower();
				instance = new SkillReplayConfig();
				if( instance.LanguageList.ContainsKey(culture) )
				{
					instance.Language = culture;
				}
				else
				{
					instance.Language = "en-us";
				}
			}
		}

		private static string GetConfigPath()
		{
			var path = System.IO.Path.Combine(
				ActGlobals.oFormActMain.AppDataFolder.FullName,
				"Config",
				ConfigXML);
			return path;
		}

	}
}
