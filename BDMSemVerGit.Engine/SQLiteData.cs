using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;

namespace BDMSemVerGit.Engine
{
	public class Parameter
	{
		public String Name { get; set; }
		public Object Value { get; set; }
		public Parameter(String name, Object value)
		{
			this.Name = name;
			this.Value = value;
		}
	}

	public class SQLiteData : IData
	{
		const String DateTimeStorageFormat = "yyyy-MM-ddTHH:mm:ss.fffffff K";

		public String DatabasePath { get; set; }

		private SQLiteConnection SQLiteConnection { get; set; }

		public SQLiteData(String databasePath)
		{
			if (String.IsNullOrEmpty(databasePath)) throw new ArgumentException($"'{nameof(databasePath)}' cannot be null or empty.", nameof(databasePath));
			this.DatabasePath = databasePath;
			this.SQLiteConnection = new SQLiteConnection($"Data Source={this.DatabasePath}").OpenAndReturn();
		}

		private void Execute(String query, params Parameter[] parameters)
		{
			using (SQLiteCommand sqliteCommand = new(query, this.SQLiteConnection))
			{
				foreach (Parameter parameter in parameters)
					if (parameter.Value is DateTimeOffset dateTimeOffset)
						sqliteCommand.Parameters.AddWithValue(parameter.Name,
								dateTimeOffset.ToString(SQLiteData.DateTimeStorageFormat)
						);
					else
						sqliteCommand.Parameters.AddWithValue(parameter.Name, parameter.Value);
				sqliteCommand.ExecuteNonQuery();
			}
		}

		private SQLiteDataReader ExecuteReader(String query, params Parameter[] parameters)
		{
			SQLiteDataReader returnValue;
			using (SQLiteCommand sqliteCommand = new(query, this.SQLiteConnection))
			{
				foreach (Parameter parameter in parameters)
					if (parameter.Value is DateTimeOffset dateTimeOffset)
						sqliteCommand.Parameters.AddWithValue(parameter.Name,
								dateTimeOffset.ToString(SQLiteData.DateTimeStorageFormat)
						);
					else
						sqliteCommand.Parameters.AddWithValue(parameter.Name, parameter.Value);
				returnValue = sqliteCommand.ExecuteReader();
			}
			return returnValue;
		}

		private SQLiteDataReader ExecuteReader(String query)
		{
			SQLiteDataReader returnValue;
			using (SQLiteCommand sqliteCommand = new(query, this.SQLiteConnection))
			{
				returnValue = sqliteCommand.ExecuteReader();
			}
			return returnValue;
		}

		#region Commit
		public void AddCommit(Commit commit)
		{
			this.AddCommit(commit.SHA, commit.Subject, commit.Body);
			foreach (String contributorRole in Constants.ContributorRoles)
			{
				if (
					commit.Contributors.ContainsKey(contributorRole)
					&& commit.Contributors[contributorRole] != null
				)
				{
					this.AddContributor(commit.Contributors[contributorRole].Name, commit.Contributors[contributorRole].Email);
					this.AddCommitContributor(
						commit.SHA,
						commit.Contributors[contributorRole].Email,
						contributorRole, commit.ContributorDates[contributorRole]
					);
				}
			}
			if (
				commit.ConventionalCommit != null
				&& !commit.ConventionalCommit.IsEmpty
			)
			{
				this.AddScope(commit.ConventionalCommit.Scope);
				this.AddConventionalCommit(commit.SHA, commit.ConventionalCommit.Type, commit.ConventionalCommit.Scope, commit.ConventionalCommit.Summary, commit.ConventionalCommit.BreakingChange);
				if (
					commit.ConventionalCommit.References != null
					&& commit.ConventionalCommit.References.Length > 0
				)
					foreach (String fix in commit.ConventionalCommit.References)
						this.AddReference(commit.SHA, fix);
			}
		}

		public Commit GetNewestCommit()
		{
			Commit returnValue = null;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT ""Commit"".""SHA""
					FROM ""Commit""
					WHERE ""Commit"".""CommitId"" =
					(
						SELECT DISTINCT ""CommitId""
							FROM ""CommitContributor""
							WHERE ""Date"" = (SELECT MAX(""Date"") FROM ""CommitContributor""))
");
			while (sqliteDataReader.Read())
				returnValue = this.GetCommit(sqliteDataReader["SHA"] as String);
			return returnValue;
		}

