using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.Engine
{
	public class ConventionalCommit
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public CommitType Type { get; set; }
		public String Scope { get; set; }
		public String Summary { get; set; }
		public String Description { get; set; }
		public String BreakingChange { get; set; }
		public String[] References { get; set; }
		public Boolean IsEmpty => String.IsNullOrEmpty(this.Summary);
		public Boolean IsBreakingChange => !String.IsNullOrEmpty(this.BreakingChange);

		public String Subject
		{
			get
			{
				if (
					this.Type == CommitType.Invalid
					&& (
						String.IsNullOrEmpty(this.Scope)
						|| this.Scope.Equals("<none>")
					)
				)
					return this.Summary;
				else
					return (
						this.Type.ToString()
						+ (
							(!String.IsNullOrEmpty(this.Scope) && !this.Scope.Equals("<none>"))
								? $"({this.Scope})"
								: ""
						)
						+ $": {this.Summary}"
					);
			}
		}

		public String Body
		{
			get
			{
				return (
					(
						(!String.IsNullOrEmpty(this.Description))
							? $"{this.Description}" : ""
					)
					+ (
						(!String.IsNullOrEmpty(this.BreakingChange))
						? $"\n\nBREAKING CHANGE: {this.BreakingChange}" : ""
					)
					+ (
						(this.References != null && this.References.Length > -1)
							? $"\n\nRefs {String.Join(", ", this.References)}" : ""
					)
				);
			}
		}

		public List<ConventionalCommitMessageLine> GetMessageLines()
		{
			List<ConventionalCommitMessageLine> returnValue = new();
			Int32 lineNumber = 1;
			returnValue.Add(new() { LineNumber = lineNumber, Element = ConventionalCommitMessageElement.Subject, Text = this.Subject });

			lineNumber++;
			if (!String.IsNullOrEmpty(this.Description))
				returnValue.Add(new() { LineNumber = lineNumber, Element = ConventionalCommitMessageElement.Description, Text = this.Description });
			else
				returnValue.Add(ConventionalCommitMessageLine.GetSpacerLine(lineNumber));
			if (this.IsBreakingChange)
			{
				lineNumber++;
				returnValue.Add(ConventionalCommitMessageLine.GetSpacerLine(lineNumber));
				lineNumber++;
				returnValue.Add(new() { LineNumber = lineNumber, Element = ConventionalCommitMessageElement.BreakingChangeSummary, Text = $"BREAKING CHANGE: {this.BreakingChange}" });
				lineNumber++;
				returnValue.Add(ConventionalCommitMessageLine.GetSpacerLine(lineNumber));
			}
			if (this.References != null && this.References.Length > -1)
			{
				lineNumber++;
				returnValue.Add(ConventionalCommitMessageLine.GetSpacerLine(lineNumber));
				lineNumber++;
				returnValue.Add(new() { LineNumber = lineNumber, Element = ConventionalCommitMessageElement.References, Text = $"Fixes {String.Join(", ", this.References)}" });
			}
			return returnValue;
		}

		public override String ToString()
		{
			return $"{this.Subject}\n\n{this.Body}";
		}

		public String ToGitCommitMessage()
		{
			return $"-m \"{this.ToString().Replace("\n", "\" -m \"")}\"";
		}

		public ConventionalCommit()
		{
			this.Type = CommitType.Invalid;
		}

		public void SetReferences(String references)
		{
			if (!String.IsNullOrEmpty(references))
			{
				references = references
					.Replace(" ", "|")
					.Replace(",", "|");
				while (references.Contains("||"))
					references = references.Replace("||", "|");
				this.References = references.Split('|');
			}
		}

		public static ConventionalCommit Parse(String subject, String body)
		{
			String type = null;
			String scope = null;
			String summary = null;
			String description = null;
			String bcSummary = null;
			String[] refs = null;
			ConventionalCommit returnValue = new();
			if (String.IsNullOrEmpty(subject) || subject.StartsWith("Merged "))
				returnValue = null;
			else
			{
				if (!String.IsNullOrEmpty(subject))
				{
					type = "fix";
					if (subject.Contains("("))
					{
						type = subject.Substring(0, subject.IndexOf("(")).Trim();
						scope = subject.Substring(subject.IndexOf("(") + 1, (subject.IndexOf(")") - subject.IndexOf("(") - 1)).Trim();
					}
					else if (subject.Contains(":"))
						type = subject.Substring(0, subject.IndexOf(":")).Trim();
					if (subject.StartsWith($"{type}:"))
						summary = subject.Remove(0, $"{type}:".Length).Trim();
					if (subject.StartsWith($"{type}({scope}):"))
						summary = subject.Remove(0, $"{type}({scope}):".Length).Trim();
				}
				if (!String.IsNullOrEmpty(body))
				{
					body = body.Replace("\r\n", "\n");
					String[] lines = body.Split('\n');
					Int32 descriptionLine = -1;
					Int32 bcSummaryLine = -1;
					Int32 refsLine = -1;
					for (Int32 loop = 0; loop < lines.Length; loop++)
					{
						if (lines[loop].Length > 0)
						{
							if (
								(loop == 0 || loop == 1)
								&& (
									!lines[loop].ToUpper().StartsWith("BREAKING CHANGE")
									&& !lines[loop].ToUpper().StartsWith("FIXES")
									&& !lines[loop].ToUpper().StartsWith("ISSUES")
									&& !lines[loop].ToUpper().StartsWith("REFS")
								)
							)
								descriptionLine = loop;
							else
							{
								if (lines[loop].ToUpper().StartsWith("BREAKING CHANGE"))
								{
									bcSummaryLine = loop;
								}
								if (
									lines[loop].ToUpper().StartsWith("FIXES")
									|| lines[loop].ToUpper().StartsWith("ISSUES")
									|| lines[loop].ToUpper().StartsWith("CLOSES")
									|| lines[loop].ToUpper().StartsWith("REFS")
								)
									refsLine = loop;
							}
						}
					}
					if (descriptionLine > -1 && !String.IsNullOrEmpty(lines[descriptionLine]))
						description = lines[descriptionLine];
					if (bcSummaryLine > -1 && !String.IsNullOrEmpty(lines[bcSummaryLine]))
						bcSummary = lines[bcSummaryLine][16..].Trim();
					if (refsLine > -1 && !String.IsNullOrEmpty(lines[refsLine]))
					{
						String references = lines[refsLine][lines[refsLine].IndexOf(" ")..].Trim();
						references = references
							.Replace(" ", "|")
							.Replace(",", "|");
						while (references.Contains("||"))
							references = references.Replace("||", "|");
						refs = references.Split('|');
					}
					returnValue.Summary = summary;
					if (Enum.TryParse<CommitType>(type, out CommitType result))
						returnValue.Type = (CommitType)result;
					returnValue.Scope = scope;
					returnValue.Description = description;
					returnValue.BreakingChange = bcSummary;
					returnValue.References = refs;
				}
			}
			return returnValue;
		}

		public String ToXMLString()
		{
			System.Xml.Serialization.XmlSerializer xmlSerializer = new(this.GetType());
			using (System.IO.StringWriter stringWriter = new())
			{
				xmlSerializer.Serialize(stringWriter, this);
				return stringWriter.ToString();
			}
		}

		public String ToJSONString()
		{
			return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
			{
				DateFormatHandling = DateFormatHandling.IsoDateFormat,
				DateParseHandling = DateParseHandling.DateTimeOffset,
				DateTimeZoneHandling = DateTimeZoneHandling.Utc,
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Include,
				TypeNameHandling = TypeNameHandling.Auto
			});
		}
	}
}
