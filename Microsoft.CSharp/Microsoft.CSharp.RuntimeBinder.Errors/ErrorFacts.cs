namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal static class ErrorFacts
{
	public static string GetMessage(ErrorCode code)
	{
		string text = code.ToString();
		if (text == null)
		{
			return null;
		}
		if (text.Length <= 4)
		{
			return null;
		}
		return SR.GetString(text.Substring(4));
	}

	public static string GetMessage(MessageID id)
	{
		return SR.GetString(id.ToString());
	}
}
