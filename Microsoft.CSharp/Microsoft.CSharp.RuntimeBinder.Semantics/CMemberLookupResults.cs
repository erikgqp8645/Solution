using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class CMemberLookupResults
{
	public class CMethodIterator
	{
		private SymbolLoader m_pSymbolLoader;

		private CSemanticChecker m_pSemanticChecker;

		private AggregateType m_pCurrentType;

		private MethodOrPropertySymbol m_pCurrentSym;

		private Declaration m_pContext;

		private TypeArray m_pContainingTypes;

		private CType m_pQualifyingType;

		private Name m_pName;

		private int m_nArity;

		private symbmask_t m_mask;

		private EXPRFLAG m_flags;

		private int m_nCurrentTypeCount;

		private bool m_bIsCheckingInstanceMethods;

		private bool m_bAtEnd;

		private bool m_bAllowBogusAndInaccessible;

		private bool m_bAllowExtensionMethods;

		private bool m_bCurrentSymIsBogus;

		private bool m_bCurrentSymIsInaccessible;

		private bool m_bcanIncludeExtensionsInResults;

		private bool m_bEndIterationAtCurrentExtensionList;

		public CMethodIterator(CSemanticChecker checker, SymbolLoader symLoader, Name name, TypeArray containingTypes, CType @object, CType qualifyingType, Declaration context, bool allowBogusAndInaccessible, bool allowExtensionMethods, int arity, EXPRFLAG flags, symbmask_t mask)
		{
			m_pSemanticChecker = checker;
			m_pSymbolLoader = symLoader;
			m_pCurrentType = null;
			m_pCurrentSym = null;
			m_pName = name;
			m_pContainingTypes = containingTypes;
			m_pQualifyingType = qualifyingType;
			m_pContext = context;
			m_bAllowBogusAndInaccessible = allowBogusAndInaccessible;
			m_bAllowExtensionMethods = allowExtensionMethods;
			m_nArity = arity;
			m_flags = flags;
			m_mask = mask;
			m_nCurrentTypeCount = 0;
			m_bIsCheckingInstanceMethods = true;
			m_bAtEnd = false;
			m_bCurrentSymIsBogus = false;
			m_bCurrentSymIsInaccessible = false;
			m_bcanIncludeExtensionsInResults = m_bAllowExtensionMethods;
			m_bEndIterationAtCurrentExtensionList = false;
		}

		public MethodOrPropertySymbol GetCurrentSymbol()
		{
			return m_pCurrentSym;
		}

		public AggregateType GetCurrentType()
		{
			return m_pCurrentType;
		}

		public bool IsCurrentSymbolInaccessible()
		{
			return m_bCurrentSymIsInaccessible;
		}

		public bool IsCurrentSymbolBogus()
		{
			return m_bCurrentSymIsBogus;
		}

		public bool MoveNext(bool canIncludeExtensionsInResults, bool endatCurrentExtensionList)
		{
			if (m_bcanIncludeExtensionsInResults)
			{
				m_bcanIncludeExtensionsInResults = canIncludeExtensionsInResults;
			}
			if (!m_bEndIterationAtCurrentExtensionList)
			{
				m_bEndIterationAtCurrentExtensionList = endatCurrentExtensionList;
			}
			if (m_bAtEnd)
			{
				return false;
			}
			if (m_pCurrentType == null)
			{
				if (m_pContainingTypes.size == 0)
				{
					m_bIsCheckingInstanceMethods = false;
					m_bAtEnd = true;
					return false;
				}
				if (!FindNextTypeForInstanceMethods())
				{
					m_bAtEnd = true;
					return false;
				}
			}
			if (!FindNextMethod())
			{
				m_bAtEnd = true;
				return false;
			}
			return true;
		}

		public bool AtEnd()
		{
			return m_pCurrentSym == null;
		}

		private CSemanticChecker GetSemanticChecker()
		{
			return m_pSemanticChecker;
		}

		private SymbolLoader GetSymbolLoader()
		{
			return m_pSymbolLoader;
		}

		public bool CanUseCurrentSymbol()
		{
			m_bCurrentSymIsInaccessible = false;
			m_bCurrentSymIsBogus = false;
			if ((m_mask == symbmask_t.MASK_MethodSymbol && ((m_flags & EXPRFLAG.EXF_CTOR) == 0 != !m_pCurrentSym.AsMethodSymbol().IsConstructor() || (m_flags & EXPRFLAG.EXF_OPERATOR) == 0 != !m_pCurrentSym.AsMethodSymbol().isOperator)) || (m_mask == symbmask_t.MASK_PropertySymbol && !m_pCurrentSym.AsPropertySymbol().isIndexer()))
			{
				return false;
			}
			if (m_nArity > 0 && m_mask == symbmask_t.MASK_MethodSymbol && m_pCurrentSym.AsMethodSymbol().typeVars.size != m_nArity)
			{
				return false;
			}
			if (!ExpressionBinder.IsMethPropCallable(m_pCurrentSym, (m_flags & EXPRFLAG.EXF_USERCALLABLE) != 0))
			{
				return false;
			}
			if (!GetSemanticChecker().CheckAccess(m_pCurrentSym, m_pCurrentType, m_pContext, m_pQualifyingType))
			{
				if (!m_bAllowBogusAndInaccessible)
				{
					return false;
				}
				m_bCurrentSymIsInaccessible = true;
			}
			if (GetSemanticChecker().CheckBogus(m_pCurrentSym))
			{
				if (!m_bAllowBogusAndInaccessible)
				{
					return false;
				}
				m_bCurrentSymIsBogus = true;
			}
			if (!m_bIsCheckingInstanceMethods && !m_pCurrentSym.AsMethodSymbol().IsExtension())
			{
				return false;
			}
			return true;
		}

		private bool FindNextMethod()
		{
			while (true)
			{
				if (m_pCurrentSym == null)
				{
					m_pCurrentSym = GetSymbolLoader().LookupAggMember(m_pName, m_pCurrentType.getAggregate(), m_mask).AsMethodOrPropertySymbol();
				}
				else
				{
					m_pCurrentSym = GetSymbolLoader().LookupNextSym(m_pCurrentSym, m_pCurrentType.getAggregate(), m_mask).AsMethodOrPropertySymbol();
				}
				if (m_pCurrentSym != null)
				{
					break;
				}
				if (m_bIsCheckingInstanceMethods)
				{
					if (!FindNextTypeForInstanceMethods() && m_bcanIncludeExtensionsInResults)
					{
						m_bIsCheckingInstanceMethods = false;
					}
					else if (m_pCurrentType == null && !m_bcanIncludeExtensionsInResults)
					{
						return false;
					}
				}
			}
			return true;
		}

		private bool FindNextTypeForInstanceMethods()
		{
			if (m_pContainingTypes.size > 0)
			{
				if (m_nCurrentTypeCount >= m_pContainingTypes.size)
				{
					m_pCurrentType = null;
				}
				else
				{
					m_pCurrentType = m_pContainingTypes.Item(m_nCurrentTypeCount++).AsAggregateType();
				}
			}
			else
			{
				m_pCurrentType = m_pCurrentType.GetBaseClass();
			}
			return m_pCurrentType != null;
		}
	}

	private Name m_pName;

	public TypeArray ContainingTypes { get; private set; }

	public CMemberLookupResults()
	{
		m_pName = null;
		ContainingTypes = null;
	}

	public CMemberLookupResults(TypeArray containingTypes, Name name)
	{
		m_pName = name;
		ContainingTypes = containingTypes;
		if (ContainingTypes == null)
		{
			ContainingTypes = BSYMMGR.EmptyTypeArray();
		}
	}

	public CMethodIterator GetMethodIterator(CSemanticChecker pChecker, SymbolLoader pSymLoader, CType pObject, CType pQualifyingType, Declaration pContext, bool allowBogusAndInaccessible, bool allowExtensionMethods, int arity, EXPRFLAG flags, symbmask_t mask)
	{
		return new CMethodIterator(pChecker, pSymLoader, m_pName, ContainingTypes, pObject, pQualifyingType, pContext, allowBogusAndInaccessible, allowExtensionMethods, arity, flags, mask);
	}
}
