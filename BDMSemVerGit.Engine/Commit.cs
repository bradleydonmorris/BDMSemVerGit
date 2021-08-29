using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class Commit
	{
		public Dictionary<String, Contributor> Contributors { get; set; }
		public Dictionary<String, DateTimeOffset> ContributorDates { get; set; }
		public String SHA { get; set; }
		public String Subject { get; set; }
		public String Body { get; set; }
		public ConventionalCommit ConventionalCommit { get; set; }
		public Commit()
		{
			this.Contributors = new();
			this.ContributorDates = new();
		}
		public Boolean IsConventional => (
				this.ConventionalCommit != null
				&& !this.ConventionalCommit.IsEmpty
			);

		public DateTimeOffset Date
		{
			get
			{
				if (this.ContributorDates.ContainsKey("Author"))
					return this.ContributorDates["Author"];
				else if (this.ContributorDates.ContainsKey("Committer"))
					return this.ContributorDates["Committer"];
				else return DateTimeOffset.MinValue;
			}
		}

		public override String ToString()
		{
			return $"{this.Subject}\n\n{this.Body}";
		}
	}
}
