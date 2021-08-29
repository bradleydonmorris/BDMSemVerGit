using BDMSemVerGit.Engine;
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
	/// Interaction logic for Repo.xaml
	/// </summary>
	public partial class Repo : UserControl
	{
		public String RepoName { get; set; }
		public String RepoDirectory { get; set; }


		public Repo()
		{
			this.InitializeComponent();
		}

		private void UserControl_Loaded(Object sender, RoutedEventArgs e)
		{
			if (this.Parent is TabItem)
			{
				TabItem tabItem = this.Parent as TabItem;
				tabItem.Header = this.RepoName;
				tabItem.ToolTip = this.RepoDirectory;
			}
			//msgbCommitMessage.RepoDirectory = this.RepoDirectory;
			//verbBumpVersion.RepoDirectory = this.RepoDirectory;
			//wizBumpVersion.RepoDirectory = this.RepoDirectory;
		}
	}
}
