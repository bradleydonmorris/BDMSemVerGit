using System;
using System.Collections.Generic;

namespace BDMSemVerGit.Engine
{
	public interface IData
	{
		#region Commit
		public void AddCommit(Commit commit);
		public Commit GetNewestCommit();
		public IEnumerable<Commit> GetCommits(String[] shas);
		public Commit GetCommit(String sha);
		public Boolean CommitExists(String sha);
		#endregion Commit

		#region Tag
		public void AddTag(Tag tag);
		public Tag GetMaxTag();
		public IEnumerable<Tag> GetTags();
		public Tag GetTag(String gitRef);
		public Boolean TagExists(String gitRef);
		#endregion Tag

		#region Version
		public void AddVersion(Engine.Version version);
		public Engine.Version GetMaxVersion();
		public Int32 GetVersionCount();
		public IEnumerable<Engine.Version> GetVersions();
		public Engine.Version GetVersion(String name);
		public Boolean VersionExists(String name);
		#endregion Version

		public void AddContributor(String name, String email);
	}
}
