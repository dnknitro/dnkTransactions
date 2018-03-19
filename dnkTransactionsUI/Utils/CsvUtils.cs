using System.Collections.Generic;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace dnkTransactionsUI.Utils
{
	internal class CSVUtils
	{
		public static string StringToCSVCell(string str)
		{
			if (str == null)
				str = string.Empty;
			var mustQuote = str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n");
			if (mustQuote)
			{
				var sb = new StringBuilder();
				sb.Append("\"");
				foreach (var nextChar in str)
				{
					sb.Append(nextChar);
					if (nextChar == '"')
						sb.Append("\"");
				}
				sb.Append("\"");
				return sb.ToString();
			}

			return str;
		}

		public static IEnumerable<string[]> ReadCSVFile(string file)
		{
			using (var parser = new TextFieldParser(file))
			{
				parser.HasFieldsEnclosedInQuotes = true;
				parser.SetDelimiters(",");

				while (!parser.EndOfData)
					yield return parser.ReadFields();
			}
		}
	}
}