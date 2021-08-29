using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BDMSemVerGit.Engine
{
	public class JsonData : IData
	{
		public String DatabasePath { get; set; }

		private List<Contributor> Contributors { get; set; }
		private List<Commit> Commits { get; set; }
		private List<Tag> Tags { get; set; }
		private List<Engine.Version> Versions { get; set; }

		private readonly String ContributorsPath;
		private readonly String CommitsPath;
		private readonly String TagsPath;
		private readonly String VersionsPath;

		private readonly JsonSerializerSettings JsonSerializerSettings = new()
		{
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateParseHandling = DateParseHandling.DateTimeOffset,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc,
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Include,
			TypeNameHandling = TypeNameHandling.Auto
		};


		public JsonData(String databasePath)
		{
			if (String.IsNullOrEmpty(databasePath)) throw new ArgumentException($"'{nameof(databasePath)}' cannot be null or empty.", nameof(databasePath));
			this.Contributors = new();
			this.Commits = new();
			this.Tags = new();
			this.Versions = new();

			this.DatabasePath = databasePath;
			this.ContributorsPath = Path.Combine(this.DatabasePath, "contributors.json");
			this.CommitsPath = Path.Combine(this.DatabasePath, "commits.json");
			this.TagsPath = Path.Combine(this.DatabasePath, "tags.json");
			this.VersionsPath = Path.Combine(this.DatabasePath, "versions.json");

			if (!File.Exists(this.ContributorsPath))
				this.SaveContributors();
			if (!File.Exists(this.CommitsPath))
				this.SaveCommits();
			if (!File.Exists(this.TagsPath))
				this.SaveTags();
			if (!File.Exists(this.VersionsPath))
				this.SaveVersions();

			this.OpenContributors();
			this.OpenCommits();
			this.OpenTags();
			this.OpenVersions();
		}

		#region File IO
		private void SaveContributors()
		{
			File.WriteAllText(
				this.ContributorsPath,
				JsonConvert.SerializeObject(this.Contributors, this.JsonSerializerSettings)
			);
		}
		private void SaveCommits()
		{
			File.WriteAllText(
				this.CommitsPath,
				JsonConvert.SerializeObject(this.Commits, this.JsonSerializerSettings)
			);
		}
		private void SaveTags()
		{
			File.WriteAllText(
				this.TagsPath,
				JsonConvert.SerializeObject(this.Tags, this.JsonSerializerSettings)
			);
		}
		private void SaveVersions()
		{
			File.WriteAllText(
				this.VersionsPath,
				JsonConvert.SerializeObject(this.Versions, this.JsonSerializerSettings)
			);
		}

		private void OpenContributors()
		{
			this.Contributors = JsonConvert.DeserializeObject<List<Contributor>>(
				File.ReadAllText(this.ContributorsPath)
			);
		}
		private void OpenCommits()
		{
			this.Commits = JsonConvert.DeserializeObject<List<Commit>>(
				File.ReadAllText(this.CommitsPath)
			);
		}
		private void OpenTags()
		{
			this.Tags = JsonConvert.DeserializeObject<List<Tag>>(
				File.ReadAllText(this.TagsPath)
			);
		}
		private void OpenVersions()
		{
			this.Versions = JsonConvert.DeserializeObject<List<Engine.Version>>(
				File.ReadAllText(this.VersionsPath)
			);
		}
		#endregion File IO

		#region Commit
		public void AddCommit(Commit commit)
		{
			if (this.Commits.Any(c => c.SHA == commit.SHA))
				this.Commits.Remove(this.Commits.First(c => c.SHA == commit.SHA));
			this.Commits.Add(commit);
			this.SaveCommits();
		}
		public Commit GetNewestCommit()
		{
			if (this.Commits.Count > 0)
				return this.Commits.OrderByDescending(c => c.Date).First();
			else
				return null;
		}
		public IEnumerable<Commit> GetCommits(String[] shas)
		{
			return this.Commits.FindAll(c => shas.Contains(c.SHA));
		}
		public Commit GetCommit(String sha)
		{
			return this.Commits.First(c => c.SHA == sha);
		}
		public Boolean CommitExists(String sha)
		{
			return this.Commits.Any(c => c.SHA == sha);
		}
		#endregion Commit

		#region Tag
		public void AddTag(Tag tag)
		{
			if (this.Tags.Any(t => t.SHA == tag.Ref))
				this.Tags.Remove(this.Tags.First(t => t.Ref == tag.Ref));
			this.Tags.Add(tag);
			this.SaveTags();
		}
		public Tag GetMaxTag()
		{
			if (this.Tags.Count > 0)
				return this.Tags.OrderByDescending(t => t.Name).First();
			else
				return null;
		}
		public IEnumerable<Tag> GetTags()
		{
			return this.Tags;
		}
		public Tag GetTag(String gitRef)
		{
			return this.Tags.First(t => t.Ref == gitRef);
		}
		public Boolean TagExists(String gitRef)
		{
			return this.Tags.Any(t => t.Ref == gitRef);
		}
		#endregion Tag

		#region Version
		private Version ExpandCommits(Engine.Version version)
		{
			if (version.Commits == null)
				version.Commits = new();
			version.Commits.Clear();
			version.Commits.AddRange(this.GetCommits(version.CommitSHAs.ToArray()));
			return version;
		}

		public void AddVersion(Engine.Version version)
		{
			version.CommitSHAs = version.Commits.Select(c => c.SHA).ToList();
			if (this.Versions.Any(v => v.Name == version.Name))
				this.Versions.Remove(this.Versions.First(v => v.Name == version.Name));
			this.Versions.Add(version);
			this.SaveVersions();
		}
		public Engine.Version GetMaxVersion()
		{
			if (this.Versions.Count > 0)
				return this.ExpandCommits(this.Versions.OrderByDescending(v => v.Name).First());
			else
				return null;
		}
		public Int32 GetVersionCount()
		{
			return this.Versions.Count;
		}
		public IEnumerable<Engine.Version> GetVersions()
		{
			return this.Versions;
		}
		public Engine.Version GetVersion(String name)
		{
			return this.ExpandCommits(this.Versions.First(v => v.Name == name));
		}
		public Boolean VersionExists(String name)
		{
			return this.Versions.Any(v => v.Name == name);
		}
		#endregion Version

		public void AddContributor(String name, String email)
		{
			throw new NotImplementedException();
		}
	}
}
