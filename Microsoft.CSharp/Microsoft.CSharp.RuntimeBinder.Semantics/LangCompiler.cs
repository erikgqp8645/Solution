using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class LangCompiler : CSemanticChecker, IErrorSink
{
	private SymbolLoader m_symbolLoader;

	private CController pController;

	private ErrorHandling m_errorContext;

	private GlobalSymbolContext globalSymbolContext;

	private UserStringBuilder m_userStringBuilder;

	public override SymbolLoader SymbolLoader => m_symbolLoader;

	public LangCompiler(CController pCtrl, NameManager pNameMgr)
	{
		pController = pCtrl;
		globalSymbolContext = new GlobalSymbolContext(pNameMgr);
		m_userStringBuilder = new UserStringBuilder(globalSymbolContext);
		m_errorContext = new ErrorHandling(m_userStringBuilder, this, pCtrl.GetErrorFactory());
		m_symbolLoader = new SymbolLoader(globalSymbolContext, null, m_errorContext);
	}

	public new ErrorHandling GetErrorContext()
	{
		return m_errorContext;
	}

	public override SymbolLoader GetSymbolLoader()
	{
		return m_symbolLoader;
	}

	public void SubmitError(CParameterizedError error)
	{
		CError cError = GetErrorContext().RealizeError(error);
		if (cError != null)
		{
			pController.SubmitError(cError);
		}
	}

	public int ErrorCount()
	{
		return 0;
	}
}
