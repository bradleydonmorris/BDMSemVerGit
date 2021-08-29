using Markdig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class ParserStatusEventArgs : EventArgs
	{
		public DateTime EventTime { get; set; }
		public String Status { get; set; }

		public ParserStatusEventArgs() { }

		public ParserStatusEventArgs(String status)
		{
			this.EventTime = DateTime.UtcNow;
			this.Status = status;
		}

		public override String ToString()
		{
			return $"{this.EventTime.ToLocalTime():yyyy-MM-dd HH:mm:ssL} - {this.Status}";
		}
	}

	public class Parser
	{
		public event EventHandler<ParserStatusEventArgs> StatusChange;
		protected virtual void OnStatusChange(String status) =>
			StatusChange?.Invoke(this, new(status));

		public String RepoDirectory { get; set; }

		public Git Git { get; set; }
		//public SQLiteData SQLiteData { get; set; }
		public IData Data { get; set; }

		public String MDChangeLogPath { get; set; }
		public String HTMLChangeLogPath { get; set; }

		public List<Engine.Version> Versions { get; set; }
		public Engine.Version NewVersion { get; set; }
		public Commit NewVersionCommit { get; set; }

		public String RefLinkTemplate { get; set; }
		public String CommitLinkTemplate { get; set; }
		public List<ProjectFileVersion> FileVersions { get; set; }

		//foreach (ProjectFileVersion projectFileVersion in this.dgProjectFileVersions.ItemsSource)
		//	projectFileVersion.NewVersion = version.SemanticVersion;

		private readonly String RepoName;
		private readonly MarkdownPipeline MarkdownPipeline;
		private readonly String MarkdownPath;
		private readonly String HTMLPath;
		private readonly Dictionary<String, String> Templates;
		private readonly String GitMarkdownPath;
		private readonly String GitHTMLPath;

		public String AppDirectory { get; set; }
		private readonly String VersionsMarkdownDirectory;
		private readonly String TemplateDirectory;

		public Parser(String repoDirectory)
		{
			if (!Git.IsGitRepository(repoDirectory))
				throw new Exception("No Git directory provided!");
			this.RepoDirectory = repoDirectory;
			this.Git = new(this.RepoDirectory);
			this.AppDirectory = Path.Combine(this.Git.GitDirectory, ".BDMSemVerGit");

			if (!Directory.Exists(this.AppDirectory))
				Directory.CreateDirectory(this.AppDirectory);
			DirectoryInfo directoryInfo = new(this.AppDirectory);
			if (!directoryInfo.Attributes.HasFlag(FileAttributes.Hidden))
				directoryInfo.Attributes |= FileAttributes.Hidden;
			this.TemplateDirectory = Path.Combine(this.AppDirectory, "templates");
			if (!Directory.Exists(this.TemplateDirectory))
				Directory.CreateDirectory(this.TemplateDirectory);
			this.VersionsMarkdownDirectory = Path.Combine(this.AppDirectory, "versions");
			if (!Directory.Exists(this.VersionsMarkdownDirectory))
				Directory.CreateDirectory(this.VersionsMarkdownDirectory);

			this.RepoName = Path.GetFileName(this.Git.GitDirectory);
			this.GitMarkdownPath = Path.Combine(this.Git.GitDirectory, "CHANGELOG.md");
			this.GitHTMLPath = Path.Combine(this.Git.GitDirectory, "CHANGELOG.html");
			this.MarkdownPath = Path.Combine(this.AppDirectory, "CHANGELOG.md");
			this.HTMLPath = Path.Combine(this.AppDirectory, "CHANGELOG.html");
			this.MDChangeLogPath = this.GitMarkdownPath;
			this.HTMLChangeLogPath = this.GitHTMLPath;
			this.Templates = new();

			//String sqliteFile = Path.Combine(this.AppDirectory, "CHANGELOG.db");
			//if (!File.Exists(sqliteFile))
			//	File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates\\template.db"), sqliteFile);
			//this.Data = new SQLiteData(sqliteFile);

			String jsonDirectory = Path.Combine(this.AppDirectory, "data");
			if (!Directory.Exists(jsonDirectory))
				Directory.CreateDirectory(jsonDirectory);
			this.Data = new JsonData(jsonDirectory);

			foreach (String templateName in new String[] { "version", "version-note", "type", "commit-scope", "commit-noscope", "refs", "breaking-change", "version-separator" })
			{
				String templatePath = Path.Combine(this.TemplateDirectory, $"{templateName}.md");
				if (!File.Exists(templatePath))
					File.Copy(
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"templates\\{templateName}.md"),
						templatePath
					);
				this.Templates.Add(templateName, File.ReadAllText(templatePath));
			}

			this.MarkdownPipeline = new MarkdownPipelineBuilder()
				.UseAdvancedExtensions()
				.UseBootstrap()
				.UseFigures()
				.UsePipeTables()
				.UseGridTables()
				.UseTaskLists()
				.UseMediaLinks()
				.UseListExtras()
				.UseFooters()
				.UseDefinitionLists()
				.UseDiagrams()
				.UseCitations()
				.UseSoftlineBreakAsHardlineBreak()
				.Build();
			this.BuildURLTemplatesFromGitOriginURL();
		}

		public void BuildURLTemplatesFromGitOriginURL()
		{
			String refLinkTemplate = "unknown";
			String commitLinkTemplate = "unknown";
			Uri remoteOriginURL = new(this.Git.GetRemoteOriginURL());
			if (remoteOriginURL.Host.EndsWith("azure.com"))
			{
				String[] orgProject = remoteOriginURL.LocalPath.Substring(
						0,
						remoteOriginURL.LocalPath.IndexOf("_git") - 1
					).Split('/');
				String organization = orgProject[1];
				String project = orgProject[2];
				refLinkTemplate = $"[|@RefNumber|]({remoteOriginURL.Scheme}://{remoteOriginURL.Host}:{remoteOriginURL.Port}/{organization}/{project}/_workitems/edit/|@RefNumberOnly|/)";
				commitLinkTemplate = $"[|@CommitSHAShort|]({remoteOriginURL.Scheme}://{remoteOriginURL.Host}:{remoteOriginURL.Port}/{organization}/{project}/_git/{project}/commit/|@CommitSHALong|?tab=details)";
			}
			this.SetURLTemplates(refLinkTemplate, commitLinkTemplate);
		}

		public void SetURLTemplates(String refLinkTemplate, String commitLinkTemplate)
		{
			this.RefLinkTemplate = refLinkTemplate;
			this.CommitLinkTemplate = commitLinkTemplate;
		}

		public void TransferGitDataToDatabase()
		{
			this.OnStatusChange($"Pruning Tags");
			this.Git.SetPruneTags();

			this.OnStatusChange($"Fetching");
			this.Git.Fetch();

			this.OnStatusChange($"Getting All Tags");
			List<TagLine> tagLines = this.Git.GetAllTags();

			this.OnStatusChange($"Getting All Commits");
			List<CommitLine> commitLines = this.Git.GetAllCommits();

			this.OnStatusChange($"Adding Tags to Database");
			List<Tag> semVerTags = new();
			foreach (TagLine tagLine in tagLines)
			{
				Tag tag = this.Git.GetTag(tagLine.Ref);
				if (!this.Data.TagExists(tagLine.Ref))
					this.Data.AddTag(tag);
				if (tag.IsSemanticVersionTag)
					semVerTags.Add(tag);
			}

			this.OnStatusChange($"Adding Commits to Database");
			foreach (CommitLine commitLine in commitLines)
			{
				if (!this.Data.CommitExists(commitLine.CommitSHA))
					this.Data.AddCommit(this.Git.GetCommit(commitLine.CommitSHA));
			}
			Commit firstCommit = this.Git.GetFirstCommit();
			Int32 versionIndex = -1;

			this.OnStatusChange($"Gathering Version Commits and Adding to Datbase");
			List<Engine.Version> versions = new();
			foreach (Tag tag in semVerTags.OrderBy(t => t.Date))
			{
				Engine.Version version = new(tag);
				if (
					firstCommit.SHA == tag.Commit.SHA
					&& versionIndex == -1
				)
					version.Commits.Add(firstCommit);
				else if (versionIndex == -1)
					version.Commits = this.Git.GetCommits(firstCommit.SHA, version.Tag.Ref).ToList();
				else
					version.Commits = this.Git.GetCommits(versions[versionIndex].Tag.Commit.SHA, version.Tag.Ref).ToList();
				this.Data.AddVersion(version);
				versions.Add(version);
				versionIndex ++;
			}
		}

		public void GatherVersions()
		{
			this.OnStatusChange($"Gathering Versions");
			this.Versions = this.Data.GetVersions().ToList();
			if (this.Versions == null)
				this.Versions = new();
			this.NewVersion = new();
			Engine.Version maxVersion = this.Data.GetMaxVersion();
			if (
				maxVersion != null
				&& maxVersion.Tag != null
			)
			{
				this.OnStatusChange($"Gathering Commits Since Last Version");
				this.NewVersion.Commits = this.Git.GetCommits(maxVersion.Tag.Commit.SHA).ToList();
				Dictionary<String, Int32> commitStats = this.NewVersion.GetCommitStats();
				this.OnStatusChange($"Bumping Version Based on Commits");
				if (commitStats["BreakingChange"] > 0)
					this.NewVersion.SemanticVersion = maxVersion.SemanticVersion.Bump("Major");
				else if (commitStats["feat"] > 0)
					this.NewVersion.SemanticVersion = maxVersion.SemanticVersion.Bump("Minor");
				else
					this.NewVersion.SemanticVersion = maxVersion.SemanticVersion.Bump("Patch");
			}
			else
			{
				this.OnStatusChange($"No Versions Found Defaults to v1.0.0");
				this.NewVersion.SemanticVersion = SemanticVersion.Parse("v1.0.0");
				this.NewVersion.Commits = this.Git.GetCommits().ToList();
			}
			this.NewVersion.Name = this.NewVersion.SemanticVersion.Name;
			this.Versions.Add(this.NewVersion);
		}

		public void GatherProjectFileVersions()
		{
			this.OnStatusChange($"Gathering Project File Versions");
			this.FileVersions = Engine.FileVersions.GetVersions(this.RepoDirectory);
			foreach (ProjectFileVersion projectFileVersion in this.FileVersions)
				projectFileVersion.NewVersion = this.NewVersion.SemanticVersion;
		}

		public void SetProjectFileVersions()
		{
			this.OnStatusChange($"Setting Project File Versions");
			this.FileVersions = Engine.FileVersions.GetVersions(this.RepoDirectory);
			foreach (ProjectFileVersion projectFileVersion in this.FileVersions)
				projectFileVersion.NewVersion = this.NewVersion.SemanticVersion;
		}

		public void BuildChangeLog()
		{
			this.OnStatusChange($"Building CHANGELOG Files");
			foreach (Engine.Version version in this.Versions)
			{
				String versionPath = Path.Combine(this.VersionsMarkdownDirectory, $"{version.Name}.md");
				if (
					this.NewVersion.Name == version.Name
					&& File.Exists(versionPath)
				)
					File.Delete(versionPath);
				if (!File.Exists(versionPath))
				{
					StringBuilder stringBuild = new();
					stringBuild.AppendLine(
						this.Templates["version"]
							.Replace("{@Version}", version.Name)
							.Replace("{@Date}", version.ReleaseDate.ToString("yyyy-MM-dd"))
					);
					foreach (Int64 key in version.Notes.Keys)
						stringBuild.AppendLine(
							this.Templates["version-note"]
								.Replace("{@Markdown}", version.Notes[key])
						);
					foreach (CommitType commitType in Enum.GetValues<CommitType>())
					{
						if (commitType != CommitType.Invalid)
						{
							var commits = version.Commits
								.Where(c => c.IsConventional)
								.Where(cc => cc.ConventionalCommit.Type.Equals(commitType))
								.OrderBy(c => c.ContributorDates["Committer"]);
							if (commits.Any())
							{
								stringBuild.AppendLine(
									this.Templates["type"]
										.Replace("{@Type}", commitType.ToString())
								);
								foreach (Commit commit in commits)
								{
									String commitURL = this.CommitLinkTemplate
										.Replace("|@CommitSHAShort|", commit.SHA[..7])
										.Replace("|@CommitSHALong|", commit.SHA);
									if (
										!String.IsNullOrEmpty(commit.ConventionalCommit.Scope)
										&& !commit.ConventionalCommit.Scope.Equals("<none>")
									)
										stringBuild.AppendLine(
											this.Templates["commit-scope"]
												.Replace("{@Link}", commitURL)
												.Replace("{@Summary}", commit.ConventionalCommit.Summary)
												.Replace("{@Scope}", commit.ConventionalCommit.Scope.Equals("<none>")
													? null
													: commit.ConventionalCommit.Scope
												)
										);
									else
										stringBuild.AppendLine(
											this.Templates["commit-noscope"]
												.Replace("{@Link}", commitURL)
												.Replace("{@Summary}", commit.ConventionalCommit.Summary)
										);
									if (commit.ConventionalCommit.IsBreakingChange)
										stringBuild.AppendLine(
											this.Templates["breaking-change"]
												.Replace("{@Summary}", commit.ConventionalCommit.BreakingChange)
										);
									if (commit.ConventionalCommit.References != null && commit.ConventionalCommit.References.Length > 0)
									{
										String refsTemplate = "   Fixes: {@RefLink(delimiter=\", \")}";
										String replacementToken = refsTemplate[refsTemplate.IndexOf("{@RefLink")..];
										replacementToken = replacementToken[..(replacementToken.IndexOf("}") + 1)];
										refsTemplate = refsTemplate.Replace(replacementToken, "{@RefLinks}");
										String delimiter = replacementToken[(replacementToken.IndexOf("(delimiter=\"") + 12)..];
										delimiter = delimiter[..(delimiter.IndexOf("\")}"))];
										String refLinks = String.Empty;
										for (Int32 loop = 0; loop < commit.ConventionalCommit.References.Length; loop++)
										{
											String gitRef = commit.ConventionalCommit.References[loop];
											String refLink = this.RefLinkTemplate
												.Replace("|@RefNumber|", gitRef)
												.Replace("|@RefNumberOnly|", gitRef.Replace("#", ""));
											if (loop < (commit.ConventionalCommit.References.Length - 1))
												refLinks += $"{refLink}{delimiter}";
											else
												refLinks += $"{refLink}";
										}
										stringBuild.AppendLine(refsTemplate.Replace("{@RefLinks}", refLinks));
									}
								}
							}
						}
					}
					File.WriteAllText(versionPath, stringBuild.ToString());
				}
			}
			StringBuilder changelogText = new();
			foreach (String filePath in Directory.EnumerateFiles(this.VersionsMarkdownDirectory).OrderByDescending(f => f))
			{
				changelogText.Append(File.ReadAllText(filePath));
				changelogText.AppendLine(this.Templates["version-separator"]);
			}
			File.WriteAllText(this.MarkdownPath, changelogText.ToString());
			File.WriteAllText(this.GitMarkdownPath, changelogText.ToString());
			File.WriteAllText(this.HTMLPath, Markdown.ToHtml(changelogText.ToString(), this.MarkdownPipeline));
			File.WriteAllText(this.GitHTMLPath, Markdown.ToHtml(changelogText.ToString(), this.MarkdownPipeline));
		}

		public void CommitVersion(ConventionalCommit conventionalCommit)
		{
			this.OnStatusChange($"Staging Changes");
			this.Git.StageAll();
			this.OnStatusChange($"Committing Changes");
			this.NewVersionCommit = this.Git.Commit(conventionalCommit.ToString());
		}

		public void TagCommittedVersion()
		{
			this.OnStatusChange($"Tagging Commit");
			if (
				this.NewVersionCommit != null
				&& this.NewVersionCommit.IsConventional
			)
				this.NewVersion.Tag = this.Git.CreateAnnotatedTag(this.NewVersion.Name, this.NewVersionCommit.SHA, this.NewVersionCommit.ConventionalCommit.Summary);
		}

		public void PushCommitAndTag()
		{
			this.OnStatusChange($"Pusing Commit");
			this.Git.Push();
			this.OnStatusChange($"Pusing Tag");
			this.Git.PushTag(this.NewVersion.Tag.Name);
		}
	}
}
