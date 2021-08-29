using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.CLI
{
	public static class Extensions
	{
		public static String Replicate(this String value, Int32 count)
		{
			String returnValue = String.Empty;
			for (Int32 loop = 1; loop <= count; loop++)
				returnValue += value;
			return returnValue;
		}

		public static String NullIf(this String value, String condition) => (
				String.IsNullOrWhiteSpace(value)
					? null
					: (
						value.Equals(condition)
						? null
						: value
					)
			);

		public static String IsEmpty(this String value, String defaultValue) => (
				String.IsNullOrWhiteSpace(value)
					? defaultValue
					: value
			);

		public static DateTimeOffset ParseGitDate(this String value, String format)
		{
			if (String.IsNullOrWhiteSpace(value))
				return DateTimeOffset.MinValue;
			if (format.EndsWith("ParseK"))
			{
				//        2021-08-11 17:25:46 -0500
				format = "yyyy-MM-dd HH:mm:ssK";
				value = value[..19] + "+" + value[21..].Insert(2, ":");
			}
			return DateTimeOffset.ParseExact(value.Trim(), format, null).ToUniversalTime();
		}


	}
}
