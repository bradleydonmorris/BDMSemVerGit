using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class TagLine
	{
		public String TagSHA { get; set; }
		public String Ref { get; set; }
		public String CommitSHA { get; set; }
		public Boolean IsSemanticVersionTag => Regex.IsMatch(this.Ref[10..], "^v([0-9]+)\\.([0-9]+)\\.([0-9]+)");

	}
}
