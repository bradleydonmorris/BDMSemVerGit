using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDMCommandLine;
using BDMSemVerGit.Engine;

namespace BDMSemVerGit.CLI
{
	public class Program
	{

		static void Main(String[] args)
		{
			ConsoleText.DefaultForegroundColor = Console.ForegroundColor;
			ConsoleText.DefaultBackgroundColor = Console.BackgroundColor;

			CommandLine commandLine = new();
			commandLine.AddCommand(new BuildConventionalCommitCommand());
			commandLine.AddCommand(new BumpVersionCommand());
			//try
			//{
				if (commandLine.ParseArguments(args))
				{
					if (!Git.IsGitRepository(Environment.CurrentDirectory))
						throw new Exception("Current directory is not a Git directory!");
					commandLine.Execute();
					Console.ResetColor();
				}
			//}
			//catch (Exception exception)
			//{
			//	CommandLine.OutputException(exception);
			//}
			Console.ResetColor();
		}
	}
}
