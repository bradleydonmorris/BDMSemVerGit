using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class CommitLine
	{
		public String CommitSHA { get; set; }
		public DateTimeOffset AuthorDate { get; set; }
		public DateTimeOffset CommitDate { get; set; }
	}
}
