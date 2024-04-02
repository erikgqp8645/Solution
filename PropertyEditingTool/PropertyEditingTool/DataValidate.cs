using System.Text.RegularExpressions;

namespace PropertyEditingTool;

public class DataValidate
{
	public static bool IsPositive(string txt)
	{
		return new Regex("^[0-9]*[1-9][0-9]*$").IsMatch(txt);
	}

	public static bool IsFun(string txt)
	{
		return new Regex("{[0-9]*}").IsMatch(txt);
	}

	public static string GetStartNum(string txt)
	{
		return new Regex("{[0-9]*}").Matches(txt)[0].ToString();
	}

	public static string ExtractChinese(string fileName)
	{
		string middle = "";
		string result = "";
		foreach (Match item in new Regex("[一-龥]+").Matches(fileName))
		{
			middle = item.ToString();
			result += middle;
		}
		return result;
	}
}
