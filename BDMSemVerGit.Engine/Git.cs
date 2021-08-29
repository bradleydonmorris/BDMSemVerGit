using BDMSemVerGit.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BDMSemVerGit.Engine
{
	public class GitCommandExecutionEventArgs : EventArgs
	{
		public DateTime EventTime { get; set; }
		public String WorkingDirectory { get; set; }
		public String Command { get; set; }

		public GitCommandExecutionEventArgs() { }

		public GitCommandExecutionEventArgs(String workingDirectory, String command)
		{
			this.EventTime = DateTime.UtcNow;
			this.WorkingDirectory = workingDirectory;
			this.Command = command;
		}

		public override String ToString()
		{
			return $"{this.WorkingDirectory}\n\t{this.Command}";
		}
	}

	public class Git
	{
		public event EventHandler<GitCommandExecutionEventArgs> BeforeExecutingCommand;
		protected virtual void OnBeforeExecutingCommand(String workingDirectory, String command) =>
			BeforeExecutingCommand?.Invoke(this, new(workingDirectory, command));

		public event EventHandler<GitCommandExecutionEventArgs> AfterExecutingCommand;
		protected virtual void OnAfterExecutingCommand(String workingDirectory, String command) =>
			AfterExecutingCommand?.Invoke(this, new(workingDirectory, command));

		public String GitDirectory { get; set; }

		public Git(String gitDirectory)
		{
			if (String.IsNullOrEmpty(gitDirectory))
				throw new ArgumentException($"'{nameof(gitDirectory)}' cannot be null or empty.", nameof(gitDirectory));
			this.GitDirectory = gitDirectory;
		}

		private GitCommandResult Execute(String arguments)
		{
			GitCommandResult returnValue = new();
			this.OnBeforeExecutingCommand(this.GitDirectory, $"git {arguments}");
			using (Process process = new())
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.WorkingDirectory = this.GitDirectory;
				process.StartInfo.FileName = "git";
				process.StartInfo.Arguments = arguments;
				process.Start();
				process.WaitForExit();
				returnValue.StandardOutput = process.StandardOutput.ReadToEnd();
				returnValue.StandardError = process.StandardError.ReadToEnd();
			}
			String output = returnValue.StandardOutput;
			output = output.Replace("\r", "|").Replace("\n", "|");
			while (output.IndexOf("||") > -1)
				output = output.Replace("||", "|");
			returnValue.StandardOutLines = output.Split('|');
			this.OnAfterExecutingCommand(this.GitDirectory, $"git {arguments}");
			return returnValue;
		}

		public static Boolean IsGitRepository(String gitDirectory)
		{
			Boolean returnValue = false;
			String arguments = $"rev-parse --is-inside-work-tree";
			GitCommandResult gitCommandResult = new();
			Process process = new();
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.WorkingDirectory = gitDirectory;
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = arguments;
			process.Start();
			process.WaitForExit();
			String output = process.StandardOutput.ReadToEnd();
			gitCommandResult.StandardOutput = output;
			gitCommandResult.StandardError = process.StandardError.ReadToEnd();
			output = output.Replace("\r", "|").Replace("\n", "|");
			output = output.Replace("\r", "|").Replace("\n", "|");
			while (output.IndexOf("||") > -1)
				output = output.Replace("||", "|");
			gitCommandResult.StandardOutLines = output.Split('|');
			foreach (String line in gitCommandResult.StandardOutLines)
				if (
					!String.IsNullOrEmpty(line.Trim())
					&& line.Equals("true")
				)
					returnValue = true;
			return returnValue;
		}
		public static String GetCurrentBranch(String gitDirectory)
		{
			if (!Git.IsGitRepository(gitDirectory))
				return null;
			String returnValue = null;
			String arguments = $"rev-parse --abbrev-ref HEAD";
			GitCommandResult gitCommandResult = new();
			Process process = new();
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.WorkingDirectory = gitDirectory;
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = arguments;
			process.Start();
			process.WaitForExit();
			String output = process.StandardOutput.ReadToEnd();
			gitCommandResult.StandardOutput = output;
			gitCommandResult.StandardError = process.StandardError.ReadToEnd();
			output = output.Replace("\r", "|").Replace("\n", "|");
			output = output.Replace("\r", "|").Replace("\n", "|");
			while (output.IndexOf("||") > -1)
				output = output.Replace("||", "|");
			gitCommandResult.StandardOutLines = output.Split('|');
			foreach (String line in gitCommandResult.StandardOutLines)
				if (!String.IsNullOrEmpty(line.Trim()))
					returnValue = line.Trim();
			return returnValue;
		}

		public String GetCurrentBranch()
		{
			String branchName = null;
			GitCommandResult gitCommandResultCommits = this.Execute("rev-parse --abbrev-ref HEAD");
			foreach (String line in gitCommandResultCommits.StandardOutLines)
				if (!String.IsNullOrWhiteSpace(line))
					branchName = line.Trim();
			if (!String.IsNullOrWhiteSpace(branchName))
				return branchName;
			else return null;
		}

		public void SetPruneTags()
		{
			this.Execute("config fetch.pruneTags true");
		}

		public IEnumerable<String> ListAllBranches()
		{
			foreach (String line in this.Execute("branch -a --list").StandardOutLines)
			{
				String clean = line.Replace("*", "").Trim();
				if (!String.IsNullOrEmpty(clean) && !clean.Contains("->"))
					yield return clean;
			}
		}

		public Boolean BranchExists(String branch)
		{
			if (String.IsNullOrEmpty(branch)) throw new ArgumentException($"'{nameof(branch)}' cannot be null or empty.", nameof(branch));
			foreach (String line in this.Execute($"rev-parse --verify {branch}").StandardOutLines)
				if (line.Equals("fatal: Needed a single revision"))
					return false;
			return true;
		}

		public void CheckoutBranch(String branch, String trackedRemote = null)
		{
			if (String.IsNullOrEmpty(branch)) throw new ArgumentException($"'{nameof(branch)}' cannot be null or empty.", nameof(branch));
			String arguments = $"checkout -b {branch}";
			if (this.BranchExists(branch))
				arguments = $"checkout {branch}";
			this.Execute(arguments);
			if (!String.IsNullOrEmpty(trackedRemote))
				arguments = $"branch -u {trackedRemote}";
			this.Execute(arguments);
		}

		public void Fetch()
		{
			this.Execute("fetch --tags");
		}

		public void Pull()
		{
			this.Execute("pull --tags");
		}

		public IEnumerable<Commit> GetCommits(String from, String to)
		{
			return this.GetCommitsFromLog($"{from}...{to}^");
		}

		public IEnumerable<Commit> GetCommits(String from)
		{
			return this.GetCommitsFromLog($"{from}...HEAD");
		}

		public IEnumerable<Commit> GetCommits()
		{
			return this.GetCommitsFromLog("");
		}

		public Commit GetFirstCommit()
		{
			String firstCommitSHA = null;
			GitCommandResult gitCommandResultCommits = this.Execute("rev-list --max-parents=0 HEAD");
			foreach (String line in gitCommandResultCommits.StandardOutLines)
				if (!String.IsNullOrEmpty(line))
					firstCommitSHA = line.Trim();
			if (!String.IsNullOrEmpty(firstCommitSHA))
				return this.GetCommit(firstCommitSHA);
			else return null;
		}

		public List<TagLine> GetAllTags()
		{
			List<TagLine> returnValues = new();
			GitCommandResult gitCommandResultTags = this.Execute("show-ref --tags");
			foreach (String line in gitCommandResultTags.StandardOutLines)
			{
				if (!String.IsNullOrEmpty(line))
				{
					TagLine tagLine = new();
					String[] fields = line.Split(' ');
					tagLine.TagSHA = fields[0];
					tagLine.Ref = fields[1];
					GitCommandResult gitCommandResultTagCommit = this.Execute("rev-parse " + fields[1] + "^{commit}");
					if (
						gitCommandResultTagCommit.StandardOutLines.Length > 0
						&& !String.IsNullOrEmpty(gitCommandResultTagCommit.StandardOutLines[0])
					)
						tagLine.CommitSHA = gitCommandResultTagCommit.StandardOutLines[0].Trim();
					returnValues.Add(tagLine);
				}
			}

			return returnValues;
		}

		public List<CommitLine> GetAllCommits()
		{
			List<CommitLine> returnValues = new();
			GitCommandResult gitCommandResultCommits = this.Execute("log --pretty=format:\"%H %aI %cI\"");
			foreach (String line in gitCommandResultCommits.StandardOutLines)
			{
				if (!String.IsNullOrEmpty(line))
				{
					String[] fields = line.Split(' ');
					returnValues.Add
					(
						new()
						{
							CommitSHA = fields[0],
							AuthorDate = fields[1].NullIf("").ParseGitDate("yyyy-MM-ddTHH:mm:ssK"),
							CommitDate = fields[2].NullIf("").ParseGitDate("yyyy-MM-ddTHH:mm:ssK")
						}
					);
				}
			}
			return returnValues;
		}

		public Commit GetCommit(String sha)
		{
			Commit returnValue = new();
			String arguments = $"show {sha} --quiet --pretty=format:'<!--%H-->%n<c><an>%an</an><ae>%ae</ae><ad>%aI</ad><cn>%cn</cn><ce>%ce</ce><cd>%cI</cd><sha>%H</sha><sub>%s</sub><b>%b</b></c>'";
			GitCommandResult gitCommandResult = this.Execute(arguments);
			XmlDocument xmlDocument = new();
			gitCommandResult.StandardOutput = gitCommandResult.StandardOutput.Replace("'<!--", "<!--").Replace("</c>'", "</c>");
			xmlDocument.LoadXml($"<list>{gitCommandResult.StandardOutput}</list>");
			foreach (XmlNode xmlCommit in xmlDocument.DocumentElement.ChildNodes)
			{
				if (xmlCommit.Name.Equals("c"))
				{
					returnValue = new();
					Contributor author = new();
					Contributor committer = new();
					foreach (XmlNode xmlCommitField in xmlCommit.ChildNodes)
					{
						switch (xmlCommitField.Name)
						{
							case "an": author.Name = xmlCommitField.InnerText.NullIf(""); break;
							case "ae": author.Email = xmlCommitField.InnerText.NullIf(""); break;
							case "ad": returnValue.ContributorDates.Add("Author", xmlCommitField.InnerText.NullIf("").ParseGitDate("yyyy-MM-ddTHH:mm:ssK")); break;

							case "cn": committer.Name = xmlCommitField.InnerText.NullIf(""); break;
							case "ce": committer.Email = xmlCommitField.InnerText.NullIf(""); break;
							case "cd": returnValue.ContributorDates.Add("Committer", xmlCommitField.InnerText.NullIf("").ParseGitDate("yyyy-MM-ddTHH:mm:ssK")); break;

							case "sha": returnValue.SHA = xmlCommitField.InnerText.NullIf(""); break;
							case "sub": returnValue.Subject = xmlCommitField.InnerText.NullIf(""); break;
							case "b": returnValue.Body = xmlCommitField.InnerText.NullIf(""); break;
						}
					}
					if (!author.IsEmpty) returnValue.Contributors.Add("Author", author);
					if (!committer.IsEmpty) returnValue.Contributors.Add("Committer", committer);
					returnValue.ConventionalCommit = ConventionalCommit.Parse(returnValue.Subject, returnValue.Body);
				}
			}
			return returnValue;
		}

		private IEnumerable<Commit> GetCommitsFromLog(String logStatement)
		{
			this.Fetch(false);
			String arguments = $"log {logStatement} --oneline --pretty=tformat:\"%H\"";
			GitCommandResult gitCommandResult = this.Execute(arguments);
			foreach (String line in gitCommandResult.StandardOutLines)
				if (!String.IsNullOrEmpty(line.Trim()))
					yield return this.GetCommit(line.Trim());
		}

		public Tag GetTag(String refOrname)
		{
			Tag returnValue = null;
			if (!refOrname.StartsWith("refs/tags/"))
				refOrname = $"refs/tags/{refOrname}";
			String arguments = $" for-each-ref {refOrname} --format='<t><ref>%(refname)</ref><sha>%(objectname)</sha><type>%(objecttype)</type><an>%(authorname)</an><ae>%(authoremail:trim)</ae><ad>%(authordate:iso8601)</ad><cn>%(committername)</cn><ce>%(committeremail:trim)</ce><cd>%(committerdate:iso8601)</cd><tn>%(taggername)</tn><te>%(taggeremail:trim)</te><td>%(taggerdate:iso8601)</td><cDate>%(creatordate:iso8601)</cDate><sub>%(contents:subject)</sub><b>%(contents:body)</b></t>'";
			GitCommandResult gitCommandResult = this.Execute(arguments);
			XmlDocument xmlDocument = new();
			gitCommandResult.StandardOutput = gitCommandResult.StandardOutput.Replace("'<t>", "<t>").Replace("</t>'", "</t>");
			xmlDocument.LoadXml($"<list>{gitCommandResult.StandardOutput}</list>");
			foreach (XmlNode xmlCommit in xmlDocument.DocumentElement.ChildNodes)
			{
				returnValue = new();
				String type = "tag";
				Contributor author = new();
				Contributor committer = new();
				Contributor tagger = new();
				foreach (XmlNode xmlCommitField in xmlCommit.ChildNodes)
				{
					switch (xmlCommitField.Name)
					{
						case "an": author.Name = xmlCommitField.InnerText.NullIf(""); break;
						case "ae": author.Email = xmlCommitField.InnerText.NullIf(""); break;
						case "ad": returnValue.ContributorDates.Add("Author", xmlCommitField.InnerText.NullIf("").ParseGitDate("yyyy-MM-ddTHH:mm:ss ParseK")); break;

						case "cn": committer.Name = xmlCommitField.InnerText.NullIf(""); break;
						case "ce": committer.Email = xmlCommitField.InnerText.NullIf(""); break;
						case "cd": returnValue.ContributorDates.Add("Committer", xmlCommitField.InnerText.NullIf("").ParseGitDate("yyyy-MM-ddTHH:mm:ss ParseK")); break;

						case "tn": tagger.Name = xmlCommitField.InnerText.NullIf(""); break;
						case "te": tagger.Email = xmlCommitField.InnerText.NullIf(""); break;
						case "td": returnValue.ContributorDates.Add("Tagger", xmlCommitField.InnerText.NullIf("").ParseGitDate("yyyy-MM-ddTHH:mm:ss ParseK")); break;

						case "ref": returnValue.Ref = xmlCommitField.InnerText.NullIf(""); break;
						case "sha": returnValue.SHA = xmlCommitField.InnerText.NullIf(""); break;

						case "sub": returnValue.Subject = xmlCommitField.InnerText.NullIf(""); break;
						case "b": returnValue.Body = xmlCommitField.InnerText.NullIf(""); break;
						case "type": type = xmlCommitField.InnerText.NullIf(""); break;
					}
				}
				if (!author.IsEmpty) returnValue.Contributors.Add("Author", author);
				if (!committer.IsEmpty) returnValue.Contributors.Add("Committer", committer);
				if (!tagger.IsEmpty) returnValue.Contributors.Add("Tagger", tagger);
				if (type.Equals("tag"))
				{
					String commitSHA = this.Execute($"rev-list -n 1 {returnValue.SHA}").StandardOutLines[0];
					if (!String.IsNullOrEmpty(commitSHA))
						returnValue.Commit = this.GetCommit(commitSHA);
				}
				returnValue.Name = returnValue.Ref[10..];
			}
			return returnValue;
		}

		public void Fetch(Boolean includeTags = false)
		{
			String arguments = "fetch";
			if (includeTags)
				arguments += " --tags";
			this.Execute(arguments);
		}

		public IEnumerable<Tag> GetTags()
		{
			this.Fetch(true);
			String arguments = "tag --list";
			GitCommandResult gitCommandResult = this.Execute(arguments);
			foreach (String line in gitCommandResult.StandardOutLines)
				if (!String.IsNullOrEmpty(line.Trim()))
					yield return this.GetTag(line.Trim());
		}

		public String GetRemoteOriginURL()
		{
			String returnValue = null;
			String arguments = $"config remote.origin.url";
			GitCommandResult gitCommandResult = this.Execute(arguments);
			foreach (String line in gitCommandResult.StandardOutLines)
				if (!String.IsNullOrEmpty(line.Trim()))
					returnValue = line.Trim();
			return returnValue;
		}

		public Tag CreateAnnotatedTag(String name, String commitSHA, String message)
		{
			Tag returnValue;

			String arguments = $"tag --annotate {name} {commitSHA} --message \"{message}\"";
			this.Execute(arguments);

			arguments = $"push origin refs/tags/{name}";
			this.Execute(arguments);
			returnValue = this.GetTag(name);

			return returnValue;
		}

		public void PushTag(String name)
		{
			this.Execute($"push origin refs/tags/{name}");
		}

		public void Push()
		{
			this.Execute($"push");
		}

		public void StageAll()
		{
			String arguments = $"add *";
			this.Execute(arguments);
		}

		public Commit Commit(String message)
		{
			if (message.Contains("\n"))
				message = $"-m \"{message.Replace("\n", "\" -m \"")}\"";
			else
				message = $"-m \"{message}\"";
			this.Execute($"commit {message}");

			String commitSHA = null;
			GitCommandResult gitCommandResult = this.Execute($"rev-parse HEAD");
			foreach (String line in gitCommandResult.StandardOutLines)
				if (!String.IsNullOrEmpty(line.Trim()))
					commitSHA = line;
			if (!String.IsNullOrEmpty(commitSHA))
				return this.GetCommit(commitSHA);
			else
				return null;
		}
	}
}
