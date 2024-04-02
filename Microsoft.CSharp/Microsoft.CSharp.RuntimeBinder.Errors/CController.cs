namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal abstract class CController
{
	private CErrorFactory m_errorFactory;

	protected CController()
	{
		m_errorFactory = new CErrorFactory();
	}

	public abstract void SubmitError(CError pError);

	public CErrorFactory GetErrorFactory()
	{
		return m_errorFactory;
	}
}
