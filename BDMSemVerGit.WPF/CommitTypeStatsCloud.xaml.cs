using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BDMSemVerGit.WPF
{
	/// <summary>
	/// Interaction logic for CommitTypeStatsCloud.xaml
	/// </summary>
	public partial class CommitTypeStatsCloud : UserControl
	{
		public Dictionary<String, Int32> CommitStats { get; set; }

		public CommitTypeStatsCloud()
		{
			this.InitializeComponent();
		}

		private void UserControl_Loaded(Object sender, RoutedEventArgs e)
		{
			if (
				this.CommitStats != null
				&& this.CommitStats.Count > 0
			)
				this.SetCommitStats();
		}

		public void SetCommitStats(Dictionary<String, Int32> commitStats)
		{
			this.CommitStats = commitStats;
			this.SetCommitStats();
		}

		public void SetCommitStats()
		{
			this.Visibility = Visibility.Visible;
			foreach (String key in this.CommitStats.Keys)
			{
				TextBlock textBlock = key.ToLower() switch
				{
					"breaks" or "breakingchanges" or "breakingchange" or "break" => this.txtBreaks,
					"features" or "feature" or "feat" => this.txtFeatures,
					"fixes" or "fix" => this.txtBugFixes,
					"documentation" or "docs" => this.txtDocumentation,
					"styles" or "style" => this.txtStyles,
					"coderefactoring" or "refactor" => this.txtCodeRefactoring,
					"performanceimprovement" or "performanceimprovements" or "perf" => this.txtPerformanceImprovements,
					"tests" or "test" => this.txtTests,
					"builds" or "build" => this.txtBuilds,
					"continuousintegration" or "continuousintegrations" or "ci" => this.txtContinuousIntegrations,
					"chores" or "chore" => this.txtChores,
					"reverts" or "revert" => this.txtReverts,
					"invalidtypes" or "invalidtype" or "invalid" or "it" => this.txtInvalidType,
					"nonconventionalcommits" or "nonconventionalcommit" or "ncc" => this.txtNonConventionalCommit,
					_ => null,
				};
				if (textBlock != null)
					textBlock.Text = (textBlock.Tag as String).Replace("{count}", this.CommitStats[key].ToString());
			}
		}
	}
}
