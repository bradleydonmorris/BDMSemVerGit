using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class GitCommandResult
	{
		public String StandardOutput { get; set; }
		public String StandardError { get; set; }
		public String[] StandardOutLines { get; set; }
	}
}
