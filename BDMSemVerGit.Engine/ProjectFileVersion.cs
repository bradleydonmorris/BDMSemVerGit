using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class ProjectFileVersion
	{
		public Boolean AlterVersion { get; set; }
		public String ProjectName { get; set; }
		public ProjectFileVersionType FileType { get; set; }
		public String RelativePath { get; set; }
		public String FilePath { get; set; }
		public SemanticVersion CurrentVersion { get; set; }
		public SemanticVersion NewVersion { get; set; }
		public String LocationInFile { get; set; }
	}
}
