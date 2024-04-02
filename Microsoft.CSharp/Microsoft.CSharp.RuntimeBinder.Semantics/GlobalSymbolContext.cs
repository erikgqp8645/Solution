using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class GlobalSymbolContext
{
	private PredefinedTypes m_predefTypes;

	private NameManager m_nameManager;

	public TypeManager TypeManager { get; private set; }

	public BSYMMGR GlobalSymbols { get; private set; }

	public GlobalSymbolContext(NameManager namemgr)
	{
		TypeManager = new TypeManager();
		GlobalSymbols = new BSYMMGR(namemgr, TypeManager);
		m_predefTypes = new PredefinedTypes(GlobalSymbols);
		TypeManager.Init(GlobalSymbols, m_predefTypes);
		GlobalSymbols.Init();
		m_nameManager = namemgr;
	}

	public TypeManager GetTypes()
	{
		return TypeManager;
	}

	public BSYMMGR GetGlobalSymbols()
	{
		return GlobalSymbols;
	}

	public NameManager GetNameManager()
	{
		return m_nameManager;
	}

	public PredefinedTypes GetPredefTypes()
	{
		return m_predefTypes;
	}

	public SymFactory GetGlobalSymbolFactory()
	{
		return GetGlobalSymbols().GetSymFactory();
	}

	public MiscSymFactory GetGlobalMiscSymFactory()
	{
		return GetGlobalSymbols().GetMiscSymFactory();
	}
}
