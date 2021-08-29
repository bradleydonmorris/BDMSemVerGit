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
	/// Interaction logic for BumpVersionWindow.xaml
	/// </summary>
	public partial class BumpVersionWindow : Window
	{
		private readonly List<Step> Steps = new();
		private Step CurrentStep;
		private Parser Parser;
		private MessageBuilder MessageBuilder;
		private ConventionalCommit ConventionalCommit;
		private BackgroundWorker BackgroundWorker = new();
		private String ScopesFilePath;
		private String CommitMessageFilePath;

		public String RepoName { get; private set; }
		public String RepoDirectory { get; private set; }

		public BumpVersionWindow()
		{
			this.RepoDirectory = Environment.CurrentDirectory;
			this.RepoName = System.IO.Path.GetFileName(this.RepoDirectory);
			this.Initialize();
		}
		public BumpVersionWindow(String repoDirectory)
		{
			this.RepoDirectory = repoDirectory;
			if (String.IsNullOrWhiteSpace(this.RepoDirectory))
				this.RepoDirectory = Environment.CurrentDirectory;
			this.RepoName = System.IO.Path.GetFileName(this.RepoDirectory);
			this.Initialize();
		}
		public BumpVersionWindow(String repoName, String repoDirectory)
		{
			this.RepoDirectory = repoDirectory;
			if (String.IsNullOrWhiteSpace(this.RepoDirectory))
				this.RepoDirectory = Environment.CurrentDirectory;
			this.RepoName = repoName;
			this.Initialize();
		}

		private void Window_Loaded(Object sender, RoutedEventArgs e)
		{
			foreach (Step step in this.Steps.OrderBy(s => s.Number))
				_ = this.stkpStepLabels.Children.Add(new TextBlock()
				{
					FontSize = 14,
					Margin = new(10, 10, 10, 0),
					Text = step.ToString()
				});
			this.Begin();
		}

		private void Initialize()
		{
			this.PreviewKeyDown += new KeyEventHandler(this.HandleEsc);
			this.InitializeComponent();
			if (!Git.IsGitRepository(this.RepoDirectory))
				throw new InvalidOperationException(
					$"Bump Version can only be ran on a Git directory." +
					$"\n{this.RepoDirectory} is not a Git directory."
				);
			this.Title = $"Bump Version: {this.RepoName}";
			this.txtRepoPath.Text = $"Repository Directory: {this.RepoDirectory}";
			this.Parser = new(this.RepoDirectory);
			this.Parser.StatusChange += this.Parser_StatusChanged;
			if (String.IsNullOrWhiteSpace(this.CommitMessageFilePath))
				this.CommitMessageFilePath = System.IO.Path.Combine(this.RepoDirectory, ".BDMSemVerGit\\commit-message.json");
			if (String.IsNullOrWhiteSpace(this.ScopesFilePath))
				this.ScopesFilePath = System.IO.Path.Combine(this.RepoDirectory, ".scopes");
			this.MessageBuilder = new MessageBuilder()
			{
				ShowCopyButton = false,
				ShowSaveButton = false,
				ScopesFilePath = this.ScopesFilePath,
				CommitMessageFilePath = this.CommitMessageFilePath,
				DefaultCommitType = CommitType.changelog,
				FileFormat = CommitMessageFileFormat.JSON
			};
			this.SetupSteps();
		}

		private void HandleEsc(Object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				this.Close();
		}


		private Step Step1_Introduction()
		{
			return new()
			{
				Number = 1,
				Name = "Introduction",
				BeforeWork = new()
				{
					Description = "",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = null,
				AfterWork = new()
				{
					Description = "First we will need to gather data from Git and store it in the database.",
					Content = null,
					Instructions = "Click <Next> to begin gathering data.",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				BeforeNextCallBack = null
			};
		}
		private Step Step2_GatherDataFromGit()
		{
			return new()
			{
				Number = 2,
				Name = "Gather Data from Git",
				BeforeWork = new()
				{
					Description = "Gathering data. Please stand by.",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = () => { this.Parser.TransferGitDataToDatabase(); },
				AfterWork = new()
				{
					Description = "Data has been gathered from Git.",
					Content = null,
					Instructions = "Click <Next> to begin gathering versions.",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = () => { this.Step2AfterWork(); }
				},
				BeforeNextCallBack = null
			};
		}
		private Step Step3_DetermineVersions()
		{
			return new()
			{
				Number = 3,
				Name = "Determine Versions",
				BeforeWork = new()
				{
					Description = "Gathering versions. Please stand by.",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = () => { this.Parser.GatherVersions(); },
				AfterWork = new()
				{
					Description = "Versions have been gathered.",
					Content = null,
					Instructions = "Click <Next> to begin gathering versions from within the projects' files.",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = () => { this.Step3AfterWork(); }
				},
				BeforeNextCallBack = null
			};
		}
		private Step Step4_GatherProjectFileVersions()
		{
			return new()
			{
				Number = 4,
				Name = "Gather Project File Versions",
				BeforeWork = new()
				{
					Description = "Gathering project file versions. Please stand by.",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = () => { this.Parser.GatherProjectFileVersions(); },
				AfterWork = new()
				{
					Description = "Project File Versions have been gathered.",
					Content = null,
					Instructions = "Click <Next> to begin setting the versions in the porjects' files to {@Version}",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = () => { this.Step4AfterWork(); }
				},
				BeforeNextCallBack = null
			};
		}
		private Step Step5_SetProjectFileVersions()
		{
			return new()
			{
				Number = 5,
				Name = "Set Project File Versions",
				BeforeWork = new()
				{
					Description = "Setting project file versions. Please stand by.",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = () =>
				{
					foreach (ProjectFileVersion projectFileVersion in this.Parser.FileVersions)
						if (projectFileVersion.AlterVersion)
							FileVersions.SetVersion(projectFileVersion);
				},
				AfterWork = new()
				{
					Description = "Project File Versions have been set.",
					Content = null,
					Instructions = "Click <Next> to begin building the CHANGELOG",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = () => { this.Step5AfterWork(); }
				},
				BeforeNextCallBack = null
			};
		}
		private Step Step6_BuildCHANGELOG()
		{
			return new()
			{
				Number = 6,
				Name = "Build CHANGELOG",
				BeforeWork = new()
				{
					Description = "Building CHANGELOG. Please stand by.",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = () => { this.Parser.BuildChangeLog(); },
				AfterWork = new()
				{
					Description = "CHANGELOG files have been built.",
					Content = null,
					Instructions = "Click <Next> to begin building the commit message.",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = () => { this.Step6AfterWork(); }
				},
				BeforeNextCallBack = null
			};
		}
		private Step Step7_BuildCommitMessage()
		{
			return new()
			{
				Number = 7,
				Name = "Build Commit Message",
				BeforeWork = new()
				{
					Description = "Please provide a commit message for the version commit.",
					Content = null,
					Instructions = "Click <Next> to save the commit message and commit the changes.",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null,
				},
				DoWorkCallBack = null,
				AfterWork = new()
				{
					Description = "Please provide a commit message for the version commit.",
					Content = this.MessageBuilder,
					Instructions = "Click <Next> to save the commit message and commit the changes.",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null,
				},
				BeforeNextCallBack = () =>
				{
					this.ConventionalCommit = this.MessageBuilder.ConventionalCommit;
				}
			};
		}
		private Step Step8_Commit()
		{
			return new()
			{
				Number = 8,
				Name = "Commit",
				BeforeWork = new()
				{
					Description = "Committing changes. Please stand by.",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = () => { this.Parser.CommitVersion(this.ConventionalCommit); },
				AfterWork = new()
				{
					Description = "Changes have been committed.",
					Content = null,
					Instructions = "Click <Next> to tag this commit.",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = () => { this.Step8AfterWork(); }
				},
				BeforeNextCallBack = null
			};
		}
		private Step Step9_TagCommit()
		{
			return new()
			{
				Number = 9,
				Name = "Tag Commit",
				BeforeWork = new()
				{
					Description = "Tagging commit. Please stand by.",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = () => { this.Parser.TagCommittedVersion(); },
				AfterWork = new()
				{
					Description = "Commit has been tagged.",
					Content = null,
					Instructions = "Click <Next> to push this commit and tag upstream.",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = () => { this.Step9AfterWork(); }
				},
				BeforeNextCallBack = null
			};
		}
		private Step Step10_PushCommitAndTag()
		{
			return new()
			{
				Number = 10,
				Name = "Push Commit and Tag",
				BeforeWork = new()
				{
					Description = "Pushing tag and commit upstream. Please stand by.",
					Content = null,
					Instructions = "",
					PreviousState = StepButtonState.VisibileNotEnabled,
					NextState = StepButtonState.VisibileNotEnabled,
					CloseState = StepButtonState.NotVisibileNotEnabled,
					CallBack = null
				},
				DoWorkCallBack = () => { this.Parser.PushCommitAndTag(); },
				AfterWork = new()
				{
					Description = "Push to upstream complete. Confirm the following exist upstream using the appropriate UI.",
					Content = null,
					Instructions = "Click <Close> to exit this wizard.",
					PreviousState = StepButtonState.NotVisibileNotEnabled,
					NextState = StepButtonState.NotVisibileNotEnabled,
					CloseState = StepButtonState.VisibileEnabled,
					CallBack = () => { this.Step10AfterWork(); }
				},
				BeforeNextCallBack = null
			};
		}

		private void SetupSteps()
		{
			this.Steps.Add(this.Step1_Introduction());
			this.Steps.Add(this.Step2_GatherDataFromGit());
			this.Steps.Add(this.Step3_DetermineVersions());
			this.Steps.Add(this.Step4_GatherProjectFileVersions());
			this.Steps.Add(this.Step5_SetProjectFileVersions());
			this.Steps.Add(this.Step6_BuildCHANGELOG());
			this.Steps.Add(this.Step7_BuildCommitMessage());
			this.Steps.Add(this.Step8_Commit());
			this.Steps.Add(this.Step9_TagCommit());
			this.Steps.Add(this.Step10_PushCommitAndTag());
		}

		private void Previous()
		{
			this.CurrentStep?.BeforePreviousCallBack?.Invoke();

			Int32 currentStepNumber = this.CurrentStep.Number;
			Int32 previousStepNumber = currentStepNumber - 1;
			if (previousStepNumber < 1)
				return;
			this.CurrentStep = this.Steps.First(s => s.Number == previousStepNumber);
			if (this.CurrentStep != null)
			{
				this.SetActiveStepLabel(this.CurrentStep.ToString());
				this.SetButtonsState(this.CurrentStep.BeforeWork);
				this.txtStepDescription.Text = this.CurrentStep.BeforeWork.Description;
				this.cntStepContent.Content = this.CurrentStep.BeforeWork.Content;
				this.txtStepInstructions.Text = this.CurrentStep.BeforeWork.Instructions;
				this.CurrentStep.BeforeWork.CallBack?.Invoke();

				if (this.CurrentStep.DoWorkCallBack != null)
				{
					this.BackgroundWorker = new();
					this.BackgroundWorker.DoWork += this.DoWork;
					this.BackgroundWorker.RunWorkerCompleted += this.RunWorkerCompleted;
					this.BackgroundWorker.RunWorkerAsync();
				}
			}
		}
		private void Next()
		{
			this.CurrentStep?.BeforeNextCallBack?.Invoke();

			Int32 currentStepNumber = this.CurrentStep.Number;
			Int32 nextStepNumber = currentStepNumber + 1;
			if (nextStepNumber > this.Steps.Count + 1)
				return;
			this.CurrentStep = this.Steps.First(s => s.Number == nextStepNumber);
			if (this.CurrentStep != null)
			{
				this.SetActiveStepLabel(this.CurrentStep.ToString());
				this.SetButtonsState(this.CurrentStep.BeforeWork);
				this.txtStepDescription.Text = this.CurrentStep.BeforeWork.Description;
				this.cntStepContent.Content = this.CurrentStep.BeforeWork.Content;
				this.txtStepInstructions.Text = this.CurrentStep.BeforeWork.Instructions;
				this.CurrentStep.BeforeWork.CallBack?.Invoke();

				if (this.CurrentStep.DoWorkCallBack != null)
				{
					this.BackgroundWorker = new();
					this.BackgroundWorker.DoWork += this.DoWork;
					this.BackgroundWorker.RunWorkerCompleted += this.RunWorkerCompleted;
					this.BackgroundWorker.RunWorkerAsync();
				}
				else
				{
					this.SetButtonsState(this.CurrentStep.AfterWork);
					this.txtStepDescription.Text = this.CurrentStep.AfterWork.Description;
					this.cntStepContent.Content = this.CurrentStep.AfterWork.Content;
					this.txtStepInstructions.Text = this.CurrentStep.AfterWork.Instructions;
					this.CurrentStep.AfterWork.CallBack?.Invoke();
				}
			}
		}
		private void Begin()
		{
			Int32 currentStepNumber = 1;
			if (currentStepNumber > this.Steps.Count)
				return;
			this.CurrentStep = this.Steps.First(s => s.Number == currentStepNumber);
			if (this.CurrentStep != null)
			{
				this.SetActiveStepLabel(this.CurrentStep.ToString());
				this.SetButtonsState(this.CurrentStep.BeforeWork);
				this.txtStepDescription.Text = this.CurrentStep.BeforeWork.Description;
				this.cntStepContent.Content = this.CurrentStep.BeforeWork.Content;
				this.txtStepInstructions.Text = this.CurrentStep.BeforeWork.Instructions;
				this.CurrentStep.BeforeWork.CallBack?.Invoke();

				if (this.CurrentStep.DoWorkCallBack != null)
				{
					this.BackgroundWorker = new();
					this.BackgroundWorker.DoWork += this.DoWork;
					this.BackgroundWorker.RunWorkerCompleted += this.RunWorkerCompleted;
					this.BackgroundWorker.RunWorkerAsync();
				}
				else
				{
					this.SetButtonsState(this.CurrentStep.AfterWork);
					this.txtStepDescription.Text = this.CurrentStep.AfterWork.Description;
					this.cntStepContent.Content = this.CurrentStep.AfterWork.Content;
					this.txtStepInstructions.Text = this.CurrentStep.AfterWork.Instructions;
					this.CurrentStep.AfterWork.CallBack?.Invoke();
				}
			}
		}

		private void SetActiveStepLabel(String text)
		{
			foreach (TextBlock childTextBlock in this.stkpStepLabels.Children.OfType<TextBlock>())
				childTextBlock.FontWeight = FontWeights.Normal;
			TextBlock textBlock = this.stkpStepLabels.Children
				.OfType<TextBlock>().ToList()
				.First(t => t.Text == text);
			textBlock.FontWeight = FontWeights.Bold;
		}
		private void SetButtonsState(StepStateData stepStateData)
		{
			if (stepStateData.PreviousState != null)
			{
				this.btnPrevious.IsEnabled = stepStateData.PreviousState.IsEnabled;
				this.btnPrevious.Visibility = stepStateData.PreviousState.IsVisibile
					? Visibility.Visible
					: Visibility.Collapsed;
			}
			if (stepStateData.NextState != null)
			{
				this.btnNext.IsEnabled = stepStateData.NextState.IsEnabled;
				this.btnNext.Visibility = stepStateData.NextState.IsVisibile
					? Visibility.Visible
					: Visibility.Collapsed;
			}
			if (stepStateData.CloseState != null)
			{
				this.btnClose.IsEnabled = stepStateData.CloseState.IsEnabled;
				this.btnClose.Visibility = stepStateData.CloseState.IsVisibile
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		private void DoWork(Object sender, DoWorkEventArgs e)
		{
			this.CurrentStep.DoWorkCallBack?.Invoke();
		}
		private void RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
		{
			this.BackgroundWorker.DoWork -= this.DoWork;
			this.BackgroundWorker.RunWorkerCompleted -= this.RunWorkerCompleted;

			this.SetButtonsState(this.CurrentStep.AfterWork);
			this.txtStepDescription.Text = this.CurrentStep.AfterWork.Description;
			this.cntStepContent.Content = this.CurrentStep.AfterWork.Content;
			this.txtStepInstructions.Text = this.CurrentStep.AfterWork.Instructions;
			this.CurrentStep.AfterWork.CallBack?.Invoke();
			this.CurrentStep.IsComplete = true;
		}
		private void Parser_StatusChanged(Object sender, ParserStatusEventArgs e)
		{
			_ = this.txtLog.Dispatcher.BeginInvoke((Action)(() =>
			  {
				  this.txtLog.Text = $"{e}\n" + this.txtLog.Text;
			  }
			));
		}
		private void btnPrevious_Click(Object sender, RoutedEventArgs e)
		{
			this.Previous();
		}
		private void btnNext_Click(Object sender, RoutedEventArgs e)
		{
			this.Next();
		}
		private void btnClose_Click(Object sender, RoutedEventArgs e)
		{
			
			//this.Close();
		}


		//Step1AfterWork not needed
		private void Step2AfterWork()
		{
			TextBlock textBlock = new();
			Commit commit = this.Parser.Data.GetNewestCommit();
			if (commit != null)
			{
				if (commit.IsConventional)
				{
					textBlock.Text += $"NEWEST COMMIT: {commit.SHA}" +
						$"\n\tDate: {commit.Date.ToLocalTime():yyyy-MM-dd HH:mm:ssL}" +
						$"\n\tType: {commit.ConventionalCommit.Type}" +
						$"\n\tScope: {commit.ConventionalCommit.Scope}" +
						$"\n\tSummary: {commit.ConventionalCommit.Summary}";
					if (!String.IsNullOrWhiteSpace(commit.ConventionalCommit.Description))
						textBlock.Text += $"\n\tDescription: {commit.ConventionalCommit.Description}";
					if (commit.ConventionalCommit.IsBreakingChange)
						textBlock.Text += $"\n\tBreaking Change: {commit.ConventionalCommit.BreakingChange}";
					if (
						commit.ConventionalCommit.References != null
						&& commit.ConventionalCommit.References.Length > 0
					)
						textBlock.Text += $"\n\tReferences: {String.Join(", ", commit.ConventionalCommit.References)}";
				}
				else
				{
					textBlock.Text += $"NEWEST COMMIT: {commit.SHA}" +
						$"\n\tDate: {commit.Date.ToLocalTime():yyyy-MM-dd HH:mm:ssL}" +
						$"\n\tMessage:\n{commit}\n";
				}
			}
			else
				textBlock.Text = "<None Found>";
			this.cntStepContent.Content = textBlock;
		}
		private void Step3AfterWork()
		{
			TextBlock textBlock = new();
			BDMSemVerGit.Engine.Version currentVersion = this.Parser.Data.GetMaxVersion();
			if (currentVersion != null)
			{
				textBlock.Text = $"CURRENT VERSION: {currentVersion.Name}" +
					$"\n\tDate: {currentVersion.ReleaseDate.ToLocalTime():yyyy-MM-dd HH:mm:ssL}" +
					$"\n\tCommit Count: {currentVersion.Commits.Count}";
			}
			else
				textBlock.Text = "CURRENT VERSION: <None Found>";
			String newVersionText;
			BDMSemVerGit.Engine.Version nextVersion = this.Parser.NewVersion;
			if (nextVersion != null)
			{
				newVersionText = $"\n\nNEXT VERSION: {nextVersion.Name}" +
					$"\n\tDate: {nextVersion.ReleaseDate.ToLocalTime():yyyy-MM-dd HH:mm:ssL}" +
					$"\n\tCommit Count: {nextVersion.Commits.Count}";
			}
			else
				newVersionText = "\n\nNEXT VERSION: <None Found>";
			textBlock.Text += newVersionText;
			this.cntStepContent.Content = textBlock;
			_ = this.stkpCurrent.Children.Add(
				new Border()
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new(2, 2, 2, 2),
					Margin = new(5, 5, 5, 5),
					Child = new TextBlock() { Text = newVersionText }
				}
			);
		}
		private void Step4AfterWork()
		{
			this.CurrentStep.AfterWork.Instructions = this.CurrentStep.AfterWork.Instructions
				.Replace("{@Version}", this.Parser.NewVersion.ToString());
			this.txtStepInstructions.Text = this.CurrentStep.AfterWork.Instructions;
			TextBlock textBlock = new();
			foreach (ProjectFileVersion projectFileVersion in this.Parser.FileVersions)
			{
				textBlock.Text += $"\n{projectFileVersion.RelativePath}" +
					$"\n\tLocation In File: {projectFileVersion.LocationInFile}" +
					$"\n\tCurrent Version: {projectFileVersion.CurrentVersion}" +
					$"\n\tNew Version: {projectFileVersion.NewVersion}\n";
			}
			this.cntStepContent.Content = textBlock;
		}
		private void Step5AfterWork()
		{
			TextBlock textBlock = new();
			foreach (ProjectFileVersion projectFileVersion in this.Parser.FileVersions)
			{
				textBlock.Text += $"\n{projectFileVersion.RelativePath}" +
					$"\n\tLocataion In File: {projectFileVersion.LocationInFile}" +
					$"\n\tCurrent Version: {projectFileVersion.CurrentVersion}" +
					$"\n\tNew Version: {projectFileVersion.NewVersion}\n";
			}
			this.cntStepContent.Content = textBlock;
		}
		private void Step6AfterWork()
		{
			//GenerateStep6Results gets called twice.
			//	This was easier than trying to clone the UI Elements.
			this.cntStepContent.Content = this.GenerateStep6Results();
			_ = this.stkpCurrent.Children.Add(
				new Border()
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new(2, 2, 2, 2),
					Margin = new(5, 5, 5, 5),
					Child = this.GenerateStep6Results()
				}
			);
		}
		private StackPanel GenerateStep6Results()
		{
			TextBlock textBlock = new()
			{
				Text = "CHANGELOG files have been built." +
					"\nPlease review the following files for completeness." +
					$"\nMarkdown: {this.Parser.MDChangeLogPath}" +
					$"\nHTML: {this.Parser.HTMLChangeLogPath}"
			};
			Button buttonMarkdown = new()
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new(10, 10, 10, 10),
				FontSize = 16,
				Content = "Open Markdown"
			};
			buttonMarkdown.Click += this.OpenMarkdown_Click;
			Button buttonHTML = new()
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new(10, 10, 10, 10),
				FontSize = 16,
				Content = "Open HTML"
			};
			buttonHTML.Click += this.OpenHTML_Click;
			StackPanel stackPanelButtons = new()
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			_ = stackPanelButtons.Children.Add(buttonMarkdown);
			_ = stackPanelButtons.Children.Add(buttonHTML);
			StackPanel stackPanel = new()
			{
				Orientation = Orientation.Vertical,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			_ = stackPanel.Children.Add(textBlock);
			_ = stackPanel.Children.Add(stackPanelButtons);
			return stackPanel;
		}
		//Step7AfterWork not needed
		private void Step8AfterWork()
		{
			this.cntStepContent.Content = new TextBlock()
			{
				Text = $"Commit SHA: {this.Parser.NewVersionCommit.SHA}"
			};
		}
		private void Step9AfterWork()
		{
			this.cntStepContent.Content = new TextBlock()
			{
				Text = $"Commit SHA: {this.Parser.NewVersionCommit.SHA}\n" +
				$"Tag: {this.Parser.NewVersion.Tag}"
			};
			this.txtStepInstructions.Text = $"Click <Next> to push this new commit and tag upstream to \"{this.Parser.Git.GetCurrentBranch()}\".";
		}
		private void Step10AfterWork()
		{
			this.cntStepContent.Content = new TextBlock()
			{
				Text = $"Commit SHA: {this.Parser.NewVersionCommit.SHA}\n" +
				$"Tag: {this.Parser.NewVersion.Tag}"
			};
		}

		private void btnOpenRepo_Click(Object sender, RoutedEventArgs e)
		{
			_ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = this.RepoDirectory
			}
			);
		}
		private void OpenMarkdown_Click(Object sender, RoutedEventArgs e)
		{
			_ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = this.Parser.MDChangeLogPath,
				UseShellExecute = true
			});
		}
		private void OpenHTML_Click(Object sender, RoutedEventArgs e)
		{
			_ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = this.Parser.HTMLChangeLogPath,
				UseShellExecute = true
			});
		}
	}
}
