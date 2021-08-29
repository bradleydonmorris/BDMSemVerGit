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
	/// Interaction logic for RepoInfo.xaml
	/// </summary>
	public partial class RepoInfo : UserControl
	{
		public String RepoName { get; set; }
		public String RepoDirectory { get; set; }
		public String CurrentBranch { get; set; }
		public TabControl ParentTabControl { get; set; }

		public RepoInfo()
		{
			this.InitializeComponent();
		}

		private void btnOpen_Click(Object sender, RoutedEventArgs e)
		{
			if (this.ParentTabControl != null)
			{
				TabItem tabItem =
				  this.ParentTabControl.Items.Cast<TabItem>()
					.FirstOrDefault(item => item.Header.Equals(this.RepoName));
				this.ParentTabControl.SelectedItem = tabItem ?? this.ParentTabControl.Items[
						this.ParentTabControl.Items.Add(new TabItem()
						{
							Header = this.RepoName,
							Content = new Repo()
							{
								RepoName = this.RepoName,
								RepoDirectory = this.RepoDirectory
							}
						}
						)
					];
			}
		}

		private void UserControl_Loaded(Object sender, RoutedEventArgs e)
		{
			this.txtRepoName.Text = this.RepoName;
			this.txtRepoDirectory.Text = this.RepoDirectory;
			this.txtCurrentBranch.Text = $"Current Branch: {this.CurrentBranch}";
		}

		private void btnBumpVersion_Click(Object sender, RoutedEventArgs e)
		{
			_ = new BumpVersionWindow(this.RepoName, this.RepoDirectory).ShowDialog();
		}
	}
}
