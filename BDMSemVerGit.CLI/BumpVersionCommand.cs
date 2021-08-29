using BDMCommandLine;
using BDMSemVerGit.Engine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace BDMSemVerGit.CLI
{
	public class BumpVersionCommand : ICommand
	{
		public BumpVersionCommand()
		{
			this.Arguments = new CommandArgument[]
			{
				new()
				{
					Name = "verbose",
					Alias = null,
					IsRequired = false,
					IsFlag = true,
					Description = "Verbose message output.",
					Options = null
				}
			};
		}

		private void Git_BeforeExecutingCommand(Object sender, GitCommandExecutionEventArgs e)
		{
			CommandLine.OutputTextCollection(ConsoleText.DarkYellow($"Executing Git Command:\n\t{e}\n"));
		}

		//private void Git_AfterExecutingCommand(Object sender, GitCommandExecutionEventArgs e)
		//{
		//	throw new NotImplementedException();
		//}

		public void Execute()
		{
			this.Parser = new Parser(Environment.CurrentDirectory);
			if (this.Verbose)
			{
				this.Parser.Git.BeforeExecutingCommand += this.Git_BeforeExecutingCommand;
				//this.ChangeParser.Git.AfterExecutingCommand += this.Git_AfterExecutingCommand;
			}

			CommandLine.OutputTextCollection(ConsoleText.Blue("Transfering Git data into database...\n"));
			this.Parser.TransferGitDataToDatabase();
			CommandLine.OutputTextCollection(ConsoleText.Blue("Gathering version information...\n"));
			this.Parser.GatherVersions();
			CommandLine.OutputTextCollection(ConsoleText.Blue("Building CHANGELOG...\n"));
			this.Parser.BuildChangeLog();
			CommandLine.OutputTextCollection(
				ConsoleText.Yellow("The next version will be "),
				ConsoleText.DarkYellow(this.Parser.NewVersion.ToString()),
				ConsoleText.Yellow(".\nThe CHANGELOG has been published to\n\t"),
				ConsoleText.DarkYellow(this.Parser.MDChangeLogPath),
				ConsoleText.Yellow("\n\t"),
				ConsoleText.DarkYellow(this.Parser.HTMLChangeLogPath),
				ConsoleText.Yellow("\nPlease review the new CHANGELOG.\n"),
				ConsoleText.Yellow("After reviewing the new CHANGELOG, do you wish to proceed?\n"),
				ConsoleText.Yellow("Type YES or NO: ")
			);
			Console.ForegroundColor = ConsoleColor.Green;
			if (Console.ReadLine() == "YES")
			{
				Boolean continueTrying = true;
				ConventionalCommit conventionalCommit = null;
				while (continueTrying)
				{
					conventionalCommit = BuildConventionalCommitCommand.Build(CommitType.changelog, null, "finalize iteration", "close iteration and build changelog");
					if (conventionalCommit == null)
					{
						CommandLine.OutputTextCollection(
							ConsoleText.Red("You cancelled out of the message buiding process.\n"),
							ConsoleText.Yellow("Do you wish to try again? ")
						);
						if (Console.ReadLine().ToUpper() == "YES")
							continueTrying = true;
						else
							continueTrying = false;
					}
					else
						continueTrying = false;
				}
				if (conventionalCommit != null)
				{
					this.Parser.CommitVersion(conventionalCommit);
					CommandLine.OutputTextCollection(
						ConsoleText.Yellow("New commit create: "),
						ConsoleText.DarkYellow(this.Parser.NewVersionCommit.SHA),
						ConsoleText.Yellow("\nDo you wish to tag this new commit? ")
					);
					Console.ForegroundColor = ConsoleColor.Green;
					if (Console.ReadLine().ToUpper() == "YES")
					{
						this.Parser.TagCommittedVersion();
						CommandLine.OutputTextCollection(
							ConsoleText.Yellow("New tag create: "),
							ConsoleText.DarkYellow(this.Parser.NewVersion.Tag.SHA),
							ConsoleText.Yellow("\nDo you wish to push this new commit and tag up stream? ")
						);
						Console.ForegroundColor = ConsoleColor.Green;
						if (Console.ReadLine().ToUpper() == "YES")
						{
							this.Parser.PushCommitAndTag();
							CommandLine.OutputTextCollection(
								ConsoleText.Yellow("New commit and tag have been pushed up stream\n"),
								ConsoleText.Yellow("Press ENTER to exit.")
							);
							Console.ReadLine();
						}
						else
						{
							CommandLine.OutputTextCollection(
								ConsoleText.Yellow($"New commit and tag not pushed.\n"),
								ConsoleText.Yellow("Use the following commands to push up stream."),
								ConsoleText.Blue($"git push\ngit push origin {this.Parser.NewVersion.Tag.Ref}"),
								ConsoleText.Yellow("Press ENTER to exit.")
							);
							Console.ResetColor();
						}
					}
					else
						CommandLine.OutputException("BumpVersion process incomplete!");
				}
				else
					CommandLine.OutputException("BumpVersion process incomplete!");
			}
			else
				CommandLine.OutputException("BumpVersion process incomplete!");
		}

		public String[] VerifyArguments(CommandArgument[] commandArguments)
		{
			this.Verbose = commandArguments.Any(a => a.Name.Equals("verbose") && a.IsFlagedTrue);
			
			return new List<String>().ToArray();
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
					returnValue.Add(ConsoleText.Default($"\n\n   "));
					returnValue.AddRange(argument.GetHelpText());
				}
			}
			returnValue.Add(ConsoleText.BlankLine());
			returnValue.Add(ConsoleText.BlankLine());
			return returnValue.ToArray();
		}

		public String Name => "bump";
		public String[] Aliases => new String[] { "bump", "bumpver", "bumpversion" };
		public CommandArgument[] Arguments { get; set; }
		public String Description => "Bumps version of current repo, commits changes, and tags new commit.";
		public String Usage => "BDMSemVerGit [bump|bumpver|bumpversion]";

		public Boolean Verbose { get; set; }

		private Parser Parser;
	}
}
