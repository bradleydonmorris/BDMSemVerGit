using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class ConventionalCommitMessageLine
	{
		public Int32 LineNumber { get; set; }
		public ConventionalCommitMessageElement Element { get; set; }
		public String Text { get; set; }

		public static ConventionalCommitMessageLine GetSpacerLine(Int32 lineNumber) =>
			new() { LineNumber = lineNumber, Element = ConventionalCommitMessageElement.SpacerLine };
	}
}
