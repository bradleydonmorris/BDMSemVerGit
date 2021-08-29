using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class Constants
	{
		public static readonly String[] CommitTypes = { "feat", "fix", "perf", "refactor", "test", "chore", "build", "ci", "docs", "revert", "changelog" };
		public static readonly String[] ContributorRoles = { "Author", "Committer", "Tagger" };

	}
}
