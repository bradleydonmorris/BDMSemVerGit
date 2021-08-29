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
	/// Interaction logic for MessageBuilder.xaml
	/// </summary>
	public partial class MessageBuilder : UserControl
	{
		public event EventHandler<EventArgs> CommitSaved;
		protected virtual void OnCommitSaved() =>
			CommitSaved?.Invoke(this, new EventArgs());



		public Boolean ShowCopyButton { get; set; }
		public Boolean ShowSaveButton { get; set; }
		public String CommitMessageFilePath { get; set; }
		public String ScopesFilePath { get; set; }
		public CommitType DefaultCommitType { get; set;}
		public CommitMessageFileFormat FileFormat { get; set; }

		public ConventionalCommit ConventionalCommit { get; set; }

		public MessageBuilder()
		{
			this.InitializeComponent();
		}

		private void UserControl_Loaded(Object sender, RoutedEventArgs e)
		{
			if (this.ConventionalCommit == null)
				this.ConventionalCommit = new();

			this.btnCopy.IsEnabled = this.ShowCopyButton;
			this.btnCopy.Visibility = this.ShowCopyButton
				? Visibility.Visible
				: Visibility.Collapsed;

			this.btnSave.IsEnabled = this.ShowSaveButton;
			this.btnSave.Visibility = this.ShowSaveButton
				? Visibility.Visible
				: Visibility.Collapsed;

			String BDMSemVerGitDirectoryPath =
				System.IO.Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					".BDMSemVerGit"
				);
			if (String.IsNullOrEmpty(this.CommitMessageFilePath))
				this.CommitMessageFilePath = System.IO.Path.ChangeExtension
				(
					System.IO.Path.Combine(BDMSemVerGitDirectoryPath, "commit-message.ext"),
					this.FileFormat switch
					{
						CommitMessageFileFormat.PlainText => "txt",
						CommitMessageFileFormat.XML => "xml",
						CommitMessageFileFormat.JSON => "json",
						_ => "txt"
					}
				);
			if (String.IsNullOrEmpty(this.ScopesFilePath))
				this.ScopesFilePath = System.IO.Path.Combine(BDMSemVerGitDirectoryPath, ".scopes");
			if (!System.IO.Directory.Exists(BDMSemVerGitDirectoryPath))
				_ = System.IO.Directory.CreateDirectory(BDMSemVerGitDirectoryPath);

			this.cboType.Items.Clear();
			foreach (String type in Constants.CommitTypes)
				_ = this.cboType.Items.Add(type);
			if (this.DefaultCommitType != CommitType.Invalid)
				this.cboType.SelectedItem = this.DefaultCommitType.ToString();

			this.cboScope.Items.Clear();
			foreach (String scope in Scopes.Open(this.ScopesFilePath).AcceptableScops.OrderBy(s => s))
				_ = this.cboScope.Items.Add(scope);
			this.cboScope.SelectedItem = "<none>";
			this.btnSave.ToolTip = $"Saves text to {this.CommitMessageFilePath}";
		}

		private void btnCopy_Click(Object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(this.txtResults.Text);
		}

		private void cboType_SelectionChanged(Object sender, SelectionChangedEventArgs e)
		{
			this.ConventionalCommit.Type = (CommitType)Enum.Parse(typeof(CommitType), this.cboType.SelectedItem as String);
			this.txtResults.Text = this.ConventionalCommit.ToString().Replace("\n", "\r\n");
		}

		private void cboScope_SelectionChanged(Object sender, SelectionChangedEventArgs e)
		{
			this.ConventionalCommit.Scope = this.cboScope.SelectedItem as String;
			this.txtResults.Text = this.ConventionalCommit.ToString().Replace("\n", "\r\n");
		}

		private void txtSummary_TextChanged(Object sender, TextChangedEventArgs e)
		{
			this.ConventionalCommit.Summary = this.txtSummary.Text;
			this.txtResults.Text = this.ConventionalCommit.ToString().Replace("\n", "\r\n");
		}

		private void txtDescription_TextChanged(Object sender, TextChangedEventArgs e)
		{
			this.ConventionalCommit.Description = this.txtDescription.Text;
			this.txtResults.Text = this.ConventionalCommit.ToString().Replace("\n", "\r\n");
		}

		private void txtBreakingChange_TextChanged(Object sender, TextChangedEventArgs e)
		{
			this.ConventionalCommit.BreakingChange = this.txtBreakingChange.Text;
			this.txtResults.Text = this.ConventionalCommit.ToString().Replace("\n", "\r\n");
		}

		private void txtFixes_TextChanged(Object sender, TextChangedEventArgs e)
		{
			this.ConventionalCommit.SetReferences(this.txtFixes.Text);
			this.txtResults.Text = this.ConventionalCommit.ToString().Replace("\n", "\r\n");
		}

		private void btnSave_Click(Object sender, RoutedEventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(this.CommitMessageFilePath))
				System.IO.File.WriteAllText(
					this.CommitMessageFilePath,
					this.FileFormat switch
					{
						CommitMessageFileFormat.PlainText => this.ConventionalCommit.ToString(),
						CommitMessageFileFormat.XML => this.ConventionalCommit.ToXMLString(),
						CommitMessageFileFormat.JSON => this.ConventionalCommit.ToJSONString(),
						_ => this.ConventionalCommit.ToString()
					}
				);
			this.OnCommitSaved();
		}
	}

	public enum CommitMessageFileFormat
	{
		PlainText,
		XML,
		JSON
	}
}