		public IEnumerable<Commit> GetCommits(String[] shas)
		{
			if (shas != null && shas.Length > 1)
				foreach (String sha in shas)
					yield return this.GetCommit(sha);
		}

		public Commit GetCommit(String sha)
		{
			Commit returnValue = null;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""Commit"".""SHA"",
					""Commit"".""Subject"",
					""Commit"".""Body""
					FROM ""Commit""
					WHERE ""Commit"".""SHA"" = @SHA
",
				new Parameter("@SHA", sha)
			);
			while (sqliteDataReader.Read())
				returnValue = new()
				{
					SHA = sqliteDataReader["SHA"] as String,
					Subject = sqliteDataReader["Subject"] as String,
					Body = sqliteDataReader["Body"] as String,
					Contributors = this.GetCommitContributors(sha),
					ContributorDates = this.GetCommitContributorDates(sha),
					ConventionalCommit = this.GetConventionalCommit(sha)
				};
			return returnValue;
		}

		public Boolean CommitExists(String sha)
		{
			Boolean returnValue = false;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT 1 AS ""Exists""
					FROM ""Commit""
					WHERE ""Commit"".""SHA"" = @SHA
",
				new Parameter("@SHA", sha)
			);
			while (sqliteDataReader.Read())
				returnValue = true;
			return returnValue;
		}

		private void AddCommit(String sha, String subject, String body)
		{
			this.Execute(@"
				INSERT INTO ""Commit""(""SHA"", ""Subject"", ""Body"")
					SELECT @SHA AS ""SHA"", @Subject AS ""Subject"", NULLIF(@Body, '') AS ""Body""
					WHERE NOT EXISTS(SELECT 1 FROM ""Commit"" WHERE ""SHA"" = @SHA)
",
				new Parameter("@SHA", sha),
				new Parameter("@Subject", subject),
				new Parameter("@Body", body)
			);
		}

		private void AddCommitContributor(String sha, String email, String role, DateTimeOffset date)
		{
			this.Execute(@"
				INSERT INTO ""CommitContributor""(""CommitId"", ""ContributorId"", ""ContributorRoleId"", ""Date"")
					SELECT
						""Commit"".""CommitId"",
						""Contributor"".""ContributorId"",
						""ContributorRole"".""ContributorRoleId"",
						@Date AS ""Date""
						FROM ""Commit""
							CROSS JOIN ""Contributor""
							CROSS JOIN ""ContributorRole""
							LEFT OUTER JOIN ""CommitContributor""
								ON
									""Commit"".""CommitId"" = ""CommitContributor"".""CommitId""
									AND ""Contributor"".""ContributorId"" = ""CommitContributor"".""ContributorId""
									AND ""ContributorRole"".""ContributorRoleId"" = ""CommitContributor"".""ContributorRoleId""
						WHERE
							""Commit"".""SHA"" = @SHA
							AND ""Contributor"".""Email"" = @Email
							AND ""ContributorRole"".""Name"" = @Role
							AND ""CommitContributor"".""CommitContributorId"" IS NULL
",
				new Parameter("@SHA", sha),
				new Parameter("@Email", email),
				new Parameter("@Role", role),
				new Parameter("@Date", date)
			);
		}

		private void AddScope(String name)
		{
			this.Execute(@"
				INSERT INTO ""Scope""(""Name"")
					SELECT @Name AS ""Name""
					WHERE NOT EXISTS(SELECT 1 FROM ""Scope"" WHERE ""Name"" = @Name)
",
				new Parameter("@Name", name ?? "<none>")
			);
		}

		private void AddConventionalCommit(String sha, CommitType commitType, String scope, String summary, String breakingChangeSummary)
		{
			this.Execute(@"
				INSERT INTO ""ConventionalCommit""(""CommitId"", ""TypeId"", ""ScopeId"", ""Summary"", ""BreakingChangeSummary"")
					SELECT
						""Commit"".""CommitId"",
						""Type"".""TypeId"",
						""Scope"".""ScopeId"",
						NULLIF(@Summary, '<none>') AS ""Summary"",
						NULLIF(@BreakingChangeSummary, '<none>') AS ""BreakingChangeSummary""
						FROM ""Commit""
							CROSS JOIN ""Type""
							CROSS JOIN ""Scope""
							LEFT OUTER JOIN ""ConventionalCommit""
								ON ""Commit"".""CommitId"" = ""ConventionalCommit"".""CommitId""
						WHERE
							""Commit"".""SHA"" = @SHA
							AND ""Type"".""Name"" = @Type
							AND ""Scope"".""Name"" = @Scope
							AND ""ConventionalCommit"".""ConventionalCommitId"" IS NULL
",
				new Parameter("@SHA", sha),
				new Parameter("@Type", commitType.ToString()),
				new Parameter("@Scope", scope ?? "<none>"),
				new Parameter("@Summary", summary),
				new Parameter("@BreakingChangeSummary", breakingChangeSummary)
			);
		}

		private void AddReference(String sha, String text)
		{
			this.Execute(@"
				INSERT INTO ""Reference""(""ConventionalCommitId"", ""Text"")
					SELECT
						""ConventionalCommit"".""ConventionalCommitId"",
						@Text AS ""Text""
						FROM ""Commit""
							INNER JOIN ""ConventionalCommit""
								ON ""Commit"".""CommitId"" = ""ConventionalCommit"".""CommitId""
							LEFT OUTER JOIN ""Reference""
								ON
									""ConventionalCommit"".""ConventionalCommitId"" = ""Reference"".""ConventionalCommitId""
									AND @Text = ""Reference"".""Text""
						WHERE
							""Commit"".""SHA"" = @SHA
							AND ""Reference"".""ReferenceId"" IS NULL
",
				new Parameter("@SHA", sha),
				new Parameter("@Text", text)
			);
		}

		private Dictionary<String, Contributor> GetCommitContributors(String sha)
		{
			Dictionary<String, Contributor> returnValue = new();
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""ContributorRole"".""Name"" AS ""ContributorRole"",
					""Contributor"".""Name"",
					""Contributor"".""Email""
					FROM ""Commit""
						INNER JOIN ""CommitContributor""
							ON ""Commit"".""CommitId"" = ""CommitContributor"".""CommitId""
						INNER JOIN ""Contributor""
							ON ""CommitContributor"".""ContributorId"" = ""Contributor"".""ContributorId""
						INNER JOIN ""ContributorRole""
							ON ""CommitContributor"".""ContributorRoleId"" = ""ContributorRole"".""ContributorRoleId""
					WHERE ""Commit"".""SHA"" = @SHA
",
				new Parameter("@SHA", sha)
			);
			while (sqliteDataReader.Read())
				returnValue.Add
				(
					sqliteDataReader["ContributorRole"] as String,
					new()
					{
						Name = sqliteDataReader["Name"] as String,
						Email = sqliteDataReader["Email"] as String
					}
				);
			return returnValue;
		}

		private Dictionary<String, DateTimeOffset> GetCommitContributorDates(String sha)
		{
			Dictionary<String, DateTimeOffset> returnValue = new();
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""ContributorRole"".""Name"" AS ""ContributorRole"",
					""CommitContributor"".""Date""
					FROM ""Commit""
						INNER JOIN ""CommitContributor""
							ON ""Commit"".""CommitId"" = ""CommitContributor"".""CommitId""
						INNER JOIN ""ContributorRole""
							ON ""CommitContributor"".""ContributorRoleId"" = ""ContributorRole"".""ContributorRoleId""
					WHERE ""Commit"".""SHA"" = @SHA
",
				new Parameter("@SHA", sha)
			);
			while (sqliteDataReader.Read())
				returnValue.Add
				(
					sqliteDataReader["ContributorRole"] as String,
					DateTimeOffset.ParseExact(
						sqliteDataReader["Date"] as String,
						SQLiteData.DateTimeStorageFormat,
						null
					)
				);
			return returnValue;
		}

		private ConventionalCommit GetConventionalCommit(String sha)
		{
			ConventionalCommit returnValue = null;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""Type"".""Name"" AS ""Type"",
					""Scope"".""Name"" AS ""Scope"",
					""ConventionalCommit"".""Summary"",
					""ConventionalCommit"".""BreakingChangeSummary""
					FROM ""ConventionalCommit""
						INNER JOIN ""Commit""
							ON ""ConventionalCommit"".""CommitId"" = ""Commit"".""CommitId""
						INNER JOIN ""Type""
							ON ""ConventionalCommit"".""TypeId"" = ""Type"".""TypeId""
						INNER JOIN ""Scope""
							ON ""ConventionalCommit"".""ScopeId"" = ""Scope"".""ScopeId""
					WHERE ""Commit"".""SHA"" = @SHA
",
				new Parameter("@SHA", sha)
			);
			while (sqliteDataReader.Read())
				returnValue = new()
				{
					Type = (CommitType)Enum.Parse(
						typeof(CommitType),
						sqliteDataReader["Type"] as String),
					Scope = sqliteDataReader["Scope"] as String,
					Summary = sqliteDataReader["Summary"] as String,
					BreakingChange = sqliteDataReader["BreakingChangeSummary"] as String,
					References = this.GetReferences(sha).ToArray()
				};
			return returnValue;
		}

		private IEnumerable<String> GetReferences(String sha)
		{
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""Reference"".""Text""
					FROM ""Reference""
						INNER JOIN ""ConventionalCommit""
							ON ""Reference"".""ConventionalCommitId"" = ""ConventionalCommit"".""ConventionalCommitId""
						INNER JOIN ""Commit""
							ON ""ConventionalCommit"".""CommitId"" = ""Commit"".""CommitId""
					WHERE ""Commit"".""SHA"" = @SHA
					ORDER BY ""Reference"".""Text""
",
				new Parameter("@SHA", sha)
			);
			while (sqliteDataReader.Read())
				yield return sqliteDataReader["Text"] as String;
		}
		#endregion Commit

		#region Tag
		public void AddTag(Tag tag)
		{
			this.AddTag(tag.SHA, tag.Ref, tag.Commit.SHA, tag.Subject, tag.Body);
			foreach (String contributorRole in Constants.ContributorRoles)
			{
				if (
					tag.Contributors.ContainsKey(contributorRole)
					&& tag.Contributors[contributorRole] != null
				)
				{
					this.AddContributor(tag.Contributors[contributorRole].Name, tag.Contributors[contributorRole].Email);
					this.AddTagContributor(
						tag.SHA,
						tag.Contributors[contributorRole].Email,
						contributorRole,
						tag.ContributorDates[contributorRole]
					);
				}
			}
		}

		private void AddTag(String sha, String gitRef, String commitSHA, String subject, String body)
		{
			this.Execute(@"
				INSERT INTO ""Tag""(""CommitId"", ""SHA"", ""Ref"", ""Subject"", ""Body"")
					SELECT
						""Commit"".""CommitId"",
						@SHA AS ""SHA"",
						@Ref AS ""Ref"",
						@Subject AS ""Subject"",
						NULLIF(@Body, '') AS ""Body""
						FROM ""Commit""
						WHERE
							""Commit"".""SHA"" = @CommitSHA
							AND NOT EXISTS(SELECT 1 FROM ""Tag"" WHERE ""Ref"" = @Ref)
",
				new Parameter("@Ref", gitRef),
				new Parameter("@SHA", sha),
				new Parameter("@CommitSHA", commitSHA),
				new Parameter("@Subject", subject),
				new Parameter("@Body", body)
			);
		}

		public Tag GetMaxTag()
		{
			Tag returnValue = null;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT ""Tag"".""Ref""
					FROM ""Tag""
					WHERE ""Tag"".""Ref"" = (SELECT MAX(""Ref"") FROM ""Tag"")
");
			while (sqliteDataReader.Read())
				returnValue = this.GetTag(sqliteDataReader["Ref"] as String);
			return returnValue;
		}

		public IEnumerable<Tag> GetTags()
		{
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT ""Tag"".""Ref""
					FROM ""Tag""
					ORDER BY ""Tag"".""Ref""
");
			while (sqliteDataReader.Read())
				yield return this.GetTag(sqliteDataReader["Ref"] as String);
		}

		public Tag GetTag(String gitRef)
		{
			Tag returnValue = null;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""Commit"".""SHA"" AS ""CommitSHA"",
					""Tag"".""SHA"",
					""Tag"".""Ref"",
					""Tag"".""Subject"",
					""Tag"".""Body""
					FROM ""Tag""
						INNER JOIN ""Commit""
							ON ""Tag"".""CommitId"" = ""Commit"".""CommitId""
					WHERE ""Tag"".""Ref"" = @Ref
",
				new Parameter("@Ref", gitRef)
			);
			while (sqliteDataReader.Read())
				returnValue = new()
				{
					SHA = sqliteDataReader["SHA"] as String,
					Ref = sqliteDataReader["Ref"] as String,
					Commit = this.GetCommit(sqliteDataReader["CommitSHA"] as String),
					Subject = sqliteDataReader["Subject"] as String,
					Body = sqliteDataReader["Body"] as String,
					Name = (sqliteDataReader["Ref"] as String)[10..],
					Contributors = this.GetTagContributors(gitRef),
					ContributorDates = this.GetTagContributorDates(gitRef)
				};
			return returnValue;
		}

		public Boolean TagExists(String gitRef)
		{
			Boolean returnValue = false;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT 1 AS ""Exists""
					FROM ""Tag""
					WHERE ""Tag"".""Ref"" = @Ref
",
				new Parameter("@Ref", gitRef)
			);
			while (sqliteDataReader.Read())
				returnValue = true;
			return returnValue;
		}

		private void AddTagContributor(String sha, String email, String role, DateTimeOffset date)
		{
			this.Execute(@"
				INSERT INTO ""TagContributor""(""TagId"", ""ContributorId"", ""ContributorRoleId"", ""Date"")
					SELECT
						""Tag"".""TagId"",
						""Contributor"".""ContributorId"",
						""ContributorRole"".""ContributorRoleId"",
						@Date AS ""Date""
						FROM ""Tag""
							CROSS JOIN ""Contributor""
							CROSS JOIN ""ContributorRole""
							LEFT OUTER JOIN ""TagContributor""
								ON
									""Tag"".""TagId"" = ""TagContributor"".""TagId""
									AND ""Contributor"".""ContributorId"" = ""TagContributor"".""ContributorId""
									AND ""ContributorRole"".""ContributorRoleId"" = ""TagContributor"".""ContributorRoleId""
						WHERE
							""Tag"".""SHA"" = @SHA
							AND ""Contributor"".""Email"" = @Email
							AND ""ContributorRole"".""Name"" = @Role
							AND ""TagContributor"".""TagContributorId"" IS NULL
",
				new Parameter("@SHA", sha),
				new Parameter("@Email", email),
				new Parameter("@Role", role),
				new Parameter("@Date", date)
			);
		}

		private Dictionary<String, Contributor> GetTagContributors(String gitRef)
		{
			Dictionary<String, Contributor> returnValue = new();
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""ContributorRole"".""Name"" AS ""ContributorRole"",
					""Contributor"".""Name"",
					""Contributor"".""Email""
					FROM ""Tag""
						INNER JOIN ""TagContributor""
							ON ""Tag"".""TagId"" = ""TagContributor"".""TagId""
						INNER JOIN ""Contributor""
							ON ""TagContributor"".""ContributorId"" = ""Contributor"".""ContributorId""
						INNER JOIN ""ContributorRole""
							ON ""TagContributor"".""ContributorRoleId"" = ""ContributorRole"".""ContributorRoleId""
					WHERE ""Tag"".""Ref"" = @Ref
",
				new Parameter("@Ref", gitRef)
			);
			while (sqliteDataReader.Read())
				returnValue.Add
				(
					sqliteDataReader["ContributorRole"] as String,
					new()
					{
						Name = sqliteDataReader["Name"] as String,
						Email = sqliteDataReader["Email"] as String
					}
				);
			return returnValue;
		}

		private Dictionary<String, DateTimeOffset> GetTagContributorDates(String gitRef)
		{
			Dictionary<String, DateTimeOffset> returnValue = new();
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""ContributorRole"".""Name"" AS ""ContributorRole"",
					""TagContributor"".""Date""
					FROM ""Tag""
						INNER JOIN ""TagContributor""
							ON ""Tag"".""TagId"" = ""TagContributor"".""TagId""
						INNER JOIN ""ContributorRole""
							ON ""TagContributor"".""ContributorRoleId"" = ""ContributorRole"".""ContributorRoleId""
					WHERE ""Tag"".""Ref"" = @Ref
",
				new Parameter("@Ref", gitRef)
			);
			while (sqliteDataReader.Read())
				returnValue.Add
				(
					sqliteDataReader["ContributorRole"] as String,
					DateTimeOffset.ParseExact(
						sqliteDataReader["Date"] as String,
						SQLiteData.DateTimeStorageFormat,
						null
					)
				);
			return returnValue;
		}
		#endregion Tag

		#region Version
		public void AddVersion(Engine.Version version)
		{
			this.AddSemanticVersion(version.SemanticVersion.Name, version.SemanticVersion.Major, version.SemanticVersion.Minor, version.SemanticVersion.Patch);
			this.AddTag(version.Tag);
			this.AddVersion(version.Tag.Ref, version.SemanticVersion.Name, version.Name, version.ReleaseDate);
			this.AddVersionCommit(version.Name, version.Tag.Commit.SHA);
			foreach (Commit commit in version.Commits)
				this.AddVersionCommit(version.Name, commit.SHA);

		}

		public Engine.Version GetMaxVersion()
		{
			Engine.Version returnValue = null;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT ""Version"".""Name""
					FROM ""Version""
					WHERE ""Version"".""ReleaseDate"" = (SELECT MAX(""ReleaseDate"") FROM ""Version"")
");
			while (sqliteDataReader.Read())
				returnValue = this.GetVersion(sqliteDataReader["Name"] as String);
			return returnValue;
		}

		public Int32 GetVersionCount()
		{
			Int32 returnValue = 0;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT COUNT(*)
					FROM ""Version""
");
			while (sqliteDataReader.Read())
				returnValue = (Int32)sqliteDataReader[0];
			return returnValue;
		}

		public IEnumerable<Engine.Version> GetVersions()
		{
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT ""Version"".""Name""
					FROM ""Version""
					ORDER BY ""Version"".""Name""
");
			while (sqliteDataReader.Read())
				yield return this.GetVersion(sqliteDataReader["Name"] as String);
		}

		public Engine.Version GetVersion(String name)
		{
			Engine.Version returnValue = null;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""Tag"".""Ref"",
					""SemanticVersion"".""Name"" AS ""SemanticVersion"",
					""Version"".""Name"",
					""Version"".""ReleaseDate""
					FROM ""Version""
						INNER JOIN ""Tag""
							ON ""Version"".""TagId"" = ""Tag"".""TagId""
						INNER JOIN ""SemanticVersion""
							ON ""Version"".""SemanticVersionId"" = ""SemanticVersion"".""SemanticVersionId""
					WHERE ""Version"".""Name"" = @Name
",
				new Parameter("@Name", name)
			);
			while (sqliteDataReader.Read())
				returnValue = new()
				{
					Name = sqliteDataReader["Name"] as String,
					ReleaseDate = DateTimeOffset.ParseExact(
						sqliteDataReader["ReleaseDate"] as String,
						SQLiteData.DateTimeStorageFormat,
						null
					),
					SemanticVersion = this.GetSemanticVersion(sqliteDataReader["SemanticVersion"] as String),
					Tag = this.GetTag(sqliteDataReader["Ref"] as String),
					Commits = this.GetCommits(this.GetVersionCommitSHAs(name).ToArray()).ToList(),
					Notes = this.GetVersionNotes(name)
				};
			return returnValue;
		}

		public Boolean VersionExists(String name)
		{
			Boolean returnValue = false;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT 1 AS ""Exists""
					FROM ""Version""
					WHERE ""Version"".""Name"" = @Name
",
				new Parameter("@Name", name)
			);
			while (sqliteDataReader.Read())
				returnValue = true;
			return returnValue;
		}

		private void AddSemanticVersion(String name, Int64 major, Int64 minor, Int64 patch)
		{
			this.Execute(@"
				INSERT INTO ""SemanticVersion""(""Name"", ""Major"", ""Minor"", ""Patch"")
					SELECT @Name AS ""Name"", @Major AS ""Major"", @Minor AS ""Minor"", @Patch AS ""Patch""
					WHERE NOT EXISTS(SELECT 1 FROM ""SemanticVersion"" WHERE ""SemanticVersion"".""Name"" = @Name)
",
				new Parameter("@Name", name),
				new Parameter("@Major", major),
				new Parameter("@Minor", minor),
				new Parameter("@PAtch", patch)
			);
		}

		private void AddVersion(String tagRef, String semanticVersionName, String name, DateTimeOffset releaseDate)
		{
			this.Execute(@"
				INSERT INTO ""Version""(""SemanticVersionId"", ""TagId"", ""Name"", ""ReleaseDate"")
					SELECT
						""VersionImport"".""SemanticVersionId"",
						""VersionImport"".""TagId"",
						@Name AS ""Name"",
						@ReleaseDate AS ""ReleaseDate""
						FROM
						(
							SELECT
								""SemanticVersion"".""SemanticVersionId"",
								""Tag"".""TagId""
								FROM ""Tag""
									CROSS JOIN ""SemanticVersion""
								WHERE
									""Tag"".""Ref"" = @TagRef
									AND ""SemanticVersion"".""Name"" = @SemanticVersionName
						) AS ""VersionImport""
							LEFT OUTER JOIN ""Version""
								ON
									""VersionImport"".""TagId"" = ""Version"".""TagId""
									AND ""VersionImport"".""SemanticVersionId"" = ""Version"".""SemanticVersionId""
						WHERE ""Version"".""VersionId"" IS NULL
",
				new Parameter("@TagRef", tagRef),
				new Parameter("@SemanticVersionName", semanticVersionName),
				new Parameter("@Name", name),
				new Parameter("@ReleaseDate", releaseDate)
			);
		}

		private void AddVersionCommit(String versionName, String commitSHA)
		{
			this.Execute(@"
				INSERT INTO ""VersionCommit""(""VersionId"", ""CommitId"")
				   SELECT
						""VersionCommitImport"".""VersionId"",
						""VersionCommitImport"".""CommitId""
						FROM
						(
						   SELECT
								""Version"".""VersionId"",
								""Commit"".""CommitId""
								FROM ""Version""
									CROSS JOIN ""Commit""
								WHERE
									""Version"".""Name"" = @VersionName
									AND ""Commit"".""SHA"" = @CommitSHA
						) AS ""VersionCommitImport""
							LEFT OUTER JOIN ""VersionCommit""
								ON
									""VersionCommitImport"".""VersionId"" = ""VersionCommit"".""VersionId""
									AND ""VersionCommitImport"".""CommitId"" = ""VersionCommit"".""CommitId""
						WHERE ""VersionCommit"".""VersionId"" IS NULL
",
				new Parameter("@VersionName", versionName),
				new Parameter("@CommitSHA", commitSHA)
			);
		}

		private void AddVersionNote(String versionName, Int64 sequence, String noteMarkdown)
		{
			if (this.VersionNoteExists(versionName, sequence))
				this.Execute(@"
					UPDATE ""VersionNote""
						SET ""NoteMarkdown"" = @NoteMarkdwon
						WHERE
							""VersionNote"".""VersionId"" = (SELECT ""Version"".""VersionId"" FROM ""Version"" WHERE ""Version"".""Name"" = @VersionName)
							AND ""VersionNote"".""Sequence"" = @Sequence
					; 
",
					new Parameter("@VersionName", versionName),
					new Parameter("@Sequence", sequence),
					new Parameter("@NoteMarkdown", noteMarkdown)
				);
			else
				this.Execute(@"
					INSERT INTO ""VersionNote""(""VersionId"", ""Sequence"", ""NoteMarkdown"")
						SELECT
							""Version"".""VersionId"",
							@Sequence AS ""Sequence"",
							@NoteMarkdown AS ""NoteMarkdown""
							FROM ""Version""
								LEFT OUTER JOIN ""VersionNote""
									ON
										""Version"".""VersionId"" = ""VersionNote"".""VersionId""
										AND @Sequence = ""VersionNote"".""Sequence""
							WHERE
								""Version"".""Name"" = @VersionName
								AND ""VersionNote"".""VersionId"" IS NULL
					;
",
					new Parameter("@VersionName", versionName),
					new Parameter("@Sequence", sequence),
					new Parameter("@NoteMarkdown", noteMarkdown)
				);
		}

		private SemanticVersion GetSemanticVersion(String name)
		{
			SemanticVersion returnValue = null;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""SemanticVersion"".""Name"",
					""SemanticVersion"".""Major"",
					""SemanticVersion"".""Minor"",
					""SemanticVersion"".""Patch""
					FROM ""SemanticVersion""
					WHERE ""SemanticVersion"".""Name"" = @Name
",
				new Parameter("@Name", name)
			);
			while (sqliteDataReader.Read())
			{
				returnValue = new()
				{
					Name = sqliteDataReader["Name"] as String,
					Major = (Int64)sqliteDataReader["Major"],
					Minor = (Int64)sqliteDataReader["Minor"],
					Patch = (Int64)sqliteDataReader["Patch"]
				};
			}
			return returnValue;
		}

		private IEnumerable<String> GetVersionCommitSHAs(String name)
		{
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT ""Commit"".""SHA""
					FROM ""Version""
						INNER JOIN ""VersionCommit""
							ON ""Version"".""VersionId"" = ""VersionCommit"".""VersionId""
						INNER JOIN ""Commit""
							ON ""VersionCommit"".""CommitId"" = ""Commit"".""CommitId""
					WHERE ""Version"".""Name"" = @Name
",
				new Parameter("@Name", name)
			);
			while (sqliteDataReader.Read())
				yield return sqliteDataReader["SHA"] as String;
		}

		private Dictionary<Int64, String> GetVersionNotes(String name)
		{
			Dictionary<Int64, String> returnValue = new();
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT
					""VersionNote"".""Sequence"",
					""VersionNote"".""NoteMarkdown""
					FROM ""Version""
						INNER JOIN ""VersionNote""
							ON ""Version"".""VersionId"" = ""VersionNote"".""VersionId""
					WHERE ""Version"".""Name"" = @Name
					ORDER BY ""VersionNote"".""Sequence""
",
				new Parameter("@Name", name)
			);
			while (sqliteDataReader.Read())
				returnValue.Add(
					(Int64)sqliteDataReader["Sequence"],
					sqliteDataReader["NoteMarkdown"] as String
				);
			return returnValue;
		}

		private Boolean VersionNoteExists(String versionName, Int64 sequence)
		{
			Boolean returnValue = false;
			SQLiteDataReader sqliteDataReader = this.ExecuteReader(@"
				SELECT 1 AS ""Exists""
					FROM ""Version""
						INNER JOIN ""VersionNote""
							ON ""Version"".""VersionId"" = ""VersionNote"".""VersionId""
					WHERE
						""Version"".""Name"" = @VersionName
						AND ""VersionNote"".""Sequence"" = @Sequence;
",
				new Parameter("@VersionName", versionName),
				new Parameter("@Sequence", sequence)
			);
			while (sqliteDataReader.Read())
				returnValue = true;
			return returnValue;
		}
		#endregion Version

		public void AddContributor(String name, String email)
		{
			this.Execute(@"
				INSERT INTO ""Contributor""(""Name"", ""Email"")
					SELECT @Name AS ""Name"", @Email AS ""Email""
						WHERE NOT EXISTS(SELECT 1 FROM ""Contributor"" WHERE ""Email"" = @Email)
",
				new Parameter("@Name", name),
				new Parameter("@Email", email)
			);
		}
	}
}
