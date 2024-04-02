namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class CParameterizedError
{
	private ErrorCode m_errorNumber;

	private ErrArg[] m_arguments;

	public void Initialize(ErrorCode errorNumber, ErrArg[] arguments)
	{
		m_errorNumber = errorNumber;
		m_arguments = (ErrArg[])arguments.Clone();
	}

	public int GetParameterCount()
	{
		return m_arguments.Length;
	}

	public ErrArg GetParameter(int index)
	{
		return m_arguments[index];
	}

	public ErrorCode GetErrorNumber()
	{
		return m_errorNumber;
	}
}
