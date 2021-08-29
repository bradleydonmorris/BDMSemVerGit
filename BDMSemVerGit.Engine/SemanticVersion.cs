using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class SemanticVersion
	{
		public String Name { get; set; }
		public Int64 Major { get; set; }
		public Int64 Minor { get; set; }
		public Int64 Patch { get; set; }
		public SemanticVersion() { }
		public SemanticVersion(String name)
		{
			if (SemanticVersion.TryParse(name, out SemanticVersion semanticVersion))
			{
				this.Major = semanticVersion.Major;
				this.Minor = semanticVersion.Minor;
				this.Patch = semanticVersion.Patch;
				this.Name = this.ToString();
			}
			else throw new ArgumentOutOfRangeException(nameof(name));
		}

		public String NumericString => $"{this.Major}.{this.Minor}.{this.Patch}";
		public String AssemblyInfoString => $"[assembly: AssemblyVersion(\"{this.NumericString}.0\")]";

		public override String ToString() => $"v{this.Major}.{this.Minor}.{this.Patch}";

		public SemanticVersion Bump(String element)
		{
			SemanticVersion returnValue = new()
			{
				Major = this.Major,
				Minor = this.Minor,
				Patch = this.Patch
			};
			switch (element.ToLower())
			{
				case "major":
					returnValue.Major++;
					returnValue.Minor = 0;
					returnValue.Patch = 0;
					break;
				case "minor":
					returnValue.Minor++;
					returnValue.Patch = 0;
					break;
				case "patch":
				default:
					returnValue.Patch++;
					break;
			}
			returnValue.Name = returnValue.ToString();
			return returnValue;
		}

		public static Boolean TryParse(String text, out SemanticVersion semanticVersion)
		{
			Boolean returnValue = true;
			semanticVersion = new();
			if (text.StartsWith("v"))
				text = text[1..];
			String[] nameElements = text.Split('.');
			if (
				nameElements.Length != 3
				&& nameElements.Length != 4
			)
				returnValue = false;
			if (Int32.TryParse(nameElements[0], out Int32 major))
				semanticVersion.Major = major;
			if (Int32.TryParse(nameElements[1], out Int32 minor))
				semanticVersion.Minor = minor;
			if (Int32.TryParse(nameElements[2], out Int32 patch))
				semanticVersion.Patch = patch;
			if (
				nameElements.Length == 3
				&& !semanticVersion.ToString().Equals($"v{text}")
			)
				returnValue = false;
			else if (
				nameElements.Length == 4
				&& Int32.TryParse(nameElements[2], out Int32 fourth)
				&& !$"v{text}".Equals($"{semanticVersion}.{fourth}")
			)
				returnValue = false;
			semanticVersion.Name = semanticVersion.ToString();
			return returnValue;
		}
		public static SemanticVersion Parse(String text)
		{
			if (SemanticVersion.TryParse(text, out SemanticVersion semanticVersion))
				return semanticVersion;
			else return null;
		}
	}
}
