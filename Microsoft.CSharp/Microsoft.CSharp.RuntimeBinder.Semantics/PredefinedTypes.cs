using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class PredefinedTypes
{
	private SymbolTable runtimeBinderSymbolTable;

	private BSYMMGR pBSymmgr;

	private AggregateSymbol[] predefSyms;

	private KAID aidMsCorLib;

	private static readonly char[] nameSeparators = new char[1] { '.' };

	public PredefinedTypes(BSYMMGR pBSymmgr)
	{
		this.pBSymmgr = pBSymmgr;
		aidMsCorLib = KAID.kaidNil;
		runtimeBinderSymbolTable = null;
	}

	private AggregateSymbol DelayLoadPredefSym(PredefinedType pt)
	{
		CType cTypeFromType = runtimeBinderSymbolTable.GetCTypeFromType(PredefinedTypeFacts.GetAssociatedSystemType(pt));
		AggregateSymbol aggregate = cTypeFromType.getAggregate();
		if (aggregate == null)
		{
			return null;
		}
		return InitializePredefinedType(aggregate, pt);
	}

	internal static AggregateSymbol InitializePredefinedType(AggregateSymbol sym, PredefinedType pt)
	{
		sym.SetPredefined(predefined: true);
		sym.SetPredefType(pt);
		sym.SetSkipUDOps(pt <= PredefinedType.PT_ENUM && pt != PredefinedType.PT_INTPTR && pt != PredefinedType.PT_UINTPTR && pt != PredefinedType.PT_TYPE);
		return sym;
	}

	public bool Init(ErrorHandling errorContext, SymbolTable symtable)
	{
		runtimeBinderSymbolTable = symtable;
		if (aidMsCorLib == KAID.kaidNil)
		{
			AggregateSymbol aggregateSymbol = FindPredefinedType(errorContext, PredefinedTypeFacts.GetName(PredefinedType.PT_OBJECT), KAID.kaidGlobal, AggKindEnum.Class, 0, isRequired: true);
			if (aggregateSymbol == null)
			{
				return false;
			}
			aidMsCorLib = aggregateSymbol.GetAssemblyID();
		}
		predefSyms = new AggregateSymbol[138];
		return true;
	}

	private AggregateSymbol FindPredefinedType(ErrorHandling errorContext, string pszType, KAID aid, AggKindEnum aggKind, int arity, bool isRequired)
	{
		NamespaceOrAggregateSymbol namespaceOrAggregateSymbol = pBSymmgr.GetRootNS();
		Name name = null;
		string[] array = pszType.Split(nameSeparators);
		int i = 0;
		for (int num = array.Length; i < num; i++)
		{
			name = pBSymmgr.GetNameManager().Add(array[i]);
			if (i == num - 1)
			{
				break;
			}
			AggregateSymbol aggregateSymbol = pBSymmgr.LookupGlobalSymCore(name, namespaceOrAggregateSymbol, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol();
			if (aggregateSymbol != null && aggregateSymbol.InAlias(aid) && aggregateSymbol.IsPredefined())
			{
				namespaceOrAggregateSymbol = aggregateSymbol;
				continue;
			}
			NamespaceSymbol namespaceSymbol = pBSymmgr.LookupGlobalSymCore(name, namespaceOrAggregateSymbol, symbmask_t.MASK_NamespaceSymbol).AsNamespaceSymbol();
			bool flag = true;
			if (!(namespaceSymbol?.InAlias(aid) ?? false))
			{
				if (isRequired)
				{
					errorContext.Error(ErrorCode.ERR_PredefinedTypeNotFound, pszType);
				}
				return null;
			}
			namespaceOrAggregateSymbol = namespaceSymbol;
		}
		AggregateSymbol paggBad;
		AggregateSymbol paggAmbig;
		AggregateSymbol aggregateSymbol2 = FindPredefinedTypeCore(name, namespaceOrAggregateSymbol, aid, aggKind, arity, out paggAmbig, out paggBad);
		if (aggregateSymbol2 == null)
		{
			if (paggBad != null && (isRequired || (aid == KAID.kaidGlobal && paggBad.IsSource())))
			{
				errorContext.ErrorRef(ErrorCode.ERR_PredefinedTypeBadType, paggBad);
			}
			else if (isRequired)
			{
				errorContext.Error(ErrorCode.ERR_PredefinedTypeNotFound, pszType);
			}
			return null;
		}
		if (paggAmbig == null && aid != 0)
		{
			AggregateSymbol paggBad2;
			AggregateSymbol aggregateSymbol3 = FindPredefinedTypeCore(name, namespaceOrAggregateSymbol, KAID.kaidGlobal, aggKind, arity, out paggAmbig, out paggBad2);
			if (aggregateSymbol3 != aggregateSymbol2)
			{
				paggAmbig = aggregateSymbol3;
			}
		}
		return aggregateSymbol2;
	}

	private AggregateSymbol FindPredefinedTypeCore(Name name, NamespaceOrAggregateSymbol bag, KAID aid, AggKindEnum aggKind, int arity, out AggregateSymbol paggAmbig, out AggregateSymbol paggBad)
	{
		AggregateSymbol aggregateSymbol = null;
		paggAmbig = null;
		paggBad = null;
		for (AggregateSymbol aggregateSymbol2 = pBSymmgr.LookupGlobalSymCore(name, bag, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol(); aggregateSymbol2 != null; aggregateSymbol2 = BSYMMGR.LookupNextSym(aggregateSymbol2, bag, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol())
		{
			if (aggregateSymbol2.InAlias(aid) && aggregateSymbol2.GetTypeVarsAll().size == arity)
			{
				if (aggregateSymbol2.AggKind() != aggKind)
				{
					if (paggBad == null)
					{
						paggBad = aggregateSymbol2;
					}
				}
				else
				{
					if (aggregateSymbol != null)
					{
						paggAmbig = aggregateSymbol2;
						break;
					}
					aggregateSymbol = aggregateSymbol2;
					if (paggAmbig == null)
					{
						break;
					}
				}
			}
		}
		return aggregateSymbol;
	}

	public void ReportMissingPredefTypeError(ErrorHandling errorContext, PredefinedType pt)
	{
		errorContext.Error(ErrorCode.ERR_PredefinedTypeNotFound, PredefinedTypeFacts.GetName(pt));
	}

	public AggregateSymbol GetReqPredefAgg(PredefinedType pt)
	{
		if (!PredefinedTypeFacts.IsRequired(pt))
		{
			throw Error.InternalCompilerError();
		}
		if (predefSyms[(uint)pt] == null)
		{
			predefSyms[(uint)pt] = DelayLoadPredefSym(pt);
		}
		return predefSyms[(uint)pt];
	}

	public AggregateSymbol GetOptPredefAgg(PredefinedType pt)
	{
		if (predefSyms[(uint)pt] == null)
		{
			predefSyms[(uint)pt] = DelayLoadPredefSym(pt);
		}
		return predefSyms[(uint)pt];
	}

	public static string GetNiceName(PredefinedType pt)
	{
		return PredefinedTypeFacts.GetNiceName(pt);
	}

	public static string GetNiceName(AggregateSymbol type)
	{
		if (type.IsPredefined())
		{
			return GetNiceName(type.GetPredefType());
		}
		return null;
	}

	public static string GetFullName(PredefinedType pt)
	{
		return PredefinedTypeFacts.GetName(pt);
	}

	public static bool isRequired(PredefinedType pt)
	{
		return PredefinedTypeFacts.IsRequired(pt);
	}
}
