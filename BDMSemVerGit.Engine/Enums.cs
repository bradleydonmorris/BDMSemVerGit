using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public enum ConventionalCommitMessageElement
	{
		Subject,
		Description,
		BreakingChangeSummary,
		References,
		SpacerLine
	}

	public enum CommitType
	{
		Invalid,
		feat,
		fix,
		perf,
		refactor,
		test,
		chore,
		build,
		ci,
		docs,
		revert,
		changelog
	}

	//public enum ContributorRole
	//{
	//	Author,
	//	Committer,
	//	Tagger
	//}

	public enum ProjectFileVersionType
	{
		Xml,
		AssemblyInfo
	}
}
