using ACT_Plugin;
using SkillReplay;
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

	}
}
