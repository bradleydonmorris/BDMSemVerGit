using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BDMSemVerGit.Engine
{
	public class FileVersions
	{
		private static String GetParentProject(String filePath)
		{
			String returnValue = null;

			if (filePath.EndsWith("\\Properties\\AssemblyInfo.cs"))
			{
				String projectDirectoryPath = filePath.Remove(filePath.LastIndexOf("\\Properties\\AssemblyInfo.cs"));
				foreach (String projFilePath in Directory.GetFiles(projectDirectoryPath, "*.csproj", SearchOption.TopDirectoryOnly))
				{
					returnValue = Path.GetFileNameWithoutExtension(projFilePath);
					break;
				}
			}

			else if (filePath.EndsWith(".publish.xml"))
			{
				//Assumes a directory convention of...
				//	{ProjectRoot}/PublishProfiles/{ProfileName}.publish.xml

				//One Level Up,
				//	Likely in the "PublishProlifes" directory
				String projectDirectoryPath = Path.GetDirectoryName(filePath);
				foreach (String projFilePath in Directory.GetFiles(projectDirectoryPath, "*.sqlproj", SearchOption.TopDirectoryOnly))
				{
					returnValue = Path.GetFileNameWithoutExtension(projFilePath);
					break;
				}
				if (returnValue == null)
				{
					//One More Level Up,
					//	Likely in the {ProjectRoot} directory
					projectDirectoryPath = Path.GetDirectoryName(projectDirectoryPath);
					foreach (String projFilePath in Directory.GetFiles(projectDirectoryPath, "*.sqlproj", SearchOption.TopDirectoryOnly))
					{
						returnValue = Path.GetFileNameWithoutExtension(projFilePath);
						break;
					}
				}
			}
			return returnValue;
		}

		public static List<ProjectFileVersion> GetVersions(String repoDirectory)
		{
			List<ProjectFileVersion> returnValue = new();
			
			//C# Project Files
			foreach (String filePath in Directory.GetFiles(repoDirectory, "*.csproj", SearchOption.AllDirectories))
			{
				ProjectFileVersion projectFileVersion = new()
				{
					AlterVersion = true,
					ProjectName = Path.GetFileNameWithoutExtension(filePath),
					RelativePath = Path.GetRelativePath(repoDirectory, filePath),
					FilePath = filePath
				};
				XDocument xDocument = XDocument.Load(filePath);
				projectFileVersion.LocationInFile = "//Project/PropertyGroup/Version";
				XElement versionElement = xDocument.XPathSelectElement(projectFileVersion.LocationInFile);
				if (versionElement != null)
					projectFileVersion.CurrentVersion = SemanticVersion.Parse(versionElement.Value);
				else
					projectFileVersion.LocationInFile = null;
				returnValue.Add(projectFileVersion);
			}

			//C# AssemblyInfo Files
			foreach (String filePath in Directory.GetFiles(repoDirectory, "AssemblyInfo.cs", SearchOption.AllDirectories))
			{
				ProjectFileVersion projectFileVersion = new()
				{
					AlterVersion = true,
					ProjectName = FileVersions.GetParentProject(filePath),
					RelativePath = Path.GetRelativePath(repoDirectory, filePath),
					FilePath = filePath
				};
				projectFileVersion.LocationInFile = File.ReadLines(filePath)
					.FirstOrDefault(l => l.StartsWith("[assembly: AssemblyVersion("));
				if (!String.IsNullOrWhiteSpace(projectFileVersion.LocationInFile))
					projectFileVersion.CurrentVersion = SemanticVersion.Parse(projectFileVersion.LocationInFile[28..^3]);
				else
					projectFileVersion.LocationInFile = null;
			}

			//SQL Database Project Files
			foreach (String filePath in Directory.GetFiles(repoDirectory, "*.sqlproj", SearchOption.AllDirectories))
			{
				//DACPAC Version
				ProjectFileVersion projectFileVersionDACPACVersion = new()
				{
					AlterVersion = true,
					ProjectName = Path.GetFileNameWithoutExtension(filePath),
					RelativePath = Path.GetRelativePath(repoDirectory, filePath),
					FilePath = filePath
				};
				XDocument xDocumentDACPACVersion = XDocument.Load(filePath);
				projectFileVersionDACPACVersion.LocationInFile = "//Project/PropertyGroup/DacVersion";
				XElement versionElementDACPACVersion = xDocumentDACPACVersion
					.XPathSelectElement(projectFileVersionDACPACVersion.LocationInFile);
				if (versionElementDACPACVersion != null)
					projectFileVersionDACPACVersion.CurrentVersion = SemanticVersion.Parse(versionElementDACPACVersion.Value);
				else
					projectFileVersionDACPACVersion.LocationInFile = null;
				returnValue.Add(projectFileVersionDACPACVersion);

				//DatabaseVersion variable
				ProjectFileVersion projectFileVersionDatabaseVersion = new()
				{
					AlterVersion = true,
					ProjectName = Path.GetFileNameWithoutExtension(filePath),
					RelativePath = Path.GetRelativePath(repoDirectory, filePath),
					FilePath = filePath
				};
				XDocument xDocumentDatabaseVersion = XDocument.Load(filePath);
				projectFileVersionDatabaseVersion.LocationInFile =
					"//*[local-name()='Project']" +
					"/*[local-name()='ItemGroup']" +
					"/*[local-name()='SqlCmdVariable' and @Include='DatabaseVersion']" +
					"/*[local-name()='DefaultValue']";
				XElement versionElementDatabaseVersion = xDocumentDatabaseVersion
					.XPathSelectElement(projectFileVersionDatabaseVersion.LocationInFile);
				if (versionElementDatabaseVersion != null)
					projectFileVersionDatabaseVersion.CurrentVersion = SemanticVersion.Parse(versionElementDatabaseVersion.Value);
				else
					projectFileVersionDatabaseVersion.LocationInFile = null;
				returnValue.Add(projectFileVersionDatabaseVersion);
			}

			//SQL Database Publish Profiles
			foreach (String filePath in Directory.GetFiles(repoDirectory, "*.publish.xml", SearchOption.AllDirectories))
			{
				//DatabaseVersion variable
				ProjectFileVersion projectFileVersion = new()
				{
					AlterVersion = true,
					ProjectName = FileVersions.GetParentProject(filePath),
					RelativePath = Path.GetRelativePath(repoDirectory, filePath),
					FilePath = filePath
				};
				XDocument xDocument = XDocument.Load(filePath);
				projectFileVersion.LocationInFile =
					"//*[local-name()='Project']" +
					"/*[local-name()='ItemGroup']" +
					"/*[local-name()='SqlCmdVariable' and @Include='DatabaseVersion']" +
					"/*[local-name()='Value']";
				XElement versionElement = xDocument
					.XPathSelectElement(projectFileVersion.LocationInFile);
				if (versionElement != null)
					projectFileVersion.CurrentVersion = SemanticVersion.Parse(versionElement.Value);
				else
					projectFileVersion.LocationInFile = null;
				returnValue.Add(projectFileVersion);
			}

			return returnValue;
		}

		public static void SetVersion(ProjectFileVersion projectFileVersion)
		{
			if (projectFileVersion.FileType == ProjectFileVersionType.AssemblyInfo)
			{
				String[] lines = File.ReadAllLines(projectFileVersion.FilePath);
				for (Int32 index = 0; index < lines.Length; index++)
					if (lines[index] == projectFileVersion.LocationInFile)
						lines[index] = projectFileVersion.NewVersion.AssemblyInfoString;
				File.WriteAllLines(projectFileVersion.FilePath, lines);
			}
			else if (projectFileVersion.FileType == ProjectFileVersionType.Xml)
			{
				XDocument xDocument = XDocument.Load(projectFileVersion.FilePath);
				XElement versionElement = xDocument
					.XPathSelectElement(projectFileVersion.LocationInFile);
				if (versionElement != null)
				{
					versionElement.Value = projectFileVersion.NewVersion.NumericString;
					xDocument.Save(projectFileVersion.FilePath);
				}
			}
		}
	}
}
