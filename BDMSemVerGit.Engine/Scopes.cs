using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class Scopes
	{
		public String ScopesFilePath { get; set; }
		public List<String> AcceptableScops { get; set; }
		public Scopes(String scopesFilePath)
		{
			this.ScopesFilePath = scopesFilePath;
			this.AcceptableScops = new();
			if (!File.Exists(this.ScopesFilePath))
			{
				this.AcceptableScops.Add("<none>");
				this.Save();
			}
			else
				this.Open();
		}

		public void Save()
		{
			String directory = Path.GetDirectoryName(this.ScopesFilePath);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			File.WriteAllText(
				this.ScopesFilePath, 
				String.Join("\n", this.AcceptableScops)
			);
		}

		public void Add(String text)
		{
			this.AcceptableScops.Add(text);
			this.Save();
		}

		public Boolean IsValid(String text) => this.AcceptableScops
			.Any(s => s.Equals(text, StringComparison.Ordinal));

		public void Open()
		{
			if (File.Exists(this.ScopesFilePath))
			{
				this.AcceptableScops.AddRange(File.ReadAllLines(this.ScopesFilePath));
				if (!this.AcceptableScops.Any(s => s.Equals("<none>", StringComparison.Ordinal)))
					this.Add("<none>");
			}
		}

		public static Scopes Open(String gitDirectory)
			=> new(gitDirectory);
	}
}
