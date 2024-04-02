using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal static class TypeBind
{
	public static bool CheckConstraints(CSemanticChecker checker, ErrorHandling errHandling, CType type, CheckConstraintsFlags flags)
	{
		type = type.GetNakedType(fStripNub: false);
		if (type.IsNullableType())
		{
			CType ats = type.AsNullableType().GetAts(checker.GetErrorContext());
			type = ((ats == null) ? type.GetNakedType(fStripNub: true) : ats);
		}
		if (!type.IsAggregateType())
		{
			return true;
		}
		AggregateType aggregateType = type.AsAggregateType();
		if (aggregateType.GetTypeArgsAll().size == 0)
		{
			aggregateType.fConstraintsChecked = true;
			aggregateType.fConstraintError = false;
			return true;
		}
		if (aggregateType.fConstraintsChecked && (!aggregateType.fConstraintError || (flags & CheckConstraintsFlags.NoDupErrors) != 0))
		{
			return !aggregateType.fConstraintError;
		}
		TypeArray typeVars = aggregateType.getAggregate().GetTypeVars();
		TypeArray typeArgsThis = aggregateType.GetTypeArgsThis();
		TypeArray typeArgsAll = aggregateType.GetTypeArgsAll();
		if (!aggregateType.fConstraintsChecked)
		{
			aggregateType.fConstraintsChecked = true;
			aggregateType.fConstraintError = false;
		}
		if (aggregateType.outerType != null && ((flags & CheckConstraintsFlags.Outer) != 0 || !aggregateType.outerType.fConstraintsChecked))
		{
			CheckConstraints(checker, errHandling, aggregateType.outerType, flags);
			aggregateType.fConstraintError |= aggregateType.outerType.fConstraintError;
		}
		if (typeVars.size > 0)
		{
			aggregateType.fConstraintError |= !CheckConstraintsCore(checker, errHandling, aggregateType.getAggregate(), typeVars, typeArgsThis, typeArgsAll, null, flags & CheckConstraintsFlags.NoErrors);
		}
		for (int i = 0; i < typeArgsThis.size; i++)
		{
			CType nakedType = typeArgsThis.Item(i).GetNakedType(fStripNub: true);
			if (nakedType.IsAggregateType() && !nakedType.AsAggregateType().fConstraintsChecked)
			{
				CheckConstraints(checker, errHandling, nakedType.AsAggregateType(), flags | CheckConstraintsFlags.Outer);
				if (nakedType.AsAggregateType().fConstraintError)
				{
					aggregateType.fConstraintError = true;
				}
			}
		}
		return !aggregateType.fConstraintError;
	}

	public static void CheckMethConstraints(CSemanticChecker checker, ErrorHandling errCtx, MethWithInst mwi)
	{
		if (mwi.TypeArgs.size > 0)
		{
			CheckConstraintsCore(checker, errCtx, mwi.Meth(), mwi.Meth().typeVars, mwi.TypeArgs, mwi.GetType().GetTypeArgsAll(), mwi.TypeArgs, CheckConstraintsFlags.None);
		}
	}

	private static bool CheckConstraintsCore(CSemanticChecker checker, ErrorHandling errHandling, Symbol symErr, TypeArray typeVars, TypeArray typeArgs, TypeArray typeArgsCls, TypeArray typeArgsMeth, CheckConstraintsFlags flags)
	{
		bool flag = false;
		for (int i = 0; i < typeVars.size; i++)
		{
			TypeParameterType var = typeVars.ItemAsTypeParameterType(i);
			CType arg = typeArgs.Item(i);
			bool flag2 = CheckSingleConstraint(checker, errHandling, symErr, var, arg, typeArgsCls, typeArgsMeth, flags);
			flag = flag || !flag2;
		}
		return !flag;
	}

	private static bool CheckSingleConstraint(CSemanticChecker checker, ErrorHandling errHandling, Symbol symErr, TypeParameterType var, CType arg, TypeArray typeArgsCls, TypeArray typeArgsMeth, CheckConstraintsFlags flags)
	{
		bool flag = (flags & CheckConstraintsFlags.NoErrors) == 0;
		if (arg.IsOpenTypePlaceholderType())
		{
			return true;
		}
		if (arg.IsErrorType())
		{
			return false;
		}
		if (checker.CheckBogus(arg))
		{
			if (flag)
			{
				errHandling.ErrorRef(ErrorCode.ERR_BogusType, arg);
			}
			return false;
		}
		if (arg.IsPointerType() || arg.isSpecialByRefType())
		{
			if (flag)
			{
				errHandling.Error(ErrorCode.ERR_BadTypeArgument, arg);
			}
			return false;
		}
		if (arg.isStaticClass())
		{
			if (flag)
			{
				checker.ReportStaticClassError(null, arg, ErrorCode.ERR_GenericArgIsStaticClass);
			}
			return false;
		}
		bool flag2 = false;
		if (var.HasRefConstraint() && !arg.IsRefType())
		{
			if (flag)
			{
				errHandling.ErrorRef(ErrorCode.ERR_RefConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
			}
			flag2 = true;
		}
		TypeArray typeArray = checker.GetSymbolLoader().GetTypeManager().SubstTypeArray(var.GetBounds(), typeArgsCls, typeArgsMeth);
		int num = 0;
		if (var.HasValConstraint())
		{
			bool flag3 = arg.IsValType();
			bool flag4 = arg.IsNullableType();
			if (flag3 && arg.IsTypeParameterType())
			{
				TypeArray bounds = arg.AsTypeParameterType().GetBounds();
				if (bounds.size > 0)
				{
					flag4 = bounds.Item(0).IsNullableType();
				}
			}
			if (!flag3 || flag4)
			{
				if (flag)
				{
					errHandling.ErrorRef(ErrorCode.ERR_ValConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
				}
				flag2 = true;
			}
			if (typeArray.size != 0 && typeArray.Item(0).isPredefType(PredefinedType.PT_VALUE))
			{
				num = 1;
			}
		}
		for (int i = num; i < typeArray.size; i++)
		{
			CType cType = typeArray.Item(i);
			if (!SatisfiesBound(checker, arg, cType))
			{
				if (flag)
				{
					ErrorCode id = (arg.IsRefType() ? ErrorCode.ERR_GenericConstraintNotSatisfiedRefType : ((arg.IsNullableType() && checker.GetSymbolLoader().HasBaseConversion(arg.AsNullableType().GetUnderlyingType(), cType)) ? ((!cType.isPredefType(PredefinedType.PT_ENUM) && arg.AsNullableType().GetUnderlyingType() != cType) ? ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface : ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum) : ((!arg.IsTypeParameterType()) ? ErrorCode.ERR_GenericConstraintNotSatisfiedValType : ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar)));
					errHandling.Error(id, new ErrArgRef(symErr), new ErrArg(cType, ErrArgFlags.Unique), var, new ErrArgRef(arg, ErrArgFlags.Unique));
				}
				flag2 = true;
			}
		}
		if (!var.HasNewConstraint() || arg.IsValType())
		{
			return !flag2;
		}
		if (arg.isClassType())
		{
			AggregateSymbol aggregate = arg.AsAggregateType().getAggregate();
			checker.GetSymbolLoader().LookupAggMember(checker.GetNameManager().GetPredefName(PredefinedName.PN_CTOR), aggregate, symbmask_t.MASK_ALL);
			if (aggregate.HasPubNoArgCtor() && !aggregate.IsAbstract())
			{
				return !flag2;
			}
		}
		else if (arg.IsTypeParameterType() && arg.AsTypeParameterType().HasNewConstraint())
		{
			return !flag2;
		}
		if (flag)
		{
			errHandling.ErrorRef(ErrorCode.ERR_NewConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
		}
		return false;
	}

	private static bool SatisfiesBound(CSemanticChecker checker, CType arg, CType typeBnd)
	{
		if (typeBnd == arg)
		{
			return true;
		}
		switch (typeBnd.GetTypeKind())
		{
		default:
			return false;
		case TypeKind.TK_VoidType:
		case TypeKind.TK_ErrorType:
		case TypeKind.TK_PointerType:
			return false;
		case TypeKind.TK_NullableType:
			typeBnd = typeBnd.AsNullableType().GetAts(checker.GetErrorContext());
			if (typeBnd == null)
			{
				return true;
			}
			break;
		case TypeKind.TK_AggregateType:
		case TypeKind.TK_ArrayType:
		case TypeKind.TK_TypeParameterType:
			break;
		}
		switch (arg.GetTypeKind())
		{
		default:
			return false;
		case TypeKind.TK_ErrorType:
		case TypeKind.TK_PointerType:
			return false;
		case TypeKind.TK_NullableType:
			arg = arg.AsNullableType().GetAts(checker.GetErrorContext());
			if (arg == null)
			{
				return true;
			}
			break;
		case TypeKind.TK_AggregateType:
		case TypeKind.TK_ArrayType:
		case TypeKind.TK_TypeParameterType:
			break;
		}
		return checker.GetSymbolLoader().HasBaseConversion(arg, typeBnd);
	}
}
