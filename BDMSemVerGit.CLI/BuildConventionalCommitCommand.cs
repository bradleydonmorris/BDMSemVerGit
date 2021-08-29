using BDMCommandLine;
using BDMSemVerGit.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BDMSemVerGit.CLI
{
	public enum OutputTarget
	{
		Invalid,
		StandardOutPlain,
		StandardOutFormatted,
		COMMIT_EDITMSG,
		File,
		GitCommand
	}

	public class BuildConventionalCommitCommand : ICommand
	{
		public BuildConventionalCommitCommand()
		{
			this.Arguments = new CommandArgument[]
			{
				new()
				{
					Name = "outtype",
					Alias = "o",
					IsRequired = true,
					Description = "Output type for the resulting message.",
					Options = new string[] { "commitedit", "command", "plain", "format", "file" }
				},
				new()
				{
					Name = "filepath",
					Alias = "f",
					IsRequired = false,
					Description = "Required if outtype is file. Output file path."
				}
			};
		}

		public void Execute()
		{
			ConventionalCommit conventionalCommit = BuildConventionalCommitCommand.Build();
			if (conventionalCommit != null)
				switch (this.OutputTarget)
				{
					case OutputTarget.StandardOutPlain:
						BuildConventionalCommitCommand.OutputCommitMessageToStandardOut(conventionalCommit);
						break;
					case OutputTarget.GitCommand:
						BuildConventionalCommitCommand.OutputCommitMessageToGitCommand(conventionalCommit);
						break;
					case OutputTarget.File:
					case OutputTarget.COMMIT_EDITMSG:
						BuildConventionalCommitCommand.OutputCommitMessageToFile(conventionalCommit, this.FilePath);
						break;
					case OutputTarget.StandardOutFormatted:
					default:
						BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(conventionalCommit);
						break;
				}
		}

		public String[] VerifyArguments(CommandArgument[] commandArguments)
		{
			List<String> returnValue = new();
			Dictionary<String, CommandArgument> arguments = commandArguments.ToDictionary(
				item => item.Name,
				item => item
			);
			if (commandArguments.Any(a => a.Name.Equals("outtype")))
				this.OutputTarget = commandArguments.First(a => a.Name.Equals("outtype")).GetValue().ToLower() switch
				{
					"plain" => OutputTarget.StandardOutPlain,
					"format" => OutputTarget.StandardOutFormatted,
					"file" => OutputTarget.File,
					"command" => OutputTarget.GitCommand,
					"commitedit" => OutputTarget.COMMIT_EDITMSG,
					_ => OutputTarget.Invalid,
				};
			if (this.OutputTarget == OutputTarget.COMMIT_EDITMSG)
				this.FilePath = Path.Combine(Environment.CurrentDirectory, ".git\\COMMIT_EDITMSG");
			else if (commandArguments.Any(a => a.Name.Equals("filepath") || a.Name.Equals("f")))
				this.FilePath = commandArguments.First(a => a.Name.Equals("filepath") || a.Name.Equals("f")).GetValue();
			if (this.OutputTarget == OutputTarget.Invalid)
				returnValue.Add("outtype is required.");
			if (
				this.OutputTarget == OutputTarget.File
				&& String.IsNullOrEmpty(this.FilePath)
			)
				returnValue.Add("If outtype is file, then filepath argument is required.");
			return returnValue.ToArray();
		}

		public ConsoleText[] GetHelpText()
		{
			List<ConsoleText> returnValue = new();
			returnValue.Add(ConsoleText.Green($"Command: {this.Name}"));
			returnValue.Add(ConsoleText.Default($"\n   {this.Description}\n   usage: {this.Usage}"));
			if (this.Arguments != null)
			{
				returnValue.Add(ConsoleText.Default("   Arguments:"));
				foreach (CommandArgument argument in this.Arguments)
				{
					returnValue.Add(ConsoleText.BlankLines(2));
					returnValue.AddRange(argument.GetHelpText());
				}
			}
			returnValue.Add(ConsoleText.BlankLine());
			returnValue.Add(ConsoleText.BlankLine());
			return returnValue.ToArray();
		}

		public String Name => "build";
		public String[] Aliases => new String[] { "build", "buildcommit" };
		public CommandArgument[] Arguments { get; set; }
		public String Description => "Builds a Conventional Commit message and returns it to the chosen out type.";
		public String Usage => "BDMSemVerGit [build|buildcommit]";

		public OutputTarget OutputTarget { get; set; }

		public String FilePath { get; set; }

		public static ConventionalCommit Build
		(
			CommitType? defaultCommitType = null,
			String defaultScope = null,
			String defaultSummary = null,
			String defaultDescription = null,
			String defaultBreakingChange = null,
			String defaultReferences = null
		)
		{
			Scopes scopes = Scopes.Open(Environment.CurrentDirectory);
			ConventionalCommit returnValue = new();
			String defaultCommitTypeText = null;
			if (defaultCommitType.HasValue)
				defaultCommitTypeText = defaultCommitType.ToString();
			String commitTypeOptions = String.Join(", ", Enum.GetNames<CommitType>().Where(s => s != "Invalid"));
			String scopeOptions = String.Join(", ", scopes.AcceptableScops.Where(s => s != "<none>"));
			BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);

			#region Commit Type
			if (!String.IsNullOrEmpty(defaultCommitTypeText))
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Commit Type is required. Acceptable values are:\n"),
					ConsoleText.DarkYellow($"   {commitTypeOptions}\n"),
					ConsoleText.Yellow("Press ENTER to accept the default value of "),
					ConsoleText.DarkYellow($"{defaultCommitTypeText}"),
					ConsoleText.Yellow(".\nCommit Type ["),
					ConsoleText.DarkYellow($"{defaultCommitTypeText}"),
					ConsoleText.Yellow("]: ")
				);
			else 
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Commit Type is required. Acceptable values are:\n"),
					ConsoleText.DarkYellow($"   {commitTypeOptions}\n"),
					ConsoleText.Yellow("\nCommit Type: ")
				);
			Console.ForegroundColor = ConsoleColor.Green;
			Boolean isCommitTypeValid = Enum.TryParse<CommitType>(
				Console.ReadLine().IsEmpty(defaultCommitTypeText).NullIf(""),
				out CommitType commitType
			);
			while (!isCommitTypeValid)
			{
				BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
				if (!String.IsNullOrEmpty(defaultCommitTypeText))
					CommandLine.OutputTextCollection(
						ConsoleText.Red("Commit Type is required. Acceptable values are:\n"),
						ConsoleText.Red($"   {commitTypeOptions}\n"),
						ConsoleText.Yellow("Press ENTER to accept the default value of "),
						ConsoleText.DarkYellow($"{defaultCommitTypeText}"),
						ConsoleText.Yellow(".\nCommit Type ["),
						ConsoleText.DarkYellow($"{defaultCommitTypeText}"),
						ConsoleText.Yellow("]: ")
					);
				else
					CommandLine.OutputTextCollection(
						ConsoleText.Red("Commit Type is required. Acceptable values are:\n"),
						ConsoleText.Red($"   {commitTypeOptions}\n"),
						ConsoleText.Yellow("\nCommit Type: ")
					);
				Console.ForegroundColor = ConsoleColor.Green;
				isCommitTypeValid = Enum.TryParse<CommitType>(
					Console.ReadLine().IsEmpty(defaultCommitTypeText).NullIf(""),
					out commitType
				);
			}
			returnValue.Type = commitType;
			BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
			#endregion Commit Type

			#region Scope
			if (!String.IsNullOrEmpty(defaultScope))
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Scope is optional. However, if provided, it must be one of the following:\n"),
					ConsoleText.DarkYellow($"   {scopeOptions}\n"),
					ConsoleText.Yellow("Press ENTER to accept the default value of "),
					ConsoleText.DarkYellow($"{defaultScope}"),
					ConsoleText.Yellow(".\nScope ["),
					ConsoleText.DarkYellow($"{defaultScope}"),
					ConsoleText.Yellow("]: ")
				);
			else
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Scope is optional. However, if provided, it must be one of the following:\n"),
					ConsoleText.DarkYellow($"   {scopeOptions}\n"),
					ConsoleText.Yellow("\nScope: ")
				);
			Console.ForegroundColor = ConsoleColor.Green;
			String scope = Console.ReadLine().IsEmpty(defaultScope).NullIf("").IsEmpty("<none>");
			Boolean isScopeValid = scopes.IsValid(scope);
			while (!isScopeValid)
			{
				BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
				if (!String.IsNullOrEmpty(defaultScope))
					CommandLine.OutputTextCollection(
						ConsoleText.Red("Scope is optional. However, if provided, it must be one of the following:\n"),
						ConsoleText.Red($"   {scopeOptions}\n"),
						ConsoleText.Yellow("Press ENTER to accept the default value of "),
						ConsoleText.DarkYellow($"{defaultScope}"),
						ConsoleText.Yellow(".\nScope ["),
						ConsoleText.DarkYellow($"{defaultScope}"),
						ConsoleText.Yellow("]: ")
					);
				else
					CommandLine.OutputTextCollection(
						ConsoleText.Red("Scope is optional. However, if provided, it must be one of the following:\n"),
						ConsoleText.Red($"   {scopeOptions}\n"),
						ConsoleText.Yellow("\nScope: ")
					);
				Console.ForegroundColor = ConsoleColor.Green;
				scope = Console.ReadLine().IsEmpty(defaultScope).NullIf("").IsEmpty("<none>");
				isScopeValid = scopes.IsValid(scope);
			}
			returnValue.Scope = scope;
			BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
			#endregion Scope

			#region Summary
			if (!String.IsNullOrEmpty(defaultSummary))
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Summary is required.\n"),
					ConsoleText.Yellow("Press ENTER to accept the default value of "),
					ConsoleText.DarkYellow($"{defaultSummary}"),
					ConsoleText.Yellow(".\nSummary ["),
					ConsoleText.DarkYellow($"{defaultSummary}"),
					ConsoleText.Yellow("]: ")
				);
			else
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Summary is required.\n"),
					ConsoleText.Yellow("\nSummary: ")
				);
			Console.ForegroundColor = ConsoleColor.Green;
			String summary = Console.ReadLine().IsEmpty(defaultSummary).NullIf("");
			while (String.IsNullOrEmpty(summary))
			{
				BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
				if (!String.IsNullOrEmpty(defaultSummary))
					CommandLine.OutputTextCollection(
						ConsoleText.Red("Summary is required.\n"),
						ConsoleText.Yellow("Press ENTER to accept the default value of "),
						ConsoleText.DarkYellow($"{defaultSummary}"),
						ConsoleText.Yellow(".\nSummary ["),
						ConsoleText.DarkYellow($"{defaultSummary}"),
						ConsoleText.Yellow("]: ")
					);
				else
					CommandLine.OutputTextCollection(
						ConsoleText.Red("Summary is required.\n"),
						ConsoleText.Yellow("\nSummary: ")
					);
				Console.ForegroundColor = ConsoleColor.Green;
				summary = Console.ReadLine().IsEmpty(defaultSummary).NullIf("");
			}
			returnValue.Summary = summary;
			BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
			#endregion Summary

			#region Description
			if (!String.IsNullOrEmpty(defaultDescription))
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Description is optional.\n"),
					ConsoleText.Yellow("Press ENTER to accept the default value of "),
					ConsoleText.DarkYellow($"{defaultDescription}"),
					ConsoleText.Yellow(".\nDescription ["),
					ConsoleText.DarkYellow($"{defaultDescription}"),
					ConsoleText.Yellow("]: ")
				);
			else
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Description is optional.\n"),
					ConsoleText.Yellow("\nDescription: ")
				);
			Console.ForegroundColor = ConsoleColor.Green;
			returnValue.Description = Console.ReadLine().IsEmpty(defaultDescription);
			BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
			#endregion Description

			#region Breaking Change
			if (!String.IsNullOrEmpty(defaultBreakingChange))
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Breaking Change is optional.\n"),
					ConsoleText.Yellow("Press ENTER to accept the default value of "),
					ConsoleText.DarkYellow($"{defaultBreakingChange}"),
					ConsoleText.Yellow(".\nBreaking Change ["),
					ConsoleText.DarkYellow($"{defaultBreakingChange}"),
					ConsoleText.Yellow("]: ")
				);
			else
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("Breaking Change is optional.\n"),
					ConsoleText.Yellow("\nBreaking Change: ")
				);
			Console.ForegroundColor = ConsoleColor.Green;
			returnValue.BreakingChange = Console.ReadLine().IsEmpty(defaultBreakingChange);
			BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
			#endregion Breaking Change

			#region References
			if (!String.IsNullOrEmpty(defaultReferences))
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("References are optional.\n"),
					ConsoleText.Yellow("Press ENTER to accept the default value of "),
					ConsoleText.DarkYellow($"{defaultReferences}"),
					ConsoleText.Yellow(".\nReferences ["),
					ConsoleText.DarkYellow($"{defaultReferences}"),
					ConsoleText.Yellow("]: ")
				);
			else
				CommandLine.OutputTextCollection(
					ConsoleText.Yellow("References is optional.\n"),
					ConsoleText.Yellow("\nReferences: ")
				);
			Console.ForegroundColor = ConsoleColor.Green;
			String references = Console.ReadLine().IsEmpty(defaultReferences);
			if (!String.IsNullOrWhiteSpace(defaultReferences))
			{
				List<String> referenceList = new();
				foreach (String reference in references.Replace(" ", "").Split())
				{
					referenceList.Add(reference.Trim());
				}
				if (referenceList.Count > 0)
					returnValue.References = referenceList.ToArray();
			}
			BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
			#endregion References

			BuildConventionalCommitCommand.OutputCommitMessageToStandardOutForDisplay(returnValue);
			CommandLine.OutputTextCollection(
				ConsoleText.Yellow("To accept this commit message type YES and press ENTER: ")
			);
			Console.ForegroundColor = ConsoleColor.Green;
			if (!Console.ReadLine().IsEmpty("NO").Equals("YES"))
				returnValue = null;
			return returnValue;
		}

		public static void OutputCommitMessageToFile(ConventionalCommit conventionalCommit, String filePath)
		{
			if (File.Exists(filePath))
				File.Delete(filePath);
			File.WriteAllLines(
				filePath,
				conventionalCommit.GetMessageLines().Select(cc => cc.Text)
			);
		}

		public static void OutputCommitMessageToGitCommand(ConventionalCommit conventionalCommit)
		{
			Console.WriteLine(conventionalCommit.ToGitCommitMessage());
		}

		public static void OutputCommitMessageToStandardOut(ConventionalCommit conventionalCommit)
		{
			foreach (ConventionalCommitMessageLine conventionalCommitMessageLine
				in conventionalCommit.GetMessageLines())
				Console.WriteLine(conventionalCommitMessageLine.Text);
		}

		public static void OutputCommitMessageToStandardOutForDisplay(ConventionalCommit conventionalCommit)
		{
			Console.Clear();
			Console.ResetColor();
			Console.WriteLine("*****COMMIT MESSAGE WILL BE*********************************************");
			Console.WriteLine("#  Element        Text");
			Console.WriteLine("-  -------------  --------------------------------------------------");
			foreach (ConventionalCommitMessageLine conventionalCommitMessageLine
				in conventionalCommit.GetMessageLines())
			{
				String element = conventionalCommitMessageLine.Element switch
				{
					ConventionalCommitMessageElement.SpacerLine => "<blank>",
					ConventionalCommitMessageElement.Subject => "Subject",
					ConventionalCommitMessageElement.Description => "Desc",
					ConventionalCommitMessageElement.BreakingChangeSummary => "Break Summary",
					ConventionalCommitMessageElement.References => "Fixes",
					_ => "<unkown>",
				};
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write($"{conventionalCommitMessageLine.LineNumber}  {element,-13} >");
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine(conventionalCommitMessageLine.Text);
			}
			Console.ResetColor();
			Console.WriteLine("************************************************************************");
		}
	}
}
