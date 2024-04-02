namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal struct AidContainer
{
	private enum Kind
	{
		None,
		File,
		ExternAlias
	}

	internal static readonly AidContainer NullAidContainer;

	private object m_value;

	public AidContainer(FileRecord file)
	{
		m_value = file;
	}
}
