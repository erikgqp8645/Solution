using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MemberLookup
{
	private CSemanticChecker m_pSemanticChecker;

	private SymbolLoader m_pSymbolLoader;

	private CType m_typeSrc;

	private EXPR m_obj;

	private CType m_typeQual;

	private ParentSymbol m_symWhere;

	private Name m_name;

	private int m_arity;

	private MemLookFlags m_flags;

	private CMemberLookupResults m_results;

	private List<AggregateType> m_rgtypeStart;

	private List<AggregateType> m_prgtype;

	private int m_csym;

	private SymWithType m_swtFirst;

	private List<MethPropWithType> m_methPropWithTypeList;

	private SymWithType m_swtAmbig;

	private SymWithType m_swtInaccess;

	private SymWithType m_swtBad;

	private SymWithType m_swtBogus;

	private SymWithType m_swtBadArity;

	private SymWithType m_swtAmbigWarn;

	private SymWithType m_swtOverride;

	private bool m_fMulti;

	private void RecordType(AggregateType type, Symbol sym)
	{
		if (!m_prgtype.Contains(type))
		{
			m_prgtype.Add(type);
		}
		m_csym++;
		if (m_swtFirst == null)
		{
			m_swtFirst.Set(sym, type);
			m_fMulti = sym.IsMethodSymbol() || (sym.IsPropertySymbol() && sym.AsPropertySymbol().isIndexer());
		}
	}

	private bool SearchSingleType(AggregateType typeCur, out bool pfHideByName)
	{
		bool result = false;
		pfHideByName = false;
		bool flag = !GetSemanticChecker().CheckTypeAccess(typeCur, m_symWhere);
		if (flag && (m_csym != 0 || m_swtInaccess != null))
		{
			return false;
		}
		Symbol symbol = null;
		symbol = GetSymbolLoader().LookupAggMember(m_name, typeCur.getAggregate(), symbmask_t.MASK_ALL);
		while (true)
		{
			if (symbol != null)
			{
				switch (symbol.getKind())
				{
				case SYMKIND.SK_MethodSymbol:
					if (m_arity > 0 && symbol.AsMethodSymbol().typeVars.size != m_arity)
					{
						if (!m_swtBadArity)
						{
							m_swtBadArity.Set(symbol, typeCur);
						}
						goto IL_056f;
					}
					break;
				case SYMKIND.SK_AggregateSymbol:
					if (symbol.AsAggregateSymbol().GetTypeVars().size != m_arity)
					{
						if (!m_swtBadArity)
						{
							m_swtBadArity.Set(symbol, typeCur);
						}
						goto IL_056f;
					}
					break;
				case SYMKIND.SK_TypeParameterSymbol:
					if ((m_flags & MemLookFlags.TypeVarsAllowed) == 0)
					{
						goto IL_056f;
					}
					if (m_arity > 0)
					{
						if (!m_swtBadArity)
						{
							m_swtBadArity.Set(symbol, typeCur);
						}
						goto IL_056f;
					}
					break;
				default:
					if (m_arity > 0)
					{
						if (!m_swtBadArity)
						{
							m_swtBadArity.Set(symbol, typeCur);
						}
						goto IL_056f;
					}
					break;
				}
				if (symbol.IsOverride() && !symbol.IsHideByName())
				{
					if (!m_swtOverride)
					{
						m_swtOverride.Set(symbol, typeCur);
					}
				}
				else
				{
					if ((m_flags & MemLookFlags.UserCallable) != 0 && symbol.IsMethodOrPropertySymbol() && !symbol.AsMethodOrPropertySymbol().isUserCallable())
					{
						bool flag2 = false;
						if (symbol.IsMethodSymbol() && symbol.AsMethodSymbol().isPropertyAccessor() && ((symbol.name.Text.StartsWith("set_", StringComparison.Ordinal) && symbol.AsMethodSymbol().Params.size > 1) || (symbol.name.Text.StartsWith("get_", StringComparison.Ordinal) && symbol.AsMethodSymbol().Params.size > 0)))
						{
							flag2 = true;
						}
						if (!flag2)
						{
							if (!m_swtInaccess)
							{
								m_swtInaccess.Set(symbol, typeCur);
							}
							goto IL_056f;
						}
					}
					if (flag || !GetSemanticChecker().CheckAccess(symbol, typeCur, m_symWhere, m_typeQual))
					{
						if (!m_swtInaccess)
						{
							m_swtInaccess.Set(symbol, typeCur);
						}
						if (flag)
						{
							return false;
						}
					}
					else if ((m_flags & MemLookFlags.Ctor) == 0 != (!symbol.IsMethodSymbol() || !symbol.AsMethodSymbol().IsConstructor()) || (m_flags & MemLookFlags.Operator) == 0 != (!symbol.IsMethodSymbol() || !symbol.AsMethodSymbol().isOperator) || (m_flags & MemLookFlags.Indexer) == 0 != (!symbol.IsPropertySymbol() || !symbol.AsPropertySymbol().isIndexer()))
					{
						if (!m_swtBad)
						{
							m_swtBad.Set(symbol, typeCur);
						}
					}
					else if (!symbol.IsMethodSymbol() && (m_flags & MemLookFlags.Indexer) == 0 && GetSemanticChecker().CheckBogus(symbol))
					{
						if (!m_swtBogus)
						{
							m_swtBogus.Set(symbol, typeCur);
						}
					}
					else if ((m_flags & MemLookFlags.MustBeInvocable) != 0 && ((symbol.IsFieldSymbol() && !IsDelegateType(symbol.AsFieldSymbol().GetType(), typeCur) && !IsDynamicMember(symbol)) || (symbol.IsPropertySymbol() && !IsDelegateType(symbol.AsPropertySymbol().RetType, typeCur) && !IsDynamicMember(symbol))))
					{
						if (!m_swtBad)
						{
							m_swtBad.Set(symbol, typeCur);
						}
					}
					else
					{
						if (symbol.IsMethodOrPropertySymbol())
						{
							MethPropWithType item = new MethPropWithType(symbol.AsMethodOrPropertySymbol(), typeCur);
							m_methPropWithTypeList.Add(item);
						}
						result = true;
						if ((bool)m_swtFirst)
						{
							if (!typeCur.isInterfaceType())
							{
								if (!m_fMulti)
								{
									if ((!m_swtFirst.Sym.IsFieldSymbol() || !symbol.IsEventSymbol() || !m_swtFirst.Field().isEvent) && (!m_swtFirst.Sym.IsFieldSymbol() || !symbol.IsEventSymbol()))
									{
										break;
									}
									goto IL_056f;
								}
								if (m_swtFirst.Sym.getKind() != symbol.getKind())
								{
									if (typeCur == m_prgtype[0])
									{
										break;
									}
									pfHideByName = true;
									goto IL_056f;
								}
							}
							else if (!m_fMulti)
							{
								if (!symbol.IsMethodSymbol())
								{
									break;
								}
								m_swtAmbigWarn = m_swtFirst;
								m_prgtype = new List<AggregateType>();
								m_csym = 0;
								m_swtFirst.Clear();
								m_swtAmbig.Clear();
							}
							else if (m_swtFirst.Sym.getKind() != symbol.getKind())
							{
								if (!typeCur.fDiffHidden)
								{
									if (!m_swtFirst.Sym.IsMethodSymbol())
									{
										break;
									}
									if (!m_swtAmbigWarn)
									{
										m_swtAmbigWarn.Set(symbol, typeCur);
									}
								}
								pfHideByName = true;
								goto IL_056f;
							}
						}
						RecordType(typeCur, symbol);
						if (symbol.IsMethodOrPropertySymbol() && symbol.AsMethodOrPropertySymbol().isHideByName)
						{
							pfHideByName = true;
						}
					}
				}
				goto IL_056f;
			}
			return result;
			IL_056f:
			symbol = GetSymbolLoader().LookupNextSym(symbol, typeCur.getAggregate(), symbmask_t.MASK_ALL);
		}
		if (!m_swtAmbig)
		{
			m_swtAmbig.Set(symbol, typeCur);
		}
		pfHideByName = true;
		return true;
	}

	private bool IsDynamicMember(Symbol sym)
	{
		DynamicAttribute dynamicAttribute = null;
		if (sym.IsFieldSymbol())
		{
			if (!sym.AsFieldSymbol().getType().isPredefType(PredefinedType.PT_OBJECT))
			{
				return false;
			}
			object[] customAttributes = sym.AsFieldSymbol().AssociatedFieldInfo.GetCustomAttributes(typeof(DynamicAttribute), inherit: false);
			if (customAttributes.Length == 1)
			{
				dynamicAttribute = customAttributes[0] as DynamicAttribute;
			}
		}
		else
		{
			if (!sym.AsPropertySymbol().getType().isPredefType(PredefinedType.PT_OBJECT))
			{
				return false;
			}
			object[] customAttributes2 = sym.AsPropertySymbol().AssociatedPropertyInfo.GetCustomAttributes(typeof(DynamicAttribute), inherit: false);
			if (customAttributes2.Length == 1)
			{
				dynamicAttribute = customAttributes2[0] as DynamicAttribute;
			}
		}
		if (dynamicAttribute == null)
		{
			return false;
		}
		if (dynamicAttribute.TransformFlags.Count != 0)
		{
			if (dynamicAttribute.TransformFlags.Count == 1)
			{
				return dynamicAttribute.TransformFlags[0];
			}
			return false;
		}
		return true;
	}

	private bool LookupInClass(AggregateType typeStart, ref AggregateType ptypeEnd)
	{
		AggregateType aggregateType = ptypeEnd;
		AggregateType aggregateType2 = typeStart;
		while (aggregateType2 != aggregateType && aggregateType2 != null)
		{
			bool pfHideByName = false;
			SearchSingleType(aggregateType2, out pfHideByName);
			m_flags &= (MemLookFlags)3221225471u;
			if ((bool)m_swtFirst && !m_fMulti)
			{
				return false;
			}
			if (pfHideByName)
			{
				ptypeEnd = null;
				return true;
			}
			if ((m_flags & MemLookFlags.Ctor) != 0)
			{
				return false;
			}
			aggregateType2 = aggregateType2.GetBaseClass();
		}
		return true;
	}

	private bool LookupInInterfaces(AggregateType typeStart, TypeArray types)
	{
		if (typeStart != null)
		{
			typeStart.fAllHidden = false;
			typeStart.fDiffHidden = m_swtFirst != null;
		}
		for (int i = 0; i < types.size; i++)
		{
			AggregateType aggregateType = types.Item(i).AsAggregateType();
			aggregateType.fAllHidden = false;
			aggregateType.fDiffHidden = m_swtFirst;
		}
		bool flag = false;
		AggregateType aggregateType2 = typeStart;
		int num = 0;
		if (aggregateType2 == null)
		{
			aggregateType2 = types.Item(num++).AsAggregateType();
		}
		while (true)
		{
			bool pfHideByName = false;
			if (!aggregateType2.fAllHidden && SearchSingleType(aggregateType2, out pfHideByName))
			{
				pfHideByName |= !m_fMulti;
				TypeArray ifacesAll = aggregateType2.GetIfacesAll();
				for (int j = 0; j < ifacesAll.size; j++)
				{
					AggregateType aggregateType3 = ifacesAll.Item(j).AsAggregateType();
					if (pfHideByName)
					{
						aggregateType3.fAllHidden = true;
					}
					aggregateType3.fDiffHidden = true;
				}
				if (pfHideByName)
				{
					flag = true;
				}
			}
			m_flags &= (MemLookFlags)3221225471u;
			if (num >= types.size)
			{
				break;
			}
			aggregateType2 = types.Item(num++).AsAggregateType();
		}
		return !flag;
	}

	private SymbolLoader GetSymbolLoader()
	{
		return m_pSymbolLoader;
	}

	private CSemanticChecker GetSemanticChecker()
	{
		return m_pSemanticChecker;
	}

	private ErrorHandling GetErrorContext()
	{
		return GetSymbolLoader().GetErrorContext();
	}

	private void ReportBogus(SymWithType swt)
	{
		switch (swt.Sym.getKind())
		{
		case SYMKIND.SK_PropertySymbol:
			if (swt.Prop().useMethInstead)
			{
				MethodSymbol methGet = swt.Prop().methGet;
				MethodSymbol methSet = swt.Prop().methSet;
				ReportBogusForEventsAndProperties(swt, methGet, methSet);
				return;
			}
			break;
		case SYMKIND.SK_MethodSymbol:
			if (swt.Meth().name == GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_INVOKE) && swt.Meth().getClass().IsDelegate())
			{
				swt.Set(swt.Meth().getClass(), swt.GetType());
			}
			break;
		}
		GetErrorContext().ErrorRef(ErrorCode.ERR_BindToBogus, swt);
	}

	private void ReportBogusForEventsAndProperties(SymWithType swt, MethodSymbol meth1, MethodSymbol meth2)
	{
		if (meth1 != null && meth2 != null)
		{
			GetErrorContext().Error(ErrorCode.ERR_BindToBogusProp2, swt.Sym.name, new SymWithType(meth1, swt.GetType()), new SymWithType(meth2, swt.GetType()), new ErrArgRefOnly(swt.Sym));
			return;
		}
		if (meth1 != null || meth2 != null)
		{
			GetErrorContext().Error(ErrorCode.ERR_BindToBogusProp1, swt.Sym.name, new SymWithType((meth1 != null) ? meth1 : meth2, swt.GetType()), new ErrArgRefOnly(swt.Sym));
			return;
		}
		throw Error.InternalCompilerError();
	}

	private bool IsDelegateType(CType pSrcType, AggregateType pAggType)
	{
		CType cType = GetSymbolLoader().GetTypeManager().SubstType(pSrcType, pAggType, pAggType.GetTypeArgsAll());
		return cType.isDelegateType();
	}

	public MemberLookup()
	{
		m_methPropWithTypeList = new List<MethPropWithType>();
		m_rgtypeStart = new List<AggregateType>();
		m_swtFirst = new SymWithType();
		m_swtAmbig = new SymWithType();
		m_swtInaccess = new SymWithType();
		m_swtBad = new SymWithType();
		m_swtBogus = new SymWithType();
		m_swtBadArity = new SymWithType();
		m_swtAmbigWarn = new SymWithType();
		m_swtOverride = new SymWithType();
	}

	public bool Lookup(CSemanticChecker checker, CType typeSrc, EXPR obj, ParentSymbol symWhere, Name name, int arity, MemLookFlags flags)
	{
		m_prgtype = m_rgtypeStart;
		m_pSemanticChecker = checker;
		m_pSymbolLoader = checker.GetSymbolLoader();
		m_typeSrc = typeSrc;
		m_obj = ((obj != null && !obj.isCLASS()) ? obj : null);
		m_symWhere = symWhere;
		m_name = name;
		m_arity = arity;
		m_flags = flags;
		if ((m_flags & MemLookFlags.BaseCall) != 0)
		{
			m_typeQual = null;
		}
		else if ((m_flags & MemLookFlags.Ctor) != 0)
		{
			m_typeQual = m_typeSrc;
		}
		else if (obj != null)
		{
			m_typeQual = obj.type;
		}
		else
		{
			m_typeQual = null;
		}
		AggregateType aggregateType = null;
		AggregateType aggregateType2 = null;
		TypeArray typeArray = BSYMMGR.EmptyTypeArray();
		AggregateType ptypeEnd = null;
		if (typeSrc.IsTypeParameterType())
		{
			m_flags &= (MemLookFlags)3221225471u;
			typeArray = typeSrc.AsTypeParameterType().GetInterfaceBounds();
			aggregateType = typeSrc.AsTypeParameterType().GetEffectiveBaseClass();
			if (typeArray.size > 0 && aggregateType.isPredefType(PredefinedType.PT_OBJECT))
			{
				aggregateType = null;
			}
		}
		else if (!typeSrc.isInterfaceType())
		{
			aggregateType = typeSrc.AsAggregateType();
			if (aggregateType.IsWindowsRuntimeType())
			{
				typeArray = aggregateType.GetWinRTCollectionIfacesAll(GetSymbolLoader());
			}
		}
		else
		{
			aggregateType2 = typeSrc.AsAggregateType();
			typeArray = aggregateType2.GetIfacesAll();
		}
		if (aggregateType2 != null || typeArray.size > 0)
		{
			ptypeEnd = GetSymbolLoader().GetReqPredefType(PredefinedType.PT_OBJECT);
		}
		if ((aggregateType == null || LookupInClass(aggregateType, ref ptypeEnd)) && (aggregateType2 != null || typeArray.size > 0) && LookupInInterfaces(aggregateType2, typeArray) && ptypeEnd != null)
		{
			AggregateType ptypeEnd2 = null;
			LookupInClass(ptypeEnd, ref ptypeEnd2);
		}
		m_results = new CMemberLookupResults(GetAllTypes(), m_name);
		return !FError();
	}

	public CMemberLookupResults GetResults()
	{
		return m_results;
	}

	public bool FError()
	{
		if ((bool)m_swtFirst)
		{
			return m_swtAmbig;
		}
		return true;
	}

	public Symbol SymFirst()
	{
		return m_swtFirst.Sym;
	}

	public SymWithType SwtFirst()
	{
		return m_swtFirst;
	}

	public SymWithType SwtInaccessible()
	{
		return m_swtInaccess;
	}

	public EXPR GetObject()
	{
		return m_obj;
	}

	public CType GetSourceType()
	{
		return m_typeSrc;
	}

	public MemLookFlags GetFlags()
	{
		return m_flags;
	}

	public TypeArray GetAllTypes()
	{
		BSYMMGR bSymmgr = GetSymbolLoader().getBSymmgr();
		int count = m_prgtype.Count;
		CType[] prgtype = m_prgtype.ToArray();
		return bSymmgr.AllocParams(count, prgtype);
	}

	public void ReportErrors()
	{
		if ((bool)m_swtFirst)
		{
			GetErrorContext().ErrorRef(ErrorCode.ERR_AmbigMember, m_swtFirst, m_swtAmbig);
		}
		else if ((bool)m_swtInaccess)
		{
			if (!m_swtInaccess.Sym.isUserCallable() && (m_flags & MemLookFlags.UserCallable) != 0)
			{
				GetErrorContext().Error(ErrorCode.ERR_CantCallSpecialMethod, m_swtInaccess);
			}
			else
			{
				GetSemanticChecker().ReportAccessError(m_swtInaccess, m_symWhere, m_typeQual);
			}
		}
		else if ((m_flags & MemLookFlags.Ctor) != 0)
		{
			if (m_arity > 0)
			{
				GetErrorContext().Error(ErrorCode.ERR_BadCtorArgCount, m_typeSrc.getAggregate(), m_arity);
			}
			else
			{
				GetErrorContext().Error(ErrorCode.ERR_NoConstructors, m_typeSrc.getAggregate());
			}
		}
		else if ((m_flags & MemLookFlags.Operator) != 0)
		{
			GetErrorContext().Error(ErrorCode.ERR_NoSuchMember, m_typeSrc, m_name);
		}
		else if ((m_flags & MemLookFlags.Indexer) != 0)
		{
			GetErrorContext().Error(ErrorCode.ERR_BadIndexLHS, m_typeSrc);
		}
		else if ((bool)m_swtBad)
		{
			GetErrorContext().Error(((m_flags & MemLookFlags.MustBeInvocable) != 0) ? ErrorCode.ERR_NonInvocableMemberCalled : ErrorCode.ERR_CantCallSpecialMethod, m_swtBad);
		}
		else if ((bool)m_swtBogus)
		{
			ReportBogus(m_swtBogus);
		}
		else if ((bool)m_swtBadArity)
		{
			switch (m_swtBadArity.Sym.getKind())
			{
			case SYMKIND.SK_MethodSymbol:
			{
				int size = m_swtBadArity.Sym.AsMethodSymbol().typeVars.size;
				GetErrorContext().ErrorRef((size > 0) ? ErrorCode.ERR_BadArity : ErrorCode.ERR_HasNoTypeVars, m_swtBadArity, new ErrArgSymKind(m_swtBadArity.Sym), size);
				break;
			}
			case SYMKIND.SK_AggregateSymbol:
			{
				int size = m_swtBadArity.Sym.AsAggregateSymbol().GetTypeVars().size;
				GetErrorContext().ErrorRef((size > 0) ? ErrorCode.ERR_BadArity : ErrorCode.ERR_HasNoTypeVars, m_swtBadArity, new ErrArgSymKind(m_swtBadArity.Sym), size);
				break;
			}
			default:
				ExpressionBinder.ReportTypeArgsNotAllowedError(GetSymbolLoader(), m_arity, m_swtBadArity, new ErrArgSymKind(m_swtBadArity.Sym));
				break;
			}
		}
		else if ((m_flags & MemLookFlags.ExtensionCall) != 0)
		{
			GetErrorContext().Error(ErrorCode.ERR_NoSuchMemberOrExtension, m_typeSrc, m_name);
		}
		else
		{
			GetErrorContext().Error(ErrorCode.ERR_NoSuchMember, m_typeSrc, m_name);
		}
	}
}
