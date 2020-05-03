using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Controls;

namespace SkillReplay
{
	/// <summary>
	/// SkillReplayControl.xaml の相互作用ロジック
	/// </summary>
	public partial class SkillReplayControl : UserControl
	{
		public SkillReplayControl()
		{
			InitializeComponent();
			DataContext = new SkillReplayControlViewModel();
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if( e.AddedItems != null )
			{
				var lang = ((ComboBox)e.Source).SelectedValue;
				var name = Assembly.GetExecutingAssembly().GetName().Name;

				ResourceDictionary dic = new ResourceDictionary();
				var path = $"pack://application:,,,/{name};component/Resources/strings.{lang}.xaml";
				dic.Source = new Uri(path, UriKind.RelativeOrAbsolute);

				this.Resources.MergedDictionaries[0] = dic;

			}
		}
	}
}
