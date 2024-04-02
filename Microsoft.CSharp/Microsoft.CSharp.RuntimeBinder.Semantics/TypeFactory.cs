using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class TypeFactory
{
	public AggregateType CreateAggregateType(Name name, AggregateSymbol parent, TypeArray typeArgsThis, AggregateType outerType)
	{
		AggregateType aggregateType = new AggregateType();
		aggregateType.outerType = outerType;
		aggregateType.SetOwningAggregate(parent);
		aggregateType.SetTypeArgsThis(typeArgsThis);
		aggregateType.SetName(name);
		aggregateType.SetTypeKind(TypeKind.TK_AggregateType);
		return aggregateType;
	}

	public TypeParameterType CreateTypeParameter(TypeParameterSymbol pSymbol)
	{
		TypeParameterType typeParameterType = new TypeParameterType();
		typeParameterType.SetTypeParameterSymbol(pSymbol);
		typeParameterType.SetUnresolved(pSymbol.parent != null && pSymbol.parent.IsAggregateSymbol() && pSymbol.parent.AsAggregateSymbol().IsUnresolved());
		typeParameterType.SetName(pSymbol.name);
		pSymbol.SetTypeParameterType(typeParameterType);
		typeParameterType.SetTypeKind(TypeKind.TK_TypeParameterType);
		return typeParameterType;
	}

	public VoidType CreateVoid()
	{
		VoidType voidType = new VoidType();
		voidType.SetTypeKind(TypeKind.TK_VoidType);
		return voidType;
	}

	public NullType CreateNull()
	{
		NullType nullType = new NullType();
		nullType.SetTypeKind(TypeKind.TK_NullType);
		return nullType;
	}

	public OpenTypePlaceholderType CreateUnit()
	{
		OpenTypePlaceholderType openTypePlaceholderType = new OpenTypePlaceholderType();
		openTypePlaceholderType.SetTypeKind(TypeKind.TK_OpenTypePlaceholderType);
		return openTypePlaceholderType;
	}

	public BoundLambdaType CreateAnonMethod()
	{
		BoundLambdaType boundLambdaType = new BoundLambdaType();
		boundLambdaType.SetTypeKind(TypeKind.TK_BoundLambdaType);
		return boundLambdaType;
	}

	public MethodGroupType CreateMethodGroup()
	{
		MethodGroupType methodGroupType = new MethodGroupType();
		methodGroupType.SetTypeKind(TypeKind.TK_MethodGroupType);
		return methodGroupType;
	}

	public ArgumentListType CreateArgList()
	{
		ArgumentListType argumentListType = new ArgumentListType();
		argumentListType.SetTypeKind(TypeKind.TK_ArgumentListType);
		return argumentListType;
	}

	public ErrorType CreateError(Name name, CType parent, AssemblyQualifiedNamespaceSymbol pParentNS, Name nameText, TypeArray typeArgs)
	{
		ErrorType errorType = new ErrorType();
		errorType.SetName(name);
		errorType.nameText = nameText;
		errorType.typeArgs = typeArgs;
		errorType.SetTypeParent(parent);
		errorType.SetNSParent(pParentNS);
		errorType.SetTypeKind(TypeKind.TK_ErrorType);
		return errorType;
	}

	public ArrayType CreateArray(Name name, CType pElementType, int rank)
	{
		ArrayType arrayType = new ArrayType();
		arrayType.SetName(name);
		arrayType.rank = rank;
		arrayType.SetElementType(pElementType);
		arrayType.SetTypeKind(TypeKind.TK_ArrayType);
		return arrayType;
	}

	public PointerType CreatePointer(Name name, CType pReferentType)
	{
		PointerType pointerType = new PointerType();
		pointerType.SetName(name);
		pointerType.SetReferentType(pReferentType);
		pointerType.SetTypeKind(TypeKind.TK_PointerType);
		return pointerType;
	}

	public ParameterModifierType CreateParameterModifier(Name name, CType pParameterType)
	{
		ParameterModifierType parameterModifierType = new ParameterModifierType();
		parameterModifierType.SetName(name);
		parameterModifierType.SetParameterType(pParameterType);
		parameterModifierType.SetTypeKind(TypeKind.TK_ParameterModifierType);
		return parameterModifierType;
	}

	public NullableType CreateNullable(Name name, CType pUnderlyingType, BSYMMGR symmgr, TypeManager typeManager)
	{
		NullableType nullableType = new NullableType();
		nullableType.SetName(name);
		nullableType.SetUnderlyingType(pUnderlyingType);
		nullableType.symmgr = symmgr;
		nullableType.typeManager = typeManager;
		nullableType.SetTypeKind(TypeKind.TK_NullableType);
		return nullableType;
	}
}
