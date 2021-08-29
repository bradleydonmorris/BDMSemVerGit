using System;
using System.Collections.Generic;
using System.IO;
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
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//private void MaximizeToSecondaryScreen()
		//{
		//	this.Left = SystemParameters.VirtualScreenLeft;
		//	this.Top = SystemParameters.VirtualScreenTop;
		//	this.Height = SystemParameters.VirtualScreenHeight;
		//	this.Width = SystemParameters.VirtualScreenWidth;
		//}

		public MainWindow()
		{
			this.InitializeComponent();
			
			//this.MaximizeToSecondaryScreen();
			_ = this.stkRepoList.Children.Add(new RepoInfo()
			{
				RepoName = "SupremeAwesomeTool",
				RepoDirectory = @"C:\Users\bradley.morris\source\repos\BDMTestingADO\SupremeAwesomeTool",
				ParentTabControl = this.tbcMain,
				CurrentBranch = Engine.Git.GetCurrentBranch(@"C:\Users\bradley.morris\source\repos\BDMTestingADO\SupremeAwesomeTool")
			});
			_ = this.stkRepoList.Children.Add(new RepoInfo()
			{
				RepoName = "AAONEnterprise",
				RepoDirectory = @"C:\Users\bradley.morris\source\repos\AAONEnterprise\AAONEnterprise",
				ParentTabControl = this.tbcMain,
				CurrentBranch = Engine.Git.GetCurrentBranch(@"C:\Users\bradley.morris\source\repos\AAONEnterprise\AAONEnterprise")
			});
			foreach (String directory in System.IO.Directory.EnumerateDirectories(@"C:\Users\bradley.morris\source\repos\bradleydonmorris"))
				if (BDMSemVerGit.Engine.Git.IsGitRepository(directory))
					_ = this.stkRepoList.Children.Add(new RepoInfo()
					{
						RepoName = System.IO.Path.GetFileName(directory),
						RepoDirectory = directory,
						ParentTabControl = this.tbcMain,
						CurrentBranch = Engine.Git.GetCurrentBranch(directory)
					});
			this.tbcMain.SelectedItem = tbcMain.Items[1];
			Application.Current.MainWindow.WindowState = WindowState.Maximized;
		}
	}
}
