using ACT_Plugin;
using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xml;
using System.Xml.Serialization;

namespace SkillReplay
{

	public partial class SkillReplayMain : IActPluginV1
	{
		PluginHelper helper;
		Label statusLabel;
		SkillReplayControlViewModel vm;

		public void Log(string str)
		{
			if( vm != null )
			{
				vm.Log(str);
			}
		}

		public SkillReplayMain()
		{
		}

		public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
		{
			statusLabel = pluginStatusText;
			statusLabel.Text = "";

			SetPluginForm(pluginScreenSpace);

			helper = new PluginHelper(vm);
			helper.Init(() =>
			{
				ActGlobals.oFormActMain.ValidateLists();
				ActGlobals.oFormActMain.OnLogLineRead += new LogLineEventDelegate(OnLogLineRead);
				statusLabel.Text += "Plugin Inited.";
				Log("plugin init ok");
			});
		}

		void SetPluginForm(TabPage pluginScreenSpace)
		{
			var elementHost = new ElementHost();
			SkillReplayControl rr = new SkillReplayControl();
			elementHost.Child = rr;
			elementHost.Dock = DockStyle.Fill;

			vm = (SkillReplayControlViewModel)rr.DataContext;

			pluginScreenSpace.Text = "SkillReplay";
			pluginScreenSpace.Controls.Add(elementHost);
		}


		public void DeInitPlugin()
		{
			ActGlobals.oFormActMain.OnLogLineRead -= OnLogLineRead;
			statusLabel.Text = "Plugin exited";
		}

		private void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
		{
			if( !isImport && vm.IsReady )
			{
				var line = logInfo.logLine.Substring(15);
				var sep = line.Split(new char[] { ':' });
				if( sep[0] == "15" )
				{
					var player = helper.GetPlayer();
					var id = player.ID.ToString("X").PadLeft(8, '0');
					if( sep[1] == id )
					{
						vm.CheckStart(sep[3]);
					}
				}
			}
		}

	}

}
