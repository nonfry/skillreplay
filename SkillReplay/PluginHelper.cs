using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ACT_Plugin
{

	public interface Logger
	{
		void Log(string str);
	}

	public class Combatant
	{
		public Combatant(dynamic obj)
		{
			ID = obj.ID;
			Job = obj.Job;
			Name = obj.Name;
			PosX = obj.PosX;
			PosY = obj.PosY;
			PosZ = obj.PosZ;
			Heading = obj.Heading;
		}

		public uint ID { get; set; }

		public int Job { get; set; }
		public string Name { get; set; }
		public float PosX { get; set; }
		public float PosY { get; set; }
		public float PosZ { get; set; }
		public float Heading { get; set; }
	}

	public class PluginHelper
	{
		public bool IsInit { get; private set; } = false;
		private Logger log;
		public PluginHelper(Logger log)
		{
			this.log = log;
		}

		public async void Init(Action cb=null)
		{
			log.Log("check ffxiv plugin");
			dynamic plugin = null;
			do
			{
				await Task.Delay(5000);
				plugin = GetFFXIVPlugin();
			} while (plugin == null);
			IsInit = true;
			log.Log("ffxiv plugin ok : " + GetPlayerName());
			cb?.Invoke();
		}

		private dynamic GetFFXIVPlugin()
		{
			dynamic plugin = (from x in ActGlobals.oFormActMain.ActPlugins
							  where
							  x.pluginFile.Name.ToUpper().Contains("FFXIV_ACT_Plugin".ToUpper()) &&
							  x.lblPluginStatus.Text.ToUpper().Contains("FFXIV Plugin Started.".ToUpper())
							  select
							  x.pluginObj).FirstOrDefault();
			return plugin;
		}

		private DateTime CombatantsUpdateTime = new DateTime(2000, 1, 1);
		private List<Combatant> LastCombatants = new List<Combatant>();
		public List<Combatant> Combatants
		{
			get
			{
				UpdateCombatants();
				return LastCombatants;
			}
		}

		private void UpdateCombatants()
		{
			var dt = DateTime.Now - CombatantsUpdateTime;
			if (dt.TotalMilliseconds > 1000)
			{
				LastCombatants.Clear();
				dynamic plugin = GetFFXIVPlugin();
				dynamic rep = plugin.DataRepository;
				IEnumerable<dynamic> list = rep.GetCombatantList() as IEnumerable<dynamic>;
				foreach (var c in list)
				{
					LastCombatants.Add(new Combatant(c));
				}
			}
		}

		public Combatant GetPlayer()
		{
			return Combatants.FirstOrDefault();
		}

		public string GetPlayerName()
		{
			if(Combatants.Count > 0) { 
				return Combatants.First().Name;
			}
			return null;
		}
	}
}
