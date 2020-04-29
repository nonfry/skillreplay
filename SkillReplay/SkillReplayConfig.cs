using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SkillReplay
{
	[Serializable]
	public class SkillReplayConfig
	{
		private const string SettingXML = "SkillReplay.config.xml";

		private bool isDurty = false;
		private string _FFLogsURL;

		public string FFLogsURL
		{
			get { return _FFLogsURL; }
			set {
				if(_FFLogsURL != value){
					_FFLogsURL = value;
					isDurty = true;
				}
			}
		}

		private int _Port=10601;

		public int Port
		{
			get { return _Port; }
			set { 
				if(_Port != value ){
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
				if (_AutoReplay != value)
				{
					_AutoReplay = value;
					isDurty = true;
				}
			}
		}
		


		[NonSerialized]
		private static SkillReplayConfig instance;

		public static SkillReplayConfig Instance {
			get
			{
				if (instance == null)
				{
					Load();
				}
				return instance;
			}
		 }

		public void Save()
		{
			if (isDurty)
			{
				string path = GetConfigPath();
				using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
				{
					try{
						XmlSerializer serializer = new XmlSerializer(typeof(SkillReplayConfig));
						serializer.Serialize(stream, instance);
					}catch (Exception){
					}
				}
				isDurty = false;
			}
		}

		public static void Load()
		{
			string path = GetConfigPath();
			if (File.Exists(path))
			{
				XmlDocument doc = new XmlDocument();
				doc.PreserveWhitespace = true;
				doc.Load(path);
				using (var stream = new XmlNodeReader(doc.DocumentElement))
				{
					try{
						XmlSerializer serializer = new XmlSerializer(typeof(SkillReplayConfig));
						instance = (SkillReplayConfig)serializer.Deserialize(stream);
						instance.isDurty = false;
					}catch(Exception){
					}
				}
			}
			if (instance == null)
			{
				instance = new SkillReplayConfig();
			}
		}

		private static string GetConfigPath()
		{
			var path = System.IO.Path.Combine(
				ActGlobals.oFormActMain.AppDataFolder.FullName,
				"Config",
				SettingXML);
			return path;
		}

	}
}
