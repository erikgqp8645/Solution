using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class CType : ITypeOrNamespace
{
	private TypeKind m_typeKind;

	private Name m_pName;

	private bool fHasErrors;

	private bool fUnres;

	private bool isBogus;

	private bool checkedBogus;

	private Type _associatedSystemType;

	public bool IsGenericParameter => IsTypeParameterType();

	public Type AssociatedSystemType
	{
		get
		{
			if (_associatedSystemType == null)
			{
				_associatedSystemType = CalculateAssociatedSystemType(this);
			}
			return _associatedSystemType;
		}
	}

	public AggregateType AsAggregateType()
	{
		return this as AggregateType;
	}

	public ErrorType AsErrorType()
	{
		return this as ErrorType;
	}

	public ArrayType AsArrayType()
	{
		return this as ArrayType;
	}

	public PointerType AsPointerType()
	{
		return this as PointerType;
	}

	public ParameterModifierType AsParameterModifierType()
	{
		return this as ParameterModifierType;
	}

	public NullableType AsNullableType()
	{
		return this as NullableType;
	}

	public TypeParameterType AsTypeParameterType()
	{
		return this as TypeParameterType;
	}

	public bool IsAggregateType()
	{
		return this is AggregateType;
	}

	public bool IsVoidType()
	{
		return this is VoidType;
	}

	public bool IsNullType()
	{
		return this is NullType;
	}

	public bool IsOpenTypePlaceholderType()
	{
		return this is OpenTypePlaceholderType;
	}

	public bool IsBoundLambdaType()
	{
		return this is BoundLambdaType;
	}

	public bool IsMethodGroupType()
	{
		return this is MethodGroupType;
	}

	public bool IsErrorType()
	{
		return this is ErrorType;
	}

	public bool IsArrayType()
	{
		return this is ArrayType;
	}

	public bool IsPointerType()
	{
		return this is PointerType;
	}

	public bool IsParameterModifierType()
	{
		return this is ParameterModifierType;
	}

	public bool IsNullableType()
	{
		return this is NullableType;
	}

	public bool IsTypeParameterType()
	{
		return this is TypeParameterType;
	}

	public bool IsWindowsRuntimeType()
	{
		return AssociatedSystemType.Attributes.HasFlag(TypeAttributes.WindowsRuntime);
	}

	public bool IsCollectionType()
	{
		if ((AssociatedSystemType.IsGenericType && (AssociatedSystemType.GetGenericTypeDefinition() == typeof(IList<>) || AssociatedSystemType.GetGenericTypeDefinition() == typeof(ICollection<>) || AssociatedSystemType.GetGenericTypeDefinition() == typeof(IEnumerable<>) || AssociatedSystemType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) || AssociatedSystemType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>) || AssociatedSystemType.GetGenericTypeDefinition() == typeof(IDictionary<, >) || AssociatedSystemType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<, >))) || AssociatedSystemType == typeof(IList) || AssociatedSystemType == typeof(ICollection) || AssociatedSystemType == typeof(IEnumerable) || AssociatedSystemType == typeof(INotifyCollectionChanged) || AssociatedSystemType == typeof(INotifyPropertyChanged))
		{
			return true;
		}
		return false;
	}

	private static Type CalculateAssociatedSystemType(CType src)
	{
		Type result = null;
		switch (src.GetTypeKind())
		{
		case TypeKind.TK_ArrayType:
		{
			ArrayType arrayType = src.AsArrayType();
			Type associatedSystemType2 = arrayType.GetElementType().AssociatedSystemType;
			result = ((arrayType.rank != 1) ? associatedSystemType2.MakeArrayType(arrayType.rank) : associatedSystemType2.MakeArrayType());
			break;
		}
		case TypeKind.TK_NullableType:
		{
			NullableType nullableType = src.AsNullableType();
			Type associatedSystemType = nullableType.GetUnderlyingType().AssociatedSystemType;
			result = typeof(Nullable<>).MakeGenericType(associatedSystemType);
			break;
		}
		case TypeKind.TK_PointerType:
		{
			PointerType pointerType = src.AsPointerType();
			Type associatedSystemType4 = pointerType.GetReferentType().AssociatedSystemType;
			result = associatedSystemType4.MakePointerType();
			break;
		}
		case TypeKind.TK_ParameterModifierType:
		{
			ParameterModifierType parameterModifierType = src.AsParameterModifierType();
			Type associatedSystemType3 = parameterModifierType.GetParameterType().AssociatedSystemType;
			result = associatedSystemType3.MakeByRefType();
			break;
		}
		case TypeKind.TK_AggregateType:
			result = CalculateAssociatedSystemTypeForAggregate(src.AsAggregateType());
			break;
		case TypeKind.TK_TypeParameterType:
		{
			TypeParameterType typeParameterType = src.AsTypeParameterType();
			Type type = null;
			if (typeParameterType.IsMethodTypeParameter())
			{
				MethodInfo methodInfo = typeParameterType.GetOwningSymbol().AsMethodSymbol().AssociatedMemberInfo as MethodInfo;
				result = methodInfo.GetGenericArguments()[typeParameterType.GetIndexInOwnParameters()];
			}
			else
			{
				type = typeParameterType.GetOwningSymbol().AsAggregateSymbol().AssociatedSystemType;
				result = type.GetGenericArguments()[typeParameterType.GetIndexInOwnParameters()];
			}
			break;
		}
		}
		return result;
	}

	private static Type CalculateAssociatedSystemTypeForAggregate(AggregateType aggtype)
	{
		AggregateSymbol owningAggregate = aggtype.GetOwningAggregate();
		TypeArray typeArgsAll = aggtype.GetTypeArgsAll();
		List<Type> list = new List<Type>();
		for (int i = 0; i < typeArgsAll.size; i++)
		{
			if (typeArgsAll.Item(i).IsTypeParameterType() && typeArgsAll.Item(i).AsTypeParameterType().GetTypeParameterSymbol()
				.name == null)
			{
				return null;
			}
			list.Add(typeArgsAll.Item(i).AssociatedSystemType);
		}
		Type[] typeArguments = list.ToArray();
		Type associatedSystemType = owningAggregate.AssociatedSystemType;
		if (associatedSystemType.IsGenericType)
		{
			try
			{
				return associatedSystemType.MakeGenericType(typeArguments);
			}
			catch (ArgumentException)
			{
				return associatedSystemType;
			}
		}
		return associatedSystemType;
	}

	public bool IsType()
	{
		return true;
	}

	public bool IsNamespace()
	{
		return false;
	}

	public AssemblyQualifiedNamespaceSymbol AsNamespace()
	{
		throw Error.InternalCompilerError();
	}

	public CType AsType()
	{
		return this;
	}

	public TypeKind GetTypeKind()
	{
		return m_typeKind;
	}

	public void SetTypeKind(TypeKind kind)
	{
		m_typeKind = kind;
	}

	public Name GetName()
	{
		return m_pName;
	}

	public void SetName(Name pName)
	{
		m_pName = pName;
	}

	public bool checkBogus()
	{
		return isBogus;
	}

	public bool getBogus()
	{
		return isBogus;
	}

	public bool hasBogus()
	{
		return checkedBogus;
	}

	public void setBogus(bool isBogus)
	{
		this.isBogus = isBogus;
		checkedBogus = true;
	}

	public bool computeCurrentBogusState()
	{
		if (hasBogus())
		{
			return checkBogus();
		}
		bool flag = false;
		switch (GetTypeKind())
		{
		case TypeKind.TK_ArrayType:
		case TypeKind.TK_PointerType:
		case TypeKind.TK_ParameterModifierType:
		case TypeKind.TK_NullableType:
			if (GetBaseOrParameterOrElementType() != null)
			{
				flag = GetBaseOrParameterOrElementType().computeCurrentBogusState();
			}
			break;
		case TypeKind.TK_ErrorType:
			setBogus(isBogus: false);
			break;
		case TypeKind.TK_AggregateType:
		{
			flag = AsAggregateType().getAggregate().computeCurrentBogusState();
			int num = 0;
			while (!flag && num < AsAggregateType().GetTypeArgsAll().size)
			{
				flag |= AsAggregateType().GetTypeArgsAll().Item(num).computeCurrentBogusState();
				num++;
			}
			break;
		}
		case TypeKind.TK_VoidType:
		case TypeKind.TK_NullType:
		case TypeKind.TK_OpenTypePlaceholderType:
		case TypeKind.TK_NaturalIntegerType:
		case TypeKind.TK_ArgumentListType:
		case TypeKind.TK_TypeParameterType:
			setBogus(isBogus: false);
			break;
		default:
			throw Error.InternalCompilerError();
		}
		if (flag)
		{
			setBogus(flag);
		}
		if (hasBogus())
		{
			return checkBogus();
		}
		return false;
	}

	public CType GetBaseOrParameterOrElementType()
	{
		return GetTypeKind() switch
		{
			TypeKind.TK_ArrayType => AsArrayType().GetElementType(), 
			TypeKind.TK_PointerType => AsPointerType().GetReferentType(), 
			TypeKind.TK_ParameterModifierType => AsParameterModifierType().GetParameterType(), 
			TypeKind.TK_NullableType => AsNullableType().GetUnderlyingType(), 
			_ => null, 
		};
	}

	public void InitFromParent()
	{
		CType cType = null;
		cType = ((!IsErrorType()) ? GetBaseOrParameterOrElementType() : AsErrorType().GetTypeParent());
		fHasErrors = cType.HasErrors();
		fUnres = cType.IsUnresolved();
	}

	public bool HasErrors()
	{
		return fHasErrors;
	}

	public void SetErrors(bool fHasErrors)
	{
		this.fHasErrors = fHasErrors;
	}

	public bool IsUnresolved()
	{
		return fUnres;
	}

	public void SetUnresolved(bool fUnres)
	{
		this.fUnres = fUnres;
	}

	public FUNDTYPE fundType()
	{
		switch (GetTypeKind())
		{
		case TypeKind.TK_AggregateType:
		{
			AggregateSymbol aggregate = AsAggregateType().getAggregate();
			if (aggregate.IsEnum())
			{
				aggregate = aggregate.GetUnderlyingType().getAggregate();
			}
			if (aggregate.IsStruct())
			{
				if (aggregate.IsPredefined())
				{
					return PredefinedTypeFacts.GetFundType(aggregate.GetPredefType());
				}
				return FUNDTYPE.FT_STRUCT;
			}
			return FUNDTYPE.FT_REF;
		}
		case TypeKind.TK_TypeParameterType:
			return FUNDTYPE.FT_VAR;
		case TypeKind.TK_NullType:
		case TypeKind.TK_ArrayType:
			return FUNDTYPE.FT_REF;
		case TypeKind.TK_PointerType:
			return FUNDTYPE.FT_PTR;
		case TypeKind.TK_NullableType:
			return FUNDTYPE.FT_STRUCT;
		default:
			return FUNDTYPE.FT_NONE;
		}
	}

	public ConstValKind constValKind()
	{
		if (isPointerLike())
		{
			return ConstValKind.IntPtr;
		}
		switch (fundType())
		{
		case FUNDTYPE.FT_I8:
		case FUNDTYPE.FT_U8:
			return ConstValKind.Long;
		case FUNDTYPE.FT_STRUCT:
			if (isPredefined() && getPredefType() == PredefinedType.PT_DATETIME)
			{
				return ConstValKind.Long;
			}
			return ConstValKind.Decimal;
		case FUNDTYPE.FT_REF:
			if (isPredefined() && getPredefType() == PredefinedType.PT_STRING)
			{
				return ConstValKind.String;
			}
			return ConstValKind.IntPtr;
		case FUNDTYPE.FT_R4:
			return ConstValKind.Float;
		case FUNDTYPE.FT_R8:
			return ConstValKind.Double;
		case FUNDTYPE.FT_I1:
			return ConstValKind.Boolean;
		default:
			return ConstValKind.Int;
		}
	}

	public CType underlyingType()
	{
		if (IsAggregateType() && getAggregate().IsEnum())
		{
			return getAggregate().GetUnderlyingType();
		}
		return this;
	}

	public CType GetNakedType(bool fStripNub)
	{
		if (this == null)
		{
			return null;
		}
		CType cType = this;
		while (true)
		{
			switch (cType.GetTypeKind())
			{
			default:
				return cType;
			case TypeKind.TK_NullableType:
				if (!fStripNub)
				{
					return cType;
				}
				cType = cType.GetBaseOrParameterOrElementType();
				break;
			case TypeKind.TK_ArrayType:
			case TypeKind.TK_PointerType:
			case TypeKind.TK_ParameterModifierType:
				cType = cType.GetBaseOrParameterOrElementType();
				break;
			}
		}
	}

	public AggregateSymbol GetNakedAgg()
	{
		return GetNakedAgg(fStripNub: false);
	}

	public AggregateSymbol GetNakedAgg(bool fStripNub)
	{
		CType nakedType = GetNakedType(fStripNub);
		if (nakedType != null && nakedType.IsAggregateType())
		{
			return nakedType.AsAggregateType().getAggregate();
		}
		return null;
	}

	public AggregateSymbol getAggregate()
	{
		return AsAggregateType().GetOwningAggregate();
	}

	public CType StripNubs()
	{
		if (this == null)
		{
			return null;
		}
		CType cType = this;
		while (cType.IsNullableType())
		{
			cType = cType.AsNullableType().GetUnderlyingType();
		}
		return cType;
	}

	public CType StripNubs(out int pcnub)
	{
		pcnub = 0;
		if (this == null)
		{
			return null;
		}
		CType cType = this;
		while (cType.IsNullableType())
		{
			pcnub++;
			cType = cType.AsNullableType().GetUnderlyingType();
		}
		return cType;
	}

	public bool isDelegateType()
	{
		if (IsAggregateType())
		{
			return getAggregate().IsDelegate();
		}
		return false;
	}

	public bool isSimpleType()
	{
		if (isPredefined())
		{
			return PredefinedTypeFacts.IsSimpleType(getPredefType());
		}
		return false;
	}

	public bool isSimpleOrEnum()
	{
		if (!isSimpleType())
		{
			return isEnumType();
		}
		return true;
	}

	public bool isSimpleOrEnumOrString()
	{
		if (!isSimpleType() && !isPredefType(PredefinedType.PT_STRING))
		{
			return isEnumType();
		}
		return true;
	}

	public bool isPointerLike()
	{
		if (!IsPointerType() && !isPredefType(PredefinedType.PT_INTPTR))
		{
			return isPredefType(PredefinedType.PT_UINTPTR);
		}
		return true;
	}

	public bool isNumericType()
	{
		if (isPredefined())
		{
			return PredefinedTypeFacts.IsNumericType(getPredefType());
		}
		return false;
	}

	public bool isStructOrEnum()
	{
		if (!IsAggregateType() || (!getAggregate().IsStruct() && !getAggregate().IsEnum()))
		{
			return IsNullableType();
		}
		return true;
	}

	public bool isStructType()
	{
		if (!IsAggregateType() || !getAggregate().IsStruct())
		{
			return IsNullableType();
		}
		return true;
	}

	public bool isEnumType()
	{
		if (IsAggregateType())
		{
			return getAggregate().IsEnum();
		}
		return false;
	}

	public bool isInterfaceType()
	{
		if (IsAggregateType())
		{
			return getAggregate().IsInterface();
		}
		return false;
	}

	public bool isClassType()
	{
		if (IsAggregateType())
		{
			return getAggregate().IsClass();
		}
		return false;
	}

	public AggregateType underlyingEnumType()
	{
		return getAggregate().GetUnderlyingType();
	}

	public bool isUnsigned()
	{
		if (IsAggregateType())
		{
			AggregateType aggregateType = AsAggregateType();
			if (aggregateType.isEnumType())
			{
				aggregateType = aggregateType.underlyingEnumType();
			}
			if (aggregateType.isPredefined())
			{
				PredefinedType predefType = aggregateType.getPredefType();
				switch (predefType)
				{
				default:
					return predefType <= PredefinedType.PT_ULONG;
				case PredefinedType.PT_SHORT:
				case PredefinedType.PT_INT:
				case PredefinedType.PT_LONG:
				case PredefinedType.PT_FLOAT:
				case PredefinedType.PT_DOUBLE:
				case PredefinedType.PT_DECIMAL:
				case PredefinedType.PT_CHAR:
				case PredefinedType.PT_BOOL:
				case PredefinedType.PT_SBYTE:
					return false;
				case PredefinedType.PT_BYTE:
				case PredefinedType.PT_UINTPTR:
					return true;
				}
			}
			return false;
		}
		return IsPointerType();
	}

	public bool isUnsafe()
	{
		if (this != null)
		{
			if (!IsPointerType())
			{
				if (IsArrayType())
				{
					return AsArrayType().GetElementType().isUnsafe();
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public bool isPredefType(PredefinedType pt)
	{
		if (this == null)
		{
			return false;
		}
		if (IsAggregateType())
		{
			if (AsAggregateType().getAggregate().IsPredefined())
			{
				return AsAggregateType().getAggregate().GetPredefType() == pt;
			}
			return false;
		}
		if (IsVoidType())
		{
			return pt == PredefinedType.PT_VOID;
		}
		return false;
	}

	public bool isPredefined()
	{
		if (IsAggregateType())
		{
			return getAggregate().IsPredefined();
		}
		return false;
	}

	public PredefinedType getPredefType()
	{
		return getAggregate().GetPredefType();
	}

	public bool isSpecialByRefType()
	{
		if (this == null)
		{
			return false;
		}
		if (isPredefined())
		{
			if (getPredefType() != PredefinedType.PT_REFANY && getPredefType() != PredefinedType.PT_ARGITERATOR)
			{
				return getPredefType() == PredefinedType.PT_ARGUMENTHANDLE;
			}
			return true;
		}
		return false;
	}

	public bool isStaticClass()
	{
		if (this == null)
		{
			return false;
		}
		AggregateSymbol nakedAgg = GetNakedAgg(fStripNub: false);
		if (nakedAgg == null)
		{
			return false;
		}
		if (!nakedAgg.IsStatic())
		{
			return false;
		}
		return true;
	}

	public bool computeManagedType(SymbolLoader symbolLoader)
	{
		if (IsVoidType())
		{
			return false;
		}
		switch (fundType())
		{
		case FUNDTYPE.FT_NONE:
		case FUNDTYPE.FT_REF:
		case FUNDTYPE.FT_VAR:
			return true;
		case FUNDTYPE.FT_STRUCT:
		{
			if (IsNullableType())
			{
				return true;
			}
			AggregateSymbol aggregate = getAggregate();
			if (aggregate.IsKnownManagedStructStatus())
			{
				return aggregate.IsManagedStruct();
			}
			if (aggregate.GetTypeVarsAll().size > 0)
			{
				aggregate.SetManagedStruct(managedStruct: true);
				return true;
			}
			if (aggregate.IsLayoutError())
			{
				aggregate.SetUnmanagedStruct(unmanagedStruct: true);
				return false;
			}
			if (symbolLoader != null)
			{
				for (Symbol symbol = aggregate.firstChild; symbol != null; symbol = symbol.nextChild)
				{
					if (symbol.IsFieldSymbol() && !symbol.AsFieldSymbol().isStatic)
					{
						CType type = symbol.AsFieldSymbol().GetType();
						if (type.computeManagedType(symbolLoader))
						{
							aggregate.SetManagedStruct(managedStruct: true);
							return true;
						}
					}
				}
				aggregate.SetUnmanagedStruct(unmanagedStruct: true);
			}
			return false;
		}
		default:
			return false;
		}
	}

	public CType GetDelegateTypeOfPossibleExpression()
	{
		if (isPredefType(PredefinedType.PT_G_EXPRESSION))
		{
			return AsAggregateType().GetTypeArgsThis().Item(0);
		}
		return this;
	}

	public bool IsValType()
	{
		return GetTypeKind() switch
		{
			TypeKind.TK_TypeParameterType => AsTypeParameterType().IsValueType(), 
			TypeKind.TK_AggregateType => AsAggregateType().getAggregate().IsValueType(), 
			TypeKind.TK_NullableType => true, 
			_ => false, 
		};
	}

	public bool IsNonNubValType()
	{
		return GetTypeKind() switch
		{
			TypeKind.TK_TypeParameterType => AsTypeParameterType().IsNonNullableValueType(), 
			TypeKind.TK_AggregateType => AsAggregateType().getAggregate().IsValueType(), 
			TypeKind.TK_NullableType => false, 
			_ => false, 
		};
	}

	public bool IsRefType()
	{
		switch (GetTypeKind())
		{
		case TypeKind.TK_NullType:
		case TypeKind.TK_ArrayType:
			return true;
		case TypeKind.TK_TypeParameterType:
			return AsTypeParameterType().IsReferenceType();
		case TypeKind.TK_AggregateType:
			return AsAggregateType().getAggregate().IsRefType();
		default:
			return false;
		}
	}

	public bool IsNeverSameType()
	{
		if (!IsBoundLambdaType() && !IsMethodGroupType())
		{
			if (IsErrorType())
			{
				return !AsErrorType().HasParent();
			}
			return false;
		}
		return true;
	}
}
