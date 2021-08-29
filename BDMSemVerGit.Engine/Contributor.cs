using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class Contributor
	{
		public String Name { get; set; }
		public String Email { get; set; }
		public Boolean IsEmpty => (String.IsNullOrEmpty(this.Name) && String.IsNullOrEmpty(this.Email));
	}
}
