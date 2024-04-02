using System.Globalization;

namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class CError
{
	private string m_text;

	public string Text => m_text;

	private static string ComputeString(ErrorCode code, string[] args)
	{
		return string.Format(CultureInfo.InvariantCulture, ErrorFacts.GetMessage(code), args);
	}

	public void Initialize(ErrorCode code, string[] args)
	{
		m_text = ComputeString(code, args);
	}
}
