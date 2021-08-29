using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class Version
	{
		public String Name { get; set; }
		public Tag Tag { get; set; }
		
		public List<String> CommitSHAs { get; set; }

		[JsonIgnore()]
		public List<Commit> Commits { get; set; }
		public SemanticVersion SemanticVersion { get; set; }
		public DateTimeOffset ReleaseDate { get; set; }

		public Dictionary<Int64, String> Notes { get; set; }

		public DateTimeOffset GetReleaseDate()
		{
			DateTimeOffset returnValue = DateTimeOffset.MinValue;
			if (this.Tag == null)
				returnValue = DateTimeOffset.MinValue;
			else if (this.Tag.ContributorDates.ContainsKey("Tagger"))
				returnValue = this.Tag.ContributorDates["Tagger"];
			else if (this.Tag.ContributorDates.ContainsKey("Author"))
				returnValue = this.Tag.ContributorDates["Author"];
			else if (this.Tag.ContributorDates.ContainsKey("Committer"))
				returnValue = this.Tag.ContributorDates["Committer"];
			else if (this.Commits.Any())
				returnValue = this.Commits.Max(c => c.Date);
			else
				returnValue = DateTimeOffset.UtcNow;
			return returnValue;
		}

		public Dictionary<String, Int32> GetCommitStats()
		{
			Dictionary<String, Int32> returnValue = new();
			returnValue.Add("BreakingChange", 0);
			if (this.Commits.Any(c =>
					c.IsConventional
					&& c.ConventionalCommit.IsBreakingChange
				))
				returnValue["BreakingChange"] = this.Commits.Count(c =>
					c.IsConventional
					&& c.ConventionalCommit.IsBreakingChange
				);
			foreach (CommitType commitType in Enum.GetValues(typeof(CommitType)))
			{
				returnValue.Add(commitType.ToString(), 0);
				if (this.Commits.Any(c =>
						c.IsConventional
						&& c.ConventionalCommit.Type.Equals(commitType)
					))
					returnValue[commitType.ToString()] = this.Commits.Count(c =>
						c.IsConventional
						&& c.ConventionalCommit.Type.Equals(commitType)
					);
			}
			returnValue.Add("NonConventionalCommit", 0);
			if (this.Commits.Any(c => !c.IsConventional))
				returnValue["NonConventionalCommit"] = this.Commits.Count(c => !c.IsConventional);
			return returnValue;
		}

		public Version()
		{
			this.SemanticVersion = new();
			this.Tag = new();
			this.ReleaseDate = DateTimeOffset.UtcNow;
			this.Notes = new();
		}

		public Version(Tag tag)
		{
			this.Tag = tag;
			if (
				tag.IsSemanticVersionTag
				&& SemanticVersion.TryParse(tag.Name, out SemanticVersion semanticVersion)
			)
				this.SemanticVersion = semanticVersion;
			this.ReleaseDate = this.GetReleaseDate();
			this.Name = tag.Name;
			this.Commits = new();
			this.Notes = new();
		}

		public override String ToString() => this.SemanticVersion.ToString();
	}
}
