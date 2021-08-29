using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class Tag
	{
		public Dictionary<String, Contributor> Contributors { get; set; }
		public Dictionary<String, DateTimeOffset> ContributorDates { get; set; }
		public String Ref { get; set; }
		public String SHA { get; set; }
		public Commit Commit { get; set; }
		public String Subject { get; set; }
		public String Body { get; set; }
		public Tag()
		{
			this.Contributors = new();
			this.ContributorDates = new();
		}

		public Boolean IsSemanticVersionTag => Regex.IsMatch(this.Name, "^v([0-9]+)\\.([0-9]+)\\.([0-9]+)");

		public String Name { get; set; }

		public DateTimeOffset Date
		{
			get
			{
				if (this.ContributorDates.ContainsKey("Tagger"))
					return this.ContributorDates["Tagger"];
				else if (this.ContributorDates.ContainsKey("Author"))
					return this.ContributorDates["Author"];
				else if (this.ContributorDates.ContainsKey("Committer"))
					return this.ContributorDates["Committer"];
				else return DateTimeOffset.MinValue;
			}
		}

		public override String ToString()
		{
			return this.Name;
		}
	}
}
