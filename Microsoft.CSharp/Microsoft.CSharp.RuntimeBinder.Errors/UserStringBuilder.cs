using System.Globalization;
using System.Text;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class UserStringBuilder
{
	protected bool fHadUndisplayableStringInError;

	protected bool m_buildingInProgress;

	protected GlobalSymbolContext m_globalSymbols;

	protected StringBuilder m_strBuilder;

	public UserStringBuilder(GlobalSymbolContext globalSymbols)
	{
		fHadUndisplayableStringInError = false;
		m_buildingInProgress = false;
		m_globalSymbols = globalSymbols;
	}

	protected void BeginString()
	{
		m_buildingInProgress = true;
		m_strBuilder = new StringBuilder();
	}

	protected void EndString(out string s)
	{
		m_buildingInProgress = false;
		s = m_strBuilder.ToString();
		m_strBuilder = null;
	}

	public bool HadUndisplayableString()
	{
		return fHadUndisplayableStringInError;
	}

	public void ResetUndisplayableStringFlag()
	{
		fHadUndisplayableStringInError = false;
	}

	protected void ErrSK(out string psz, SYMKIND sk)
	{
		ErrId(out psz, sk switch
		{
			SYMKIND.SK_MethodSymbol => MessageID.SK_METHOD, 
			SYMKIND.SK_AggregateSymbol => MessageID.SK_CLASS, 
			SYMKIND.SK_NamespaceSymbol => MessageID.SK_NAMESPACE, 
			SYMKIND.SK_FieldSymbol => MessageID.SK_FIELD, 
			SYMKIND.SK_LocalVariableSymbol => MessageID.SK_VARIABLE, 
			SYMKIND.SK_PropertySymbol => MessageID.SK_PROPERTY, 
			SYMKIND.SK_EventSymbol => MessageID.SK_EVENT, 
			SYMKIND.SK_TypeParameterSymbol => MessageID.SK_TYVAR, 
			SYMKIND.SK_AssemblyQualifiedNamespaceSymbol => MessageID.SK_ALIAS, 
			_ => MessageID.SK_UNKNOWN, 
		});
	}

	protected void ErrAppendParamList(TypeArray @params, bool isVarargs, bool isParamArray)
	{
		if (@params == null)
		{
			return;
		}
		for (int i = 0; i < @params.size; i++)
		{
			if (i > 0)
			{
				ErrAppendString(", ");
			}
			if (isParamArray && i == @params.size - 1)
			{
				ErrAppendString("params ");
			}
			ErrAppendType(@params.Item(i), null);
		}
		if (isVarargs)
		{
			if (@params.size != 0)
			{
				ErrAppendString(", ");
			}
			ErrAppendString("...");
		}
	}

	public void ErrAppendString(string str)
	{
		m_strBuilder.Append(str);
	}

	public void ErrAppendChar(char ch)
	{
		m_strBuilder.Append(ch);
	}

	public void ErrAppendPrintf(string format, params object[] args)
	{
		ErrAppendString(string.Format(CultureInfo.InvariantCulture, format, args));
	}

	public void ErrAppendName(Name name)
	{
		CheckDisplayableName(name);
		if (name == GetNameManager().GetPredefName(PredefinedName.PN_INDEXERINTERNAL))
		{
			ErrAppendString("this");
		}
		else
		{
			ErrAppendString(name.Text);
		}
	}

	protected void ErrAppendMethodParentSym(MethodSymbol sym, SubstContext pcxt, out TypeArray substMethTyParams)
	{
		substMethTyParams = null;
		ErrAppendParentSym(sym, pcxt);
	}

	protected void ErrAppendParentSym(Symbol sym, SubstContext pctx)
	{
		ErrAppendParentCore(sym.parent, pctx);
	}

	protected void ErrAppendParentType(CType pType, SubstContext pctx)
	{
		if (pType.IsErrorType())
		{
			if (pType.AsErrorType().HasTypeParent())
			{
				ErrAppendType(pType.AsErrorType().GetTypeParent(), null);
				ErrAppendChar('.');
			}
			else
			{
				ErrAppendParentCore(pType.AsErrorType().GetNSParent(), pctx);
			}
		}
		else if (pType.IsAggregateType())
		{
			ErrAppendParentCore(pType.AsAggregateType().GetOwningAggregate(), pctx);
		}
		else if (pType.GetBaseOrParameterOrElementType() != null)
		{
			ErrAppendType(pType.GetBaseOrParameterOrElementType(), null);
			ErrAppendChar('.');
		}
	}

	protected void ErrAppendParentCore(Symbol parent, SubstContext pctx)
	{
		if (parent != null && parent != getBSymmgr().GetRootNS())
		{
			if (pctx != null && !pctx.FNop() && parent.IsAggregateSymbol() && parent.AsAggregateSymbol().GetTypeVarsAll().size != 0)
			{
				CType pType = GetTypeManager().SubstType(parent.AsAggregateSymbol().getThisType(), pctx);
				ErrAppendType(pType, null);
			}
			else
			{
				ErrAppendSym(parent, null);
			}
			ErrAppendChar('.');
		}
	}

	protected void ErrAppendTypeParameters(TypeArray @params, SubstContext pctx, bool forClass)
	{
		if (@params != null && @params.size != 0)
		{
			ErrAppendChar('<');
			ErrAppendType(@params.Item(0), pctx);
			for (int i = 1; i < @params.size; i++)
			{
				ErrAppendString(",");
				ErrAppendType(@params.Item(i), pctx);
			}
			ErrAppendChar('>');
		}
	}

	protected void ErrAppendMethod(MethodSymbol meth, SubstContext pctx, bool fArgs)
	{
		if (meth.IsExpImpl() && (bool)meth.swtSlot)
		{
			ErrAppendParentSym(meth, pctx);
			SubstContext pctx2 = new SubstContext(GetTypeManager().SubstType(meth.swtSlot.GetType(), pctx).AsAggregateType());
			ErrAppendSym(meth.swtSlot.Sym, pctx2, fArgs);
			return;
		}
		if (meth.isPropertyAccessor())
		{
			PropertySymbol property = meth.getProperty();
			ErrAppendSym(property, pctx);
			if (property.methGet == meth)
			{
				ErrAppendString(".get");
			}
			else
			{
				ErrAppendString(".set");
			}
			return;
		}
		if (meth.isEventAccessor())
		{
			EventSymbol @event = meth.getEvent();
			ErrAppendSym(@event, pctx);
			if (@event.methAdd == meth)
			{
				ErrAppendString(".add");
			}
			else
			{
				ErrAppendString(".remove");
			}
			return;
		}
		TypeArray substMethTyParams = null;
		ErrAppendMethodParentSym(meth, pctx, out substMethTyParams);
		if (meth.IsConstructor())
		{
			ErrAppendName(meth.getClass().name);
		}
		else if (meth.IsDestructor())
		{
			ErrAppendChar('~');
			ErrAppendName(meth.getClass().name);
		}
		else if (meth.isConversionOperator())
		{
			ErrAppendString(meth.isImplicit() ? "implicit" : "explicit");
			ErrAppendString(" operator ");
			ErrAppendType(meth.RetType, pctx);
		}
		else if (meth.isOperator)
		{
			ErrAppendString("operator ");
			OperatorKind op = Operators.OperatorOfMethodName(GetNameManager(), meth.name);
			string str = (Operators.HasDisplayName(op) ? Operators.GetDisplayName(op) : ((meth.name != GetNameManager().GetPredefName(PredefinedName.PN_OPEQUALS)) ? "compare" : "equals"));
			ErrAppendString(str);
		}
		else if (meth.IsExpImpl())
		{
			if (meth.errExpImpl != null)
			{
				ErrAppendType(meth.errExpImpl, pctx, fArgs);
			}
		}
		else
		{
			ErrAppendName(meth.name);
		}
		if (substMethTyParams == null)
		{
			ErrAppendTypeParameters(meth.typeVars, pctx, forClass: false);
		}
		if (fArgs)
		{
			ErrAppendChar('(');
			if (!meth.computeCurrentBogusState())
			{
				ErrAppendParamList(GetTypeManager().SubstTypeArray(meth.Params, pctx), meth.isVarargs, meth.isParamArray);
			}
			ErrAppendChar(')');
		}
	}

	protected void ErrAppendIndexer(IndexerSymbol indexer, SubstContext pctx)
	{
		ErrAppendString("this[");
		ErrAppendParamList(GetTypeManager().SubstTypeArray(indexer.Params, pctx), isVarargs: false, indexer.isParamArray);
		ErrAppendChar(']');
	}

	protected void ErrAppendProperty(PropertySymbol prop, SubstContext pctx)
	{
		ErrAppendParentSym(prop, pctx);
		if (prop.IsExpImpl() && prop.swtSlot.Sym != null)
		{
			SubstContext pctx2 = new SubstContext(GetTypeManager().SubstType(prop.swtSlot.GetType(), pctx).AsAggregateType());
			ErrAppendSym(prop.swtSlot.Sym, pctx2);
		}
		else if (prop.IsExpImpl())
		{
			if (prop.errExpImpl != null)
			{
				ErrAppendType(prop.errExpImpl, pctx, fArgs: false);
			}
			if (prop.isIndexer())
			{
				ErrAppendChar('.');
				ErrAppendIndexer(prop.AsIndexerSymbol(), pctx);
			}
		}
		else if (prop.isIndexer())
		{
			ErrAppendIndexer(prop.AsIndexerSymbol(), pctx);
		}
		else
		{
			ErrAppendName(prop.name);
		}
	}

	protected void ErrAppendEvent(EventSymbol @event, SubstContext pctx)
	{
	}

	public void ErrAppendId(MessageID id)
	{
		ErrId(out var s, id);
		ErrAppendString(s);
	}

	public void ErrAppendSym(Symbol sym, SubstContext pctx)
	{
		ErrAppendSym(sym, pctx, fArgs: true);
	}

	public void ErrAppendSym(Symbol sym, SubstContext pctx, bool fArgs)
	{
		switch (sym.getKind())
		{
		case SYMKIND.SK_NamespaceDeclaration:
			ErrAppendSym(sym.AsNamespaceDeclaration().NameSpace(), null);
			break;
		case SYMKIND.SK_GlobalAttributeDeclaration:
			ErrAppendName(sym.name);
			break;
		case SYMKIND.SK_AggregateDeclaration:
			ErrAppendSym(sym.AsAggregateDeclaration().Agg(), pctx);
			break;
		case SYMKIND.SK_AggregateSymbol:
		{
			string niceName = PredefinedTypes.GetNiceName(sym.AsAggregateSymbol());
			if (niceName != null)
			{
				ErrAppendString(niceName);
				break;
			}
			if (sym.AsAggregateSymbol().IsAnonymousType())
			{
				ErrAppendId(MessageID.AnonymousType);
				break;
			}
			ErrAppendParentSym(sym, pctx);
			ErrAppendName(sym.name);
			ErrAppendTypeParameters(sym.AsAggregateSymbol().GetTypeVars(), pctx, forClass: true);
			break;
		}
		case SYMKIND.SK_MethodSymbol:
			ErrAppendMethod(sym.AsMethodSymbol(), pctx, fArgs);
			break;
		case SYMKIND.SK_PropertySymbol:
			ErrAppendProperty(sym.AsPropertySymbol(), pctx);
			break;
		case SYMKIND.SK_EventSymbol:
			ErrAppendEvent(sym.AsEventSymbol(), pctx);
			break;
		case SYMKIND.SK_NamespaceSymbol:
		case SYMKIND.SK_AssemblyQualifiedNamespaceSymbol:
			if (sym == getBSymmgr().GetRootNS())
			{
				ErrAppendId(MessageID.GlobalNamespace);
				break;
			}
			ErrAppendParentSym(sym, null);
			ErrAppendName(sym.name);
			break;
		case SYMKIND.SK_FieldSymbol:
			ErrAppendParentSym(sym, pctx);
			ErrAppendName(sym.name);
			break;
		case SYMKIND.SK_TypeParameterSymbol:
			if (sym.name == null)
			{
				if (sym.AsTypeParameterSymbol().IsMethodTypeParameter())
				{
					ErrAppendChar('!');
				}
				ErrAppendChar('!');
				ErrAppendPrintf("{0}", sym.AsTypeParameterSymbol().GetIndexInTotalParameters());
			}
			else
			{
				ErrAppendName(sym.name);
			}
			break;
		case SYMKIND.SK_LocalVariableSymbol:
		case SYMKIND.SK_TransparentIdentifierMemberSymbol:
		case SYMKIND.SK_LabelSymbol:
			ErrAppendName(sym.name);
			break;
		case SYMKIND.SK_AliasSymbol:
		case SYMKIND.SK_ExternalAliasDefinitionSymbol:
		case SYMKIND.SK_Scope:
		case SYMKIND.SK_CachedNameSymbol:
		case SYMKIND.SK_LambdaScope:
			break;
		}
	}

	public void ErrAppendType(CType pType, SubstContext pCtx)
	{
		ErrAppendType(pType, pCtx, fArgs: true);
	}

	public void ErrAppendType(CType pType, SubstContext pctx, bool fArgs)
	{
		if (pctx != null)
		{
			if (!pctx.FNop())
			{
				pType = GetTypeManager().SubstType(pType, pctx);
			}
			pctx = null;
		}
		switch (pType.GetTypeKind())
		{
		case TypeKind.TK_AggregateType:
		{
			AggregateType aggregateType = pType.AsAggregateType();
			string niceName = PredefinedTypes.GetNiceName(aggregateType.getAggregate());
			if (niceName != null)
			{
				ErrAppendString(niceName);
			}
			else
			{
				if (aggregateType.getAggregate().IsAnonymousType())
				{
					ErrAppendPrintf("AnonymousType#{0}", GetTypeID(aggregateType));
					break;
				}
				if (aggregateType.outerType != null)
				{
					ErrAppendType(aggregateType.outerType, pctx);
					ErrAppendChar('.');
				}
				else
				{
					ErrAppendParentSym(aggregateType.getAggregate(), pctx);
				}
				ErrAppendName(aggregateType.getAggregate().name);
			}
			ErrAppendTypeParameters(aggregateType.GetTypeArgsThis(), pctx, forClass: true);
			break;
		}
		case TypeKind.TK_TypeParameterType:
			if (pType.GetName() == null)
			{
				if (pType.AsTypeParameterType().IsMethodTypeParameter())
				{
					ErrAppendChar('!');
				}
				ErrAppendChar('!');
				ErrAppendPrintf("{0}", pType.AsTypeParameterType().GetIndexInTotalParameters());
			}
			else
			{
				ErrAppendName(pType.GetName());
			}
			break;
		case TypeKind.TK_ErrorType:
			if (pType.AsErrorType().HasParent())
			{
				ErrAppendParentType(pType, pctx);
				ErrAppendName(pType.AsErrorType().nameText);
				ErrAppendTypeParameters(pType.AsErrorType().typeArgs, pctx, forClass: true);
			}
			else
			{
				ErrAppendId(MessageID.ERRORSYM);
			}
			break;
		case TypeKind.TK_NullType:
			ErrAppendId(MessageID.NULL);
			break;
		case TypeKind.TK_BoundLambdaType:
			ErrAppendId(MessageID.AnonMethod);
			break;
		case TypeKind.TK_UnboundLambdaType:
			ErrAppendId(MessageID.Lambda);
			break;
		case TypeKind.TK_MethodGroupType:
			ErrAppendId(MessageID.MethodGroup);
			break;
		case TypeKind.TK_ArgumentListType:
			ErrAppendString(TokenFacts.GetText(TokenKind.ArgList));
			break;
		case TypeKind.TK_ArrayType:
		{
			CType baseElementType = pType.AsArrayType().GetBaseElementType();
			if (baseElementType == null)
			{
				break;
			}
			ErrAppendType(baseElementType, pctx);
			baseElementType = pType;
			while (baseElementType != null && baseElementType.IsArrayType())
			{
				int rank = baseElementType.AsArrayType().rank;
				ErrAppendChar('[');
				if (rank > 1)
				{
					ErrAppendChar('*');
				}
				for (int num = rank; num > 1; num--)
				{
					ErrAppendChar(',');
					ErrAppendChar('*');
				}
				ErrAppendChar(']');
				baseElementType = baseElementType.AsArrayType().GetElementType();
			}
			break;
		}
		case TypeKind.TK_VoidType:
			ErrAppendName(GetNameManager().Lookup(TokenFacts.GetText(TokenKind.Void)));
			break;
		case TypeKind.TK_ParameterModifierType:
			ErrAppendString(pType.AsParameterModifierType().isOut ? "out " : "ref ");
			ErrAppendType(pType.AsParameterModifierType().GetParameterType(), pctx);
			break;
		case TypeKind.TK_PointerType:
			ErrAppendType(pType.AsPointerType().GetReferentType(), pctx);
			ErrAppendChar('*');
			break;
		case TypeKind.TK_NullableType:
			ErrAppendType(pType.AsNullableType().GetUnderlyingType(), pctx);
			ErrAppendChar('?');
			break;
		case TypeKind.TK_OpenTypePlaceholderType:
		case TypeKind.TK_NaturalIntegerType:
			break;
		}
	}

	public bool ErrArgToString(out string psz, ErrArg parg, out bool fUserStrings)
	{
		fUserStrings = false;
		psz = null;
		bool result = true;
		switch (parg.eak)
		{
		case ErrArgKind.Ids:
			ErrId(out psz, parg.ids);
			break;
		case ErrArgKind.SymKind:
			ErrSK(out psz, parg.sk);
			break;
		case ErrArgKind.Type:
			BeginString();
			ErrAppendType(parg.pType, null);
			EndString(out psz);
			fUserStrings = true;
			break;
		case ErrArgKind.Sym:
			BeginString();
			ErrAppendSym(parg.sym, null);
			EndString(out psz);
			fUserStrings = true;
			break;
		case ErrArgKind.Name:
			if (parg.name == GetNameManager().GetPredefinedName(PredefinedName.PN_INDEXERINTERNAL))
			{
				psz = "this";
			}
			else
			{
				psz = parg.name.Text;
			}
			break;
		case ErrArgKind.Str:
			psz = parg.psz;
			break;
		case ErrArgKind.PredefName:
			BeginString();
			ErrAppendName(GetNameManager().GetPredefName(parg.pdn));
			EndString(out psz);
			break;
		case ErrArgKind.SymWithType:
		{
			SubstContext pctx2 = new SubstContext(parg.swtMemo.ats, null);
			BeginString();
			ErrAppendSym(parg.swtMemo.sym, pctx2, fArgs: true);
			EndString(out psz);
			fUserStrings = true;
			break;
		}
		case ErrArgKind.MethWithInst:
		{
			SubstContext pctx = new SubstContext(parg.mpwiMemo.ats, parg.mpwiMemo.typeArgs);
			BeginString();
			ErrAppendSym(parg.mpwiMemo.sym, pctx, fArgs: true);
			EndString(out psz);
			fUserStrings = true;
			break;
		}
		default:
			result = false;
			break;
		}
		return result;
	}

	protected bool IsDisplayableName(Name name)
	{
		return name != GetNameManager().GetPredefName(PredefinedName.PN_MISSING);
	}

	protected void CheckDisplayableName(Name name)
	{
		if (!IsDisplayableName(name))
		{
			fHadUndisplayableStringInError = true;
		}
	}

	protected NameManager GetNameManager()
	{
		return m_globalSymbols.GetNameManager();
	}

	protected TypeManager GetTypeManager()
	{
		return m_globalSymbols.GetTypes();
	}

	protected BSYMMGR getBSymmgr()
	{
		return m_globalSymbols.GetGlobalSymbols();
	}

	protected int GetTypeID(CType type)
	{
		return 0;
	}

	public void ErrId(out string s, MessageID id)
	{
		s = ErrorFacts.GetMessage(id);
	}
}
