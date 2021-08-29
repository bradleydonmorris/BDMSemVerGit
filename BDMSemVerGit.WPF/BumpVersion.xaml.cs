using BDMSemVerGit.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace BDMSemVerGit.WPF
{
	/// <summary>
	/// Interaction logic for BumpVersion.xaml
	/// </summary>
	public partial class BumpVersion : Window
	{
		private Int32 CurrentStep = 1;

		private readonly BackgroundWorker BackgroundWorker = new();

		private String CommitMessageFilePath;
		private Parser Parser;

		public String RepoName { get; set; }
		public String RepoDirectory { get; set; }

		public BumpVersion(String repoName, String repoDirectory)
		{
			this.RepoName = repoName;
			this.RepoDirectory = repoDirectory;
			this.Title = $"Bump Version: {this.RepoName}";
			this.InitializeComponent();
			this.Step1();
		}

		private void Window_Loaded(Object sender, RoutedEventArgs e)
		{
			this.Parser = new(this.RepoDirectory);
			this.Parser.StatusChange += this.ParserStatusChanged;
			this.CommitMessageFilePath = System.IO.Path.Combine(this.Parser.AppDirectory, "commitmessage.txt");
			this.Title = $"Bump Version: {this.RepoName}";
		}

		private void ParserStatusChanged(Object sender, ParserStatusEventArgs e)
		{
			this.txtLog.Dispatcher.BeginInvoke((Action)(() =>
			{
				txtLog.Text = $"{e}\n" + txtLog.Text;
			}
			));
		}

		private void EnableNavButtons()
		{
			Dispatcher.BeginInvoke((Action)(() => {
				btnPrevious.IsEnabled = true;
				btnNext.IsEnabled = true;
			}
			));
		}

		private void DisableNavButtons()
		{
			Dispatcher.BeginInvoke((Action)(() => {
				btnPrevious.IsEnabled = false;
				btnNext.IsEnabled = false;
			}
			));
		}

		private void btnPrevious_Click(Object sender, RoutedEventArgs e)
		{
			switch (this.CurrentStep)
			{
				case 2: this.Step1(); break;
				case 3: this.Step2(); break;
				case 4: this.Step3(); break;
				case 5: this.Step4(); break;
				case 6: this.Step5(); break;
				case 7: this.Step6(); break;
				case 8: this.Step7(); break;
				case 9: this.Step8(); break;
				case 10: this.Step9(); break;
				default: break;
			}
		}

		private void btnNext_Click(Object sender, RoutedEventArgs e)
		{
			switch (this.CurrentStep)
			{
				case 1: this.Step2(); break;
				case 2: this.Step3(); break;
				case 3: this.Step4(); break;
				case 4: this.Step5(); break;
				case 5: this.Step6(); break;
				case 6: this.Step7(); break;
				case 7: this.Step8(); break;
				case 8: this.Step9(); break;
				case 9: this.Step10(); break;
				default: break;
			}
		}

		private void SetActiveStepLabel()
		{
			this.txtStep1.FontWeight = FontWeights.Normal;
			this.txtStep2.FontWeight = FontWeights.Normal;
			this.txtStep3.FontWeight = FontWeights.Normal;
			this.txtStep4.FontWeight = FontWeights.Normal;
			this.txtStep5.FontWeight = FontWeights.Normal;
			this.txtStep6.FontWeight = FontWeights.Normal;
			this.txtStep7.FontWeight = FontWeights.Normal;
			this.txtStep8.FontWeight = FontWeights.Normal;
			this.txtStep9.FontWeight = FontWeights.Normal;
			this.txtStep10.FontWeight = FontWeights.Normal;

			switch (this.CurrentStep)
			{
				case 1: this.txtStep1.FontWeight = FontWeights.Bold; break;
				case 2: this.txtStep2.FontWeight = FontWeights.Bold; break;
				case 3: this.txtStep3.FontWeight = FontWeights.Bold; break;
				case 4: this.txtStep4.FontWeight = FontWeights.Bold; break;
				case 5: this.txtStep5.FontWeight = FontWeights.Bold; break;
				case 6: this.txtStep6.FontWeight = FontWeights.Bold; break;
				case 7: this.txtStep7.FontWeight = FontWeights.Bold; break;
				case 8: this.txtStep8.FontWeight = FontWeights.Bold; break;
				case 9: this.txtStep9.FontWeight = FontWeights.Bold; break;
				case 10: this.txtStep10.FontWeight = FontWeights.Bold; break;
			}
		}

		private void Step1()
		{
			this.txtStepDescription.Text = "First we will need to gather data from Git and store it in the database.";
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = "Click <Next> to begin gathering data.";

			this.CurrentStep = 1;
			this.SetActiveStepLabel();

			this.btnNext.IsEnabled = true;
			this.btnPrevious.IsEnabled = false;
		}

		#region Step 2
		private Boolean Step2Complete = false;
		private void Step2()
		{
			if (this.Step2Complete)
				this.Step2DispalyResults();
			else
			{
				this.txtStepDescription.Text = "Gathering data. Please stand by.";
				this.stepContent.Content = null;
				this.txtStepInstructions.Text = null;

				this.CurrentStep = 2;
				this.SetActiveStepLabel();

				this.btnNext.IsEnabled = true;
				this.btnPrevious.IsEnabled = true;

				this.BackgroundWorker.DoWork += this.Step2_DoWork;
				this.BackgroundWorker.RunWorkerCompleted += this.Step2_RunWorkerCompleted;
				this.BackgroundWorker.RunWorkerAsync();
			}
		}
		private void Step2DispalyResults()
		{
			this.txtStepDescription.Text = "Data has been gathered from Git.";
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = null;

			Commit commit = this.Parser.Data.GetNewestCommit();
			if (commit != null)
			{
				if (commit.IsConventional)
				{
					this.txtStepDescription.Text += $"\nNEWEST COMMIT:\nSHA: {commit.SHA}\n" +
						$"Date: {commit.Date.ToLocalTime():yyyy-MM-dd HH:mm:ssL}\n" +
						$"Type: {commit.ConventionalCommit.Type}\n" +
						$"Scope: {commit.ConventionalCommit.Scope}\n" +
						$"Summary: {commit.ConventionalCommit.Summary}";
					if (!String.IsNullOrWhiteSpace(commit.ConventionalCommit.Description))
						this.txtStepDescription.Text += $"\nDescription: {commit.ConventionalCommit.Description}";
					if (commit.ConventionalCommit.IsBreakingChange)
						this.txtStepDescription.Text += $"\nBreaking Change: {commit.ConventionalCommit.BreakingChange}";
					if (
						commit.ConventionalCommit.References != null
						&& commit.ConventionalCommit.References.Length > 0
					)
						this.txtStepDescription.Text += $"\nReferences: {String.Join(", ", commit.ConventionalCommit.References)}";
				}
				else
				{
					this.txtStepDescription.Text += $"\nSHA: {commit.SHA}\n" +
						$"Date: {commit.Date.ToLocalTime():yyyy-MM-dd HH:mm:ssL}\n" +
						$"Message:\n{commit}\n";
				}
			}
			else
				this.txtStepDescription.Text += "<None Found>";
			this.txtStepInstructions.Text = "To proceed, click <Next> to begin gathering versions.";
		}
		private void Step2_DoWork(Object sender, DoWorkEventArgs e)
		{
			this.DisableNavButtons();
			this.Parser.TransferGitDataToDatabase();
		}
		private void Step2_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
		{
			this.BackgroundWorker.WorkerReportsProgress = true;
			this.BackgroundWorker.DoWork -= this.Step2_DoWork;
			this.BackgroundWorker.RunWorkerCompleted -= this.Step2_RunWorkerCompleted;
			this.EnableNavButtons();
			this.Step2DispalyResults();
			this.Step2Complete = true;
		}
		#endregion Step 2

		#region Step 3
		private Boolean Step3Complete = false;
		private void Step3()
		{
			if (this.Step3Complete)
				this.Step3DispalyResults();
			else
			{
				this.txtStepDescription.Text = "Gathering versions. Please stand by.";
				this.stepContent.Content = null;
				this.txtStepInstructions.Text = null;

				this.CurrentStep = 3;
				this.SetActiveStepLabel();

				this.btnNext.IsEnabled = true;
				this.btnPrevious.IsEnabled = true;

				this.BackgroundWorker.DoWork += this.Step3_DoWork;
				this.BackgroundWorker.RunWorkerCompleted += this.Step3_RunWorkerCompleted;
				this.BackgroundWorker.RunWorkerAsync();
			}
		}
		private void Step3DispalyResults()
		{
			this.txtStepDescription.Text = "Versions have been gathered.";
			BDMSemVerGit.Engine.Version currentVersion = this.Parser.Data.GetMaxVersion();
			if (currentVersion != null)
			{
				this.txtStepDescription.Text += $"\nName: {currentVersion.Name}" +
					$"\nDate: {currentVersion.ReleaseDate.ToLocalTime():yyyy-MM-dd HH:mm:ssL}" +
					$"\nCommit Count: {currentVersion.Commits.Count}";
			}
			else
				this.txtStepDescription.Text = "\n<None Found>";
			BDMSemVerGit.Engine.Version nextVersion = this.Parser.NewVersion;
			if (nextVersion != null)
			{
				this.txtStepDescription.Text += $"\nName: {nextVersion.Name}" +
					$"\nDate: {nextVersion.ReleaseDate.ToLocalTime():yyyy-MM-dd HH:mm:ssL}" +
					$"\nCommit Count: {nextVersion.Commits.Count}";
			}
			else
				this.txtStepDescription.Text = "<None Found>";
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = "To proceed, click <Next> to begin gathering versions from within the porjects' files.";
		}
		private void Step3_DoWork(Object sender, DoWorkEventArgs e)
		{
			this.DisableNavButtons();
			this.Parser.GatherVersions();
		}
		private void Step3_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
		{
			this.BackgroundWorker.WorkerReportsProgress = true;
			this.BackgroundWorker.DoWork -= this.Step3_DoWork;
			this.BackgroundWorker.RunWorkerCompleted -= this.Step3_RunWorkerCompleted;
			this.EnableNavButtons();
			this.Step3DispalyResults();
			this.Step3Complete = true;
		}
		#endregion Step 3

		#region Step 4
		private Boolean Step4Complete = false;
		private void Step4()
		{
			if (this.Step4Complete)
				this.Step4DispalyResults();
			else
			{
				this.txtStepDescription.Text = "Gathering project file versions. Please stand by.";
				this.stepContent.Content = null;
				this.txtStepInstructions.Text = null;

				this.CurrentStep = 4;
				this.SetActiveStepLabel();

				this.btnNext.IsEnabled = true;
				this.btnPrevious.IsEnabled = true;

				this.BackgroundWorker.DoWork += this.Step4_DoWork;
				this.BackgroundWorker.RunWorkerCompleted += this.Step4_RunWorkerCompleted;
				this.BackgroundWorker.RunWorkerAsync();
			}
		}
		private void Step4DispalyResults()
		{
			this.txtStepDescription.Text = "Project File Versions have been gathered.";
			foreach (ProjectFileVersion projectFileVersion in this.Parser.FileVersions)
			{
				this.txtStepDescription.Text += $"\n{projectFileVersion.RelativePath}" +
					$"\n\tLocataion In File: {projectFileVersion.LocationInFile}" +
					$"\n\tCurrent Version: {projectFileVersion.CurrentVersion}" +
					$"\n\tNew Version: {projectFileVersion.NewVersion}\n";
			}
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = $"To proceed, click <Next> to begin setting the versions in the porjects' files to {this.Parser.NewVersion}.";
		}
		private void Step4_DoWork(Object sender, DoWorkEventArgs e)
		{
			this.DisableNavButtons();
			this.Parser.GatherProjectFileVersions();
		}
		private void Step4_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
		{
			this.BackgroundWorker.WorkerReportsProgress = true;
			this.BackgroundWorker.DoWork -= this.Step4_DoWork;
			this.BackgroundWorker.RunWorkerCompleted -= this.Step4_RunWorkerCompleted;
			this.EnableNavButtons();
			this.Step4DispalyResults();
			this.Step4Complete = true;
		}
		#endregion Step 4

		#region Step 5
		private Boolean Step5Complete = false;
		private void Step5()
		{
			if (this.Step5Complete)
				this.Step5DispalyResults();
			else
			{
				this.txtStepDescription.Text = "Setting project file versions. Please stand by.";
				this.stepContent.Content = null;
				this.txtStepInstructions.Text = null;

				this.CurrentStep = 5;
				this.SetActiveStepLabel();

				this.btnNext.IsEnabled = true;
				this.btnPrevious.IsEnabled = true;

				this.BackgroundWorker.DoWork += this.Step5_DoWork;
				this.BackgroundWorker.RunWorkerCompleted += this.Step5_RunWorkerCompleted;
				this.BackgroundWorker.RunWorkerAsync();
			}
		}
		private void Step5DispalyResults()
		{
			this.txtStepDescription.Text = "Project File Versions have been set.";
			foreach (ProjectFileVersion projectFileVersion in this.Parser.FileVersions)
			{
				this.txtStepDescription.Text += $"\n{projectFileVersion.RelativePath}" +
					$"\n\tLocataion In File: {projectFileVersion.LocationInFile}" +
					$"\n\tPrevious Version: {projectFileVersion.CurrentVersion}" +
					$"\n\tCurrent Version: {projectFileVersion.NewVersion}\n";
			}
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = "To proceed, click <Next> to begin building the CHANGELOG.";
		}
		private void Step5_DoWork(Object sender, DoWorkEventArgs e)
		{
			this.DisableNavButtons();
			foreach (ProjectFileVersion projectFileVersion in this.Parser.FileVersions)
				if (projectFileVersion.AlterVersion)
					FileVersions.SetVersion(projectFileVersion);
		}
		private void Step5_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
		{
			this.BackgroundWorker.WorkerReportsProgress = true;
			this.BackgroundWorker.DoWork -= this.Step5_DoWork;
			this.BackgroundWorker.RunWorkerCompleted -= this.Step5_RunWorkerCompleted;
			this.EnableNavButtons();
			this.Step5DispalyResults();
			this.Step5Complete = true;
		}
		#endregion Step 5

		#region Step 6
		private Boolean Step6Complete = false;
		private void Step6()
		{
			if (this.Step6Complete)
				this.Step6DispalyResults();
			else
			{
				this.txtStepDescription.Text = "Building CHANGELOG. Please stand by.";
				this.stepContent.Content = null;

				this.CurrentStep = 6;
				this.SetActiveStepLabel();

				this.btnNext.IsEnabled = true;
				this.btnPrevious.IsEnabled = true;

				this.BackgroundWorker.DoWork += this.Step6_DoWork;
				this.BackgroundWorker.RunWorkerCompleted += this.Step6_RunWorkerCompleted;
				this.BackgroundWorker.RunWorkerAsync();
			}
		}
		private void Step6DispalyResults()
		{
			this.txtStepDescription.Text = $"CHANGELOG files have been built." +
				$"Please review the following files for completeness." +
				$"\n{this.Parser.MDChangeLogPath}" +
				$"\n{this.Parser.HTMLChangeLogPath}";
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = "Click <Next> to proceed to building the committ message.";
		}
		private void Step6_DoWork(Object sender, DoWorkEventArgs e)
		{
			this.DisableNavButtons();
			foreach (ProjectFileVersion projectFileVersion in this.Parser.FileVersions)
				if (projectFileVersion.AlterVersion)
					FileVersions.SetVersion(projectFileVersion);
		}
		private void Step6_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
		{
			this.BackgroundWorker.WorkerReportsProgress = true;
			this.BackgroundWorker.DoWork -= this.Step6_DoWork;
			this.BackgroundWorker.RunWorkerCompleted -= this.Step6_RunWorkerCompleted;
			this.EnableNavButtons();
			this.Step6DispalyResults();
			this.Step6Complete = true;
		}
		#endregion Step 6

		#region Step 7
		private void Step7()
		{
			this.txtStepDescription.Text = "Please provide a commit message for the version commit.";
			this.stepContent.Content = new MessageBuilder()
			{
				ShowCopyButton = true,
				ShowSaveButton = true,
				ScopesFilePath = this.CommitMessageFilePath,
				CommitMessageFilePath = this.CommitMessageFilePath,
				DefaultCommitType = CommitType.changelog
			};
			this.txtStepInstructions.Text = "Click <Save> and then click <Next> to commit the CHNAGELOG.";

			this.CurrentStep = 7;
			this.SetActiveStepLabel();
		}
		#endregion Step 7

		#region Step 8
		private Boolean Step8Complete = false;
		private void Step8()
		{
			if (this.Step8Complete)
				this.Step8DispalyResults();
			else
			{
				this.txtStepDescription.Text = "Committing changes. Please stand by.";
				this.stepContent.Content = null;
				this.txtStepInstructions.Text = null;

				this.CurrentStep = 8;
				this.SetActiveStepLabel();

				this.btnNext.IsEnabled = true;
				this.btnPrevious.IsEnabled = true;

				this.BackgroundWorker.DoWork += this.Step8_DoWork;
				this.BackgroundWorker.RunWorkerCompleted += this.Step8_RunWorkerCompleted;
				this.BackgroundWorker.RunWorkerAsync();
			}
		}
		private void Step8DispalyResults()
		{
			this.txtStepDescription.Text = $"Changes have been committed\nCommit SHA:{this.Parser.NewVersionCommit.SHA}.";
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = $"Click <Next> to tag this commit with {this.Parser.NewVersion.SemanticVersion}.";
		}
		private void Step8_DoWork(Object sender, DoWorkEventArgs e)
		{
			this.DisableNavButtons();
			this.Parser.TagCommittedVersion();
		}
		private void Step8_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
		{
			this.BackgroundWorker.WorkerReportsProgress = true;
			this.BackgroundWorker.DoWork -= this.Step8_DoWork;
			this.BackgroundWorker.RunWorkerCompleted -= this.Step8_RunWorkerCompleted;
			this.EnableNavButtons();
			this.Step8DispalyResults();
			this.Step8Complete = true;
		}
		#endregion Step 8

		#region Step 9
		private void Step9()
		{
			this.txtStepDescription.Text = $"Commit {this.Parser.NewVersionCommit.SHA} has been tagged with {this.Parser.NewVersion.Tag} Please stand by.";
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = $"Click <Next> to push this new commit and tag upstream to \"{this.Parser.Git.GetCurrentBranch()}\" origin.";

			this.CurrentStep = 9;
			this.SetActiveStepLabel();
		}
		#endregion Step 9

		#region Step 10
		private Boolean Step10Complete = false;
		private void Step10()
		{
			if (this.Step10Complete)
				this.Step10DispalyResults();
			else
			{
				this.txtStepDescription.Text = "Pushing upstream. Please stand by.";
				this.stepContent.Content = null;
				this.txtStepInstructions.Text = null;

				this.CurrentStep = 10;
				this.SetActiveStepLabel();

				this.btnNext.IsEnabled = true;
				this.btnPrevious.IsEnabled = true;

				this.BackgroundWorker.DoWork += this.Step10_DoWork;
				this.BackgroundWorker.RunWorkerCompleted += this.Step10_RunWorkerCompleted;
				this.BackgroundWorker.RunWorkerAsync();
			}
		}
		private void Step10DispalyResults()
		{
			this.txtStepDescription.Text = $"Push to upstream complete. Confirm the following exist upstream using the appropriate UI.";
			this.stepContent.Content = null;
			this.txtStepInstructions.Text = null;
			this.btnPrevious.Visibility = Visibility.Hidden;
			this.btnNext.Visibility = Visibility.Hidden;
		}
		private void Step10_DoWork(Object sender, DoWorkEventArgs e)
		{
			this.DisableNavButtons();
			this.Parser.PushCommitAndTag();
		}
		private void Step10_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
		{
			this.BackgroundWorker.WorkerReportsProgress = true;
			this.BackgroundWorker.DoWork -= this.Step10_DoWork;
			this.BackgroundWorker.RunWorkerCompleted -= this.Step10_RunWorkerCompleted;
			this.Step10DispalyResults();
			this.Step10Complete = true;
		}
		#endregion Step 10
	}
}
