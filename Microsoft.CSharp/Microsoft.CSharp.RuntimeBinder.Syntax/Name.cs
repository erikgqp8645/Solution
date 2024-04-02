namespace Microsoft.CSharp.RuntimeBinder.Syntax;

internal class Name
{
	private string _text;

	public string Text => _text;

	public virtual PredefinedName PredefinedName => PredefinedName.PN_COUNT;

	public Name(string text)
	{
		_text = text;
	}

	public override string ToString()
	{
		return _text;
	}
}
