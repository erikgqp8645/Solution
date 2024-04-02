using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class MethodTypeInferrer
{
	private enum NewInferenceResult
	{
		InferenceFailed,
		MadeProgress,
		NoProgress,
		Success
	}

	private enum Dependency
	{
		Unknown = 0,
		NotDependent = 1,
		DependsMask = 16,
		Direct = 17,
		Indirect = 18
	}

	private SymbolLoader symbolLoader;

	private ExpressionBinder binder;

	private TypeArray pMethodTypeParameters;

	private TypeArray pClassTypeArguments;

	private TypeArray pMethodFormalParameterTypes;

	private ArgInfos pMethodArguments;

	private List<CType>[] pExactBounds;

	private List<CType>[] pUpperBounds;

	private List<CType>[] pLowerBounds;

	private CType[] pFixedResults;

	private Dependency[,] ppDependencies;

	private bool dependenciesDirty;

	public static bool Infer(ExpressionBinder binder, SymbolLoader symbolLoader, MethodSymbol pMethod, TypeArray pClassTypeArguments, TypeArray pMethodFormalParameterTypes, ArgInfos pMethodArguments, out TypeArray ppInferredTypeArguments)
	{
		ppInferredTypeArguments = null;
		if (pMethodFormalParameterTypes.size == 0 || pMethod.InferenceMustFail())
		{
			return false;
		}
		MethodTypeInferrer methodTypeInferrer = new MethodTypeInferrer(binder, symbolLoader, pMethodFormalParameterTypes, pMethodArguments, pMethod.typeVars, pClassTypeArguments);
		bool result = ((!pMethodArguments.fHasExprs) ? methodTypeInferrer.InferForMethodGroupConversion() : methodTypeInferrer.InferTypeArgs());
		ppInferredTypeArguments = methodTypeInferrer.GetResults();
		return result;
	}

	private MethodTypeInferrer(ExpressionBinder exprBinder, SymbolLoader symLoader, TypeArray pMethodFormalParameterTypes, ArgInfos pMethodArguments, TypeArray pMethodTypeParameters, TypeArray pClassTypeArguments)
	{
		binder = exprBinder;
		symbolLoader = symLoader;
		this.pMethodFormalParameterTypes = pMethodFormalParameterTypes;
		this.pMethodArguments = pMethodArguments;
		this.pMethodTypeParameters = pMethodTypeParameters;
		this.pClassTypeArguments = pClassTypeArguments;
		pFixedResults = new CType[pMethodTypeParameters.size];
		pLowerBounds = new List<CType>[pMethodTypeParameters.size];
		pUpperBounds = new List<CType>[pMethodTypeParameters.size];
		pExactBounds = new List<CType>[pMethodTypeParameters.size];
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			pLowerBounds[i] = new List<CType>();
			pUpperBounds[i] = new List<CType>();
			pExactBounds[i] = new List<CType>();
		}
		ppDependencies = null;
	}

	private TypeArray GetResults()
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (pFixedResults[i] != null)
			{
				if (!pFixedResults[i].IsErrorType())
				{
					continue;
				}
				Name nameText = pFixedResults[i].AsErrorType().nameText;
				if (nameText != null && nameText != GetGlobalSymbols().GetNameManager().GetPredefName(PredefinedName.PN_MISSING))
				{
					continue;
				}
			}
			pFixedResults[i] = GetTypeManager().GetErrorType(null, null, pMethodTypeParameters.ItemAsTypeParameterType(i).GetName(), BSYMMGR.EmptyTypeArray());
		}
		return GetGlobalSymbols().AllocParams(pMethodTypeParameters.size, pFixedResults);
	}

	private bool IsUnfixed(int iParam)
	{
		return pFixedResults[iParam] == null;
	}

	private bool IsUnfixed(TypeParameterType pParam)
	{
		int indexInTotalParameters = pParam.GetIndexInTotalParameters();
		return IsUnfixed(indexInTotalParameters);
	}

	private bool AllFixed()
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (IsUnfixed(i))
			{
				return false;
			}
		}
		return true;
	}

	private void AddLowerBound(TypeParameterType pParam, CType pBound)
	{
		int indexInTotalParameters = pParam.GetIndexInTotalParameters();
		if (!pLowerBounds[indexInTotalParameters].Contains(pBound))
		{
			pLowerBounds[indexInTotalParameters].Add(pBound);
		}
	}

	private void AddUpperBound(TypeParameterType pParam, CType pBound)
	{
		int indexInTotalParameters = pParam.GetIndexInTotalParameters();
		if (!pUpperBounds[indexInTotalParameters].Contains(pBound))
		{
			pUpperBounds[indexInTotalParameters].Add(pBound);
		}
	}

	private void AddExactBound(TypeParameterType pParam, CType pBound)
	{
		int indexInTotalParameters = pParam.GetIndexInTotalParameters();
		if (!pExactBounds[indexInTotalParameters].Contains(pBound))
		{
			pExactBounds[indexInTotalParameters].Add(pBound);
		}
	}

	private bool HasBound(int iParam)
	{
		if (pLowerBounds[iParam].IsEmpty() && pExactBounds[iParam].IsEmpty())
		{
			return !pUpperBounds[iParam].IsEmpty();
		}
		return true;
	}

	private TypeArray GetFixedDelegateParameters(AggregateType pDelegateType)
	{
		CType[] array = new CType[pMethodTypeParameters.size];
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			TypeParameterType typeParameterType = pMethodTypeParameters.ItemAsTypeParameterType(i);
			array[i] = (IsUnfixed(i) ? typeParameterType : pFixedResults[i]);
		}
		SubstContext pctx = new SubstContext(pClassTypeArguments.ToArray(), pClassTypeArguments.size, array, pMethodTypeParameters.size);
		AggregateType aggregateType = GetTypeManager().SubstType(pDelegateType, pctx).AsAggregateType();
		return aggregateType.GetDelegateParameters(GetSymbolLoader());
	}

	private bool InferTypeArgs()
	{
		InferTypeArgsFirstPhase();
		return InferTypeArgsSecondPhase();
	}

	private static bool IsReallyAType(CType pType)
	{
		if (pType.IsNullType() || pType.IsBoundLambdaType() || pType.IsVoidType() || pType.IsMethodGroupType())
		{
			return false;
		}
		return true;
	}

	private void InferTypeArgsFirstPhase()
	{
		for (int i = 0; i < pMethodArguments.carg; i++)
		{
			EXPR eXPR = pMethodArguments.prgexpr[i];
			if (eXPR.IsOptionalArgument)
			{
				continue;
			}
			CType cType = pMethodFormalParameterTypes.Item(i);
			CType cType2 = ((eXPR.RuntimeObjectActualType != null) ? eXPR.RuntimeObjectActualType : pMethodArguments.types.Item(i));
			bool flag = false;
			if (cType.IsParameterModifierType())
			{
				cType = cType.AsParameterModifierType().GetParameterType();
				flag = true;
			}
			if (cType2.IsParameterModifierType())
			{
				cType2 = cType2.AsParameterModifierType().GetParameterType();
			}
			if (IsReallyAType(cType2))
			{
				if (flag)
				{
					ExactInference(cType2, cType);
				}
				else
				{
					LowerBoundInference(cType2, cType);
				}
			}
		}
	}

	private bool InferTypeArgsSecondPhase()
	{
		InitializeDependencies();
		while (true)
		{
			switch (DoSecondPhase())
			{
			case NewInferenceResult.InferenceFailed:
				return false;
			case NewInferenceResult.Success:
				return true;
			}
		}
	}

	private NewInferenceResult DoSecondPhase()
	{
		if (AllFixed())
		{
			return NewInferenceResult.Success;
		}
		MakeOutputTypeInferences();
		NewInferenceResult newInferenceResult = FixNondependentParameters();
		if (newInferenceResult != NewInferenceResult.NoProgress)
		{
			return newInferenceResult;
		}
		newInferenceResult = FixDependentParameters();
		if (newInferenceResult != NewInferenceResult.NoProgress)
		{
			return newInferenceResult;
		}
		return NewInferenceResult.InferenceFailed;
	}

	private void MakeOutputTypeInferences()
	{
		for (int i = 0; i < pMethodArguments.carg; i++)
		{
			CType cType = pMethodFormalParameterTypes.Item(i);
			if (cType.IsParameterModifierType())
			{
				cType = cType.AsParameterModifierType().GetParameterType();
			}
			EXPR eXPR = pMethodArguments.prgexpr[i];
			if (HasUnfixedParamInOutputType(eXPR, cType) && !HasUnfixedParamInInputType(eXPR, cType))
			{
				CType cType2 = pMethodArguments.types.Item(i);
				if (cType2.IsParameterModifierType())
				{
					cType2 = cType2.AsParameterModifierType().GetParameterType();
				}
				OutputTypeInference(eXPR, cType2, cType);
			}
		}
	}

	private NewInferenceResult FixNondependentParameters()
	{
		bool[] array = new bool[pMethodTypeParameters.size];
		NewInferenceResult result = NewInferenceResult.NoProgress;
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (IsUnfixed(i) && HasBound(i) && !DependsOnAny(i))
			{
				array[i] = true;
				result = NewInferenceResult.MadeProgress;
			}
		}
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (array[i] && !Fix(i))
			{
				result = NewInferenceResult.InferenceFailed;
			}
		}
		return result;
	}

	private NewInferenceResult FixDependentParameters()
	{
		bool[] array = new bool[pMethodTypeParameters.size];
		NewInferenceResult result = NewInferenceResult.NoProgress;
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (IsUnfixed(i) && HasBound(i) && AnyDependsOn(i))
			{
				array[i] = true;
				result = NewInferenceResult.MadeProgress;
			}
		}
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (array[i] && !Fix(i))
			{
				result = NewInferenceResult.InferenceFailed;
			}
		}
		return result;
	}

	private bool DoesInputTypeContain(EXPR pSource, CType pDest, TypeParameterType pParam)
	{
		pDest = pDest.GetDelegateTypeOfPossibleExpression();
		if (!pDest.isDelegateType())
		{
			return false;
		}
		if (!pSource.isUNBOUNDLAMBDA() && !pSource.isMEMGRP())
		{
			return false;
		}
		TypeArray delegateParameters = pDest.AsAggregateType().GetDelegateParameters(GetSymbolLoader());
		if (delegateParameters == null)
		{
			return false;
		}
		return TypeManager.ParametersContainTyVar(delegateParameters, pParam);
	}

	private bool HasUnfixedParamInInputType(EXPR pSource, CType pDest)
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (IsUnfixed(i) && DoesInputTypeContain(pSource, pDest, pMethodTypeParameters.ItemAsTypeParameterType(i)))
			{
				return true;
			}
		}
		return false;
	}

	private bool DoesOutputTypeContain(EXPR pSource, CType pDest, TypeParameterType pParam)
	{
		pDest = pDest.GetDelegateTypeOfPossibleExpression();
		if (!pDest.isDelegateType())
		{
			return false;
		}
		if (!pSource.isUNBOUNDLAMBDA() && !pSource.isMEMGRP())
		{
			return false;
		}
		CType delegateReturnType = pDest.AsAggregateType().GetDelegateReturnType(GetSymbolLoader());
		if (delegateReturnType == null)
		{
			return false;
		}
		return TypeManager.TypeContainsType(delegateReturnType, pParam);
	}

	private bool HasUnfixedParamInOutputType(EXPR pSource, CType pDest)
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (IsUnfixed(i) && DoesOutputTypeContain(pSource, pDest, pMethodTypeParameters.ItemAsTypeParameterType(i)))
			{
				return true;
			}
		}
		return false;
	}

	private bool DependsDirectlyOn(int iParam, int jParam)
	{
		for (int i = 0; i < pMethodArguments.carg; i++)
		{
			CType cType = pMethodFormalParameterTypes.Item(i);
			if (cType.IsParameterModifierType())
			{
				cType = cType.AsParameterModifierType().GetParameterType();
			}
			EXPR pSource = pMethodArguments.prgexpr[i];
			if (DoesInputTypeContain(pSource, cType, pMethodTypeParameters.ItemAsTypeParameterType(jParam)) && DoesOutputTypeContain(pSource, cType, pMethodTypeParameters.ItemAsTypeParameterType(iParam)))
			{
				return true;
			}
		}
		return false;
	}

	private void InitializeDependencies()
	{
		ppDependencies = new Dependency[pMethodTypeParameters.size, pMethodTypeParameters.size];
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			for (int j = 0; j < pMethodTypeParameters.size; j++)
			{
				if (DependsDirectlyOn(i, j))
				{
					ppDependencies[i, j] = Dependency.Direct;
				}
			}
		}
		DeduceAllDependencies();
	}

	private bool DependsOn(int iParam, int jParam)
	{
		if (dependenciesDirty)
		{
			SetIndirectsToUnknown();
			DeduceAllDependencies();
		}
		return (ppDependencies[iParam, jParam] & Dependency.DependsMask) != 0;
	}

	private bool DependsTransitivelyOn(int iParam, int jParam)
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if ((ppDependencies[iParam, i] & Dependency.DependsMask) != 0 && (ppDependencies[i, jParam] & Dependency.DependsMask) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private void DeduceAllDependencies()
	{
		while (DeduceDependencies())
		{
		}
		SetUnknownsToNotDependent();
		dependenciesDirty = false;
	}

	private bool DeduceDependencies()
	{
		bool result = false;
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			for (int j = 0; j < pMethodTypeParameters.size; j++)
			{
				if (ppDependencies[i, j] == Dependency.Unknown && DependsTransitivelyOn(i, j))
				{
					ppDependencies[i, j] = Dependency.Indirect;
					result = true;
				}
			}
		}
		return result;
	}

	private void SetUnknownsToNotDependent()
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			for (int j = 0; j < pMethodTypeParameters.size; j++)
			{
				if (ppDependencies[i, j] == Dependency.Unknown)
				{
					ppDependencies[i, j] = Dependency.NotDependent;
				}
			}
		}
	}

	private void SetIndirectsToUnknown()
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			for (int j = 0; j < pMethodTypeParameters.size; j++)
			{
				if (ppDependencies[i, j] == Dependency.Indirect)
				{
					ppDependencies[i, j] = Dependency.Unknown;
				}
			}
		}
	}

	private void UpdateDependenciesAfterFix(int iParam)
	{
		if (ppDependencies != null)
		{
			for (int i = 0; i < pMethodTypeParameters.size; i++)
			{
				ppDependencies[iParam, i] = Dependency.NotDependent;
				ppDependencies[i, iParam] = Dependency.NotDependent;
			}
			dependenciesDirty = true;
		}
	}

	private bool DependsOnAny(int iParam)
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (DependsOn(iParam, i))
			{
				return true;
			}
		}
		return false;
	}

	private bool AnyDependsOn(int iParam)
	{
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			if (DependsOn(i, iParam))
			{
				return true;
			}
		}
		return false;
	}

	private void OutputTypeInference(EXPR pExpr, CType pSource, CType pDest)
	{
		if (!MethodGroupReturnTypeInference(pExpr, pDest) && IsReallyAType(pSource))
		{
			LowerBoundInference(pSource, pDest);
		}
	}

	private bool MethodGroupReturnTypeInference(EXPR pSource, CType pType)
	{
		if (!pSource.isMEMGRP())
		{
			return false;
		}
		pType = pType.GetDelegateTypeOfPossibleExpression();
		if (!pType.isDelegateType())
		{
			return false;
		}
		AggregateType aggregateType = pType.AsAggregateType();
		CType delegateReturnType = aggregateType.GetDelegateReturnType(GetSymbolLoader());
		if (delegateReturnType == null)
		{
			return false;
		}
		if (delegateReturnType.IsVoidType())
		{
			return false;
		}
		TypeArray fixedDelegateParameters = GetFixedDelegateParameters(aggregateType);
		if (fixedDelegateParameters == null)
		{
			return false;
		}
		ArgInfos args = new ArgInfos
		{
			carg = fixedDelegateParameters.size,
			types = fixedDelegateParameters,
			fHasExprs = false,
			prgexpr = null
		};
		ExpressionBinder.GroupToArgsBinder groupToArgsBinder = new ExpressionBinder.GroupToArgsBinder(binder, (BindingFlag)0, pSource.asMEMGRP(), args, null, bHasNamedArguments: false, aggregateType);
		if (!groupToArgsBinder.Bind(bReportErrors: false))
		{
			return false;
		}
		MethPropWithInst bestResult = groupToArgsBinder.GetResultsOfBind().GetBestResult();
		CType cType = GetTypeManager().SubstType(bestResult.Meth().RetType, bestResult.GetType(), bestResult.TypeArgs);
		if (cType.IsVoidType())
		{
			return false;
		}
		LowerBoundInference(cType, delegateReturnType);
		return true;
	}

	private void ExactInference(CType pSource, CType pDest)
	{
		if (!ExactTypeParameterInference(pSource, pDest) && !ExactArrayInference(pSource, pDest) && !ExactNullableInference(pSource, pDest))
		{
			ExactConstructedInference(pSource, pDest);
		}
	}

	private bool ExactTypeParameterInference(CType pSource, CType pDest)
	{
		if (pDest.IsTypeParameterType())
		{
			TypeParameterType typeParameterType = pDest.AsTypeParameterType();
			if (typeParameterType.IsMethodTypeParameter() && IsUnfixed(typeParameterType))
			{
				AddExactBound(typeParameterType, pSource);
				return true;
			}
		}
		return false;
	}

	private bool ExactArrayInference(CType pSource, CType pDest)
	{
		if (!pSource.IsArrayType() || !pDest.IsArrayType())
		{
			return false;
		}
		ArrayType arrayType = pSource.AsArrayType();
		ArrayType arrayType2 = pDest.AsArrayType();
		if (arrayType.rank != arrayType2.rank)
		{
			return false;
		}
		ExactInference(arrayType.GetElementType(), arrayType2.GetElementType());
		return true;
	}

	private bool ExactNullableInference(CType pSource, CType pDest)
	{
		if (!pSource.IsNullableType() || !pDest.IsNullableType())
		{
			return false;
		}
		ExactInference(pSource.AsNullableType().GetUnderlyingType(), pDest.AsNullableType().GetUnderlyingType());
		return true;
	}

	private bool ExactConstructedInference(CType pSource, CType pDest)
	{
		if (!pSource.IsAggregateType() || !pDest.IsAggregateType())
		{
			return false;
		}
		AggregateType aggregateType = pSource.AsAggregateType();
		AggregateType aggregateType2 = pDest.AsAggregateType();
		if (aggregateType.GetOwningAggregate() != aggregateType2.GetOwningAggregate())
		{
			return false;
		}
		ExactTypeArgumentInference(aggregateType, aggregateType2);
		return true;
	}

	private void ExactTypeArgumentInference(AggregateType pSource, AggregateType pDest)
	{
		TypeArray typeArgsAll = pSource.GetTypeArgsAll();
		TypeArray typeArgsAll2 = pDest.GetTypeArgsAll();
		for (int i = 0; i < typeArgsAll.size; i++)
		{
			ExactInference(typeArgsAll.Item(i), typeArgsAll2.Item(i));
		}
	}

	private void LowerBoundInference(CType pSource, CType pDest)
	{
		if (!LowerBoundTypeParameterInference(pSource, pDest) && !LowerBoundArrayInference(pSource, pDest) && !ExactNullableInference(pSource, pDest))
		{
			LowerBoundConstructedInference(pSource, pDest);
		}
	}

	private bool LowerBoundTypeParameterInference(CType pSource, CType pDest)
	{
		if (pDest.IsTypeParameterType())
		{
			TypeParameterType typeParameterType = pDest.AsTypeParameterType();
			if (typeParameterType.IsMethodTypeParameter() && IsUnfixed(typeParameterType))
			{
				AddLowerBound(typeParameterType, pSource);
				return true;
			}
		}
		return false;
	}

	private bool LowerBoundArrayInference(CType pSource, CType pDest)
	{
		if (pSource.IsTypeParameterType())
		{
			pSource = pSource.AsTypeParameterType().GetEffectiveBaseClass();
		}
		if (!pSource.IsArrayType())
		{
			return false;
		}
		ArrayType arrayType = pSource.AsArrayType();
		CType elementType = arrayType.GetElementType();
		CType cType = null;
		if (pDest.IsArrayType())
		{
			ArrayType arrayType2 = pDest.AsArrayType();
			if (arrayType2.rank != arrayType.rank)
			{
				return false;
			}
			cType = arrayType2.GetElementType();
		}
		else
		{
			if (!pDest.isPredefType(PredefinedType.PT_G_IENUMERABLE) && !pDest.isPredefType(PredefinedType.PT_G_ICOLLECTION) && !pDest.isPredefType(PredefinedType.PT_G_ILIST) && !pDest.isPredefType(PredefinedType.PT_G_IREADONLYCOLLECTION) && !pDest.isPredefType(PredefinedType.PT_G_IREADONLYLIST))
			{
				return false;
			}
			if (arrayType.rank != 1)
			{
				return false;
			}
			AggregateType aggregateType = pDest.AsAggregateType();
			cType = aggregateType.GetTypeArgsThis().Item(0);
		}
		if (elementType.IsRefType())
		{
			LowerBoundInference(elementType, cType);
		}
		else
		{
			ExactInference(elementType, cType);
		}
		return true;
	}

	private bool LowerBoundConstructedInference(CType pSource, CType pDest)
	{
		if (!pDest.IsAggregateType())
		{
			return false;
		}
		AggregateType aggregateType = pDest.AsAggregateType();
		TypeArray typeArgsAll = aggregateType.GetTypeArgsAll();
		if (typeArgsAll.size == 0)
		{
			return false;
		}
		if (pSource.IsAggregateType() && pSource.AsAggregateType().GetOwningAggregate() == aggregateType.GetOwningAggregate())
		{
			if (pSource.isInterfaceType() || pSource.isDelegateType())
			{
				LowerBoundTypeArgumentInference(pSource.AsAggregateType(), aggregateType);
			}
			else
			{
				ExactTypeArgumentInference(pSource.AsAggregateType(), aggregateType);
			}
			return true;
		}
		if (LowerBoundClassInference(pSource, aggregateType))
		{
			return true;
		}
		if (LowerBoundInterfaceInference(pSource, aggregateType))
		{
			return true;
		}
		return false;
	}

	private bool LowerBoundClassInference(CType pSource, AggregateType pDest)
	{
		if (!pDest.isClassType())
		{
			return false;
		}
		AggregateType aggregateType = null;
		if (pSource.isClassType())
		{
			aggregateType = pSource.AsAggregateType().GetBaseClass();
		}
		else if (pSource.IsTypeParameterType())
		{
			aggregateType = pSource.AsTypeParameterType().GetEffectiveBaseClass();
		}
		while (aggregateType != null)
		{
			if (aggregateType.GetOwningAggregate() == pDest.GetOwningAggregate())
			{
				ExactTypeArgumentInference(aggregateType, pDest);
				return true;
			}
			aggregateType = aggregateType.GetBaseClass();
		}
		return false;
	}

	private bool LowerBoundInterfaceInference(CType pSource, AggregateType pDest)
	{
		if (!pDest.isInterfaceType())
		{
			return false;
		}
		if (!pSource.isStructType() && !pSource.isClassType() && !pSource.isInterfaceType() && !pSource.IsTypeParameterType())
		{
			return false;
		}
		IEnumerable<CType> enumerable = pSource.AllPossibleInterfaces();
		AggregateType aggregateType = null;
		foreach (AggregateType item in enumerable)
		{
			if (item.GetOwningAggregate() == pDest.GetOwningAggregate())
			{
				if (aggregateType == null)
				{
					aggregateType = item;
				}
				else if (aggregateType != item)
				{
					return false;
				}
			}
		}
		if (aggregateType == null)
		{
			return false;
		}
		LowerBoundTypeArgumentInference(aggregateType, pDest);
		return true;
	}

	private void LowerBoundTypeArgumentInference(AggregateType pSource, AggregateType pDest)
	{
		TypeArray typeVarsAll = pSource.GetOwningAggregate().GetTypeVarsAll();
		TypeArray typeArgsAll = pSource.GetTypeArgsAll();
		TypeArray typeArgsAll2 = pDest.GetTypeArgsAll();
		for (int i = 0; i < typeArgsAll.size; i++)
		{
			TypeParameterType typeParameterType = typeVarsAll.ItemAsTypeParameterType(i);
			CType cType = typeArgsAll.Item(i);
			CType pDest2 = typeArgsAll2.Item(i);
			if (cType.IsRefType() && typeParameterType.Covariant)
			{
				LowerBoundInference(cType, pDest2);
			}
			else if (cType.IsRefType() && typeParameterType.Contravariant)
			{
				UpperBoundInference(typeArgsAll.Item(i), typeArgsAll2.Item(i));
			}
			else
			{
				ExactInference(typeArgsAll.Item(i), typeArgsAll2.Item(i));
			}
		}
	}

	private void UpperBoundInference(CType pSource, CType pDest)
	{
		if (!UpperBoundTypeParameterInference(pSource, pDest) && !UpperBoundArrayInference(pSource, pDest) && !ExactNullableInference(pSource, pDest))
		{
			UpperBoundConstructedInference(pSource, pDest);
		}
	}

	private bool UpperBoundTypeParameterInference(CType pSource, CType pDest)
	{
		if (pDest.IsTypeParameterType())
		{
			TypeParameterType typeParameterType = pDest.AsTypeParameterType();
			if (typeParameterType.IsMethodTypeParameter() && IsUnfixed(typeParameterType))
			{
				AddUpperBound(typeParameterType, pSource);
				return true;
			}
		}
		return false;
	}

	private bool UpperBoundArrayInference(CType pSource, CType pDest)
	{
		if (!pDest.IsArrayType())
		{
			return false;
		}
		ArrayType arrayType = pDest.AsArrayType();
		CType elementType = arrayType.GetElementType();
		CType cType = null;
		if (pSource.IsArrayType())
		{
			ArrayType arrayType2 = pSource.AsArrayType();
			if (arrayType.rank != arrayType2.rank)
			{
				return false;
			}
			cType = arrayType2.GetElementType();
		}
		else
		{
			if (!pSource.isPredefType(PredefinedType.PT_G_IENUMERABLE) && !pSource.isPredefType(PredefinedType.PT_G_ICOLLECTION) && !pSource.isPredefType(PredefinedType.PT_G_ILIST) && !pSource.isPredefType(PredefinedType.PT_G_IREADONLYLIST) && !pSource.isPredefType(PredefinedType.PT_G_IREADONLYCOLLECTION))
			{
				return false;
			}
			if (arrayType.rank != 1)
			{
				return false;
			}
			AggregateType aggregateType = pSource.AsAggregateType();
			cType = aggregateType.GetTypeArgsThis().Item(0);
		}
		if (cType.IsRefType())
		{
			UpperBoundInference(cType, elementType);
		}
		else
		{
			ExactInference(cType, elementType);
		}
		return true;
	}

	private bool UpperBoundConstructedInference(CType pSource, CType pDest)
	{
		if (!pSource.IsAggregateType())
		{
			return false;
		}
		AggregateType aggregateType = pSource.AsAggregateType();
		TypeArray typeArgsAll = aggregateType.GetTypeArgsAll();
		if (typeArgsAll.size == 0)
		{
			return false;
		}
		if (pDest.IsAggregateType() && aggregateType.GetOwningAggregate() == pDest.AsAggregateType().GetOwningAggregate())
		{
			if (pDest.isInterfaceType() || pDest.isDelegateType())
			{
				UpperBoundTypeArgumentInference(aggregateType, pDest.AsAggregateType());
			}
			else
			{
				ExactTypeArgumentInference(aggregateType, pDest.AsAggregateType());
			}
			return true;
		}
		if (UpperBoundClassInference(aggregateType, pDest))
		{
			return true;
		}
		if (UpperBoundInterfaceInference(aggregateType, pDest))
		{
			return true;
		}
		return false;
	}

	private bool UpperBoundClassInference(AggregateType pSource, CType pDest)
	{
		if (!pSource.isClassType() || !pDest.isClassType())
		{
			return false;
		}
		for (AggregateType baseClass = pDest.AsAggregateType().GetBaseClass(); baseClass != null; baseClass = baseClass.GetBaseClass())
		{
			if (baseClass.GetOwningAggregate() == pSource.GetOwningAggregate())
			{
				ExactTypeArgumentInference(pSource, baseClass);
				return true;
			}
		}
		return false;
	}

	private bool UpperBoundInterfaceInference(AggregateType pSource, CType pDest)
	{
		if (!pSource.isInterfaceType())
		{
			return false;
		}
		if (!pDest.isStructType() && !pDest.isClassType() && !pDest.isInterfaceType())
		{
			return false;
		}
		IEnumerable<CType> enumerable = pDest.AllPossibleInterfaces();
		AggregateType aggregateType = null;
		foreach (AggregateType item in enumerable)
		{
			if (item.GetOwningAggregate() == pSource.GetOwningAggregate())
			{
				if (aggregateType == null)
				{
					aggregateType = item;
				}
				else if (aggregateType != item)
				{
					return false;
				}
			}
		}
		if (aggregateType == null)
		{
			return false;
		}
		UpperBoundTypeArgumentInference(aggregateType, pDest.AsAggregateType());
		return true;
	}

	private void UpperBoundTypeArgumentInference(AggregateType pSource, AggregateType pDest)
	{
		TypeArray typeVarsAll = pSource.GetOwningAggregate().GetTypeVarsAll();
		TypeArray typeArgsAll = pSource.GetTypeArgsAll();
		TypeArray typeArgsAll2 = pDest.GetTypeArgsAll();
		for (int i = 0; i < typeArgsAll.size; i++)
		{
			TypeParameterType typeParameterType = typeVarsAll.ItemAsTypeParameterType(i);
			CType cType = typeArgsAll.Item(i);
			CType pDest2 = typeArgsAll2.Item(i);
			if (cType.IsRefType() && typeParameterType.Covariant)
			{
				UpperBoundInference(cType, pDest2);
			}
			else if (cType.IsRefType() && typeParameterType.Contravariant)
			{
				LowerBoundInference(typeArgsAll.Item(i), typeArgsAll2.Item(i));
			}
			else
			{
				ExactInference(typeArgsAll.Item(i), typeArgsAll2.Item(i));
			}
		}
	}

	private bool Fix(int iParam)
	{
		if (pExactBounds[iParam].Count >= 2)
		{
			return false;
		}
		List<CType> list = new List<CType>();
		if (pExactBounds[iParam].IsEmpty())
		{
			HashSet<CType> hashSet = new HashSet<CType>();
			foreach (CType item in pLowerBounds[iParam])
			{
				if (!hashSet.Contains(item))
				{
					hashSet.Add(item);
					list.Add(item);
				}
			}
			foreach (CType item2 in pUpperBounds[iParam])
			{
				if (!hashSet.Contains(item2))
				{
					hashSet.Add(item2);
					list.Add(item2);
				}
			}
		}
		else
		{
			list.Add(pExactBounds[iParam].Head());
		}
		if (list.IsEmpty())
		{
			return false;
		}
		foreach (CType item3 in pLowerBounds[iParam])
		{
			List<CType> list2 = new List<CType>();
			foreach (CType item4 in list)
			{
				if (item3 != item4 && !binder.canConvert(item3, item4))
				{
					list2.Add(item4);
				}
			}
			foreach (CType item5 in list2)
			{
				list.Remove(item5);
			}
		}
		foreach (CType item6 in pUpperBounds[iParam])
		{
			List<CType> list3 = new List<CType>();
			foreach (CType item7 in list)
			{
				if (item6 != item7 && !binder.canConvert(item7, item6))
				{
					list3.Add(item7);
				}
			}
			foreach (CType item8 in list3)
			{
				list.Remove(item8);
			}
		}
		CType cType = null;
		foreach (CType item9 in list)
		{
			foreach (CType item10 in list)
			{
				if (item9 == item10 || binder.canConvert(item10, item9))
				{
					continue;
				}
				goto IL_02d2;
			}
			if (cType != null)
			{
				return false;
			}
			cType = item9;
			IL_02d2:;
		}
		if (cType == null)
		{
			return false;
		}
		if (GetTypeManager().GetBestAccessibleType(binder.GetSemanticChecker(), binder.GetContext(), cType, out var typeDst))
		{
			cType = typeDst;
			pFixedResults[iParam] = cType;
			UpdateDependenciesAfterFix(iParam);
			return true;
		}
		return false;
	}

	private bool InferForMethodGroupConversion()
	{
		for (int i = 0; i < pMethodArguments.carg; i++)
		{
			CType cType = pMethodFormalParameterTypes.Item(i);
			CType cType2 = pMethodArguments.types.Item(i);
			if (cType.IsParameterModifierType())
			{
				cType = cType.AsParameterModifierType().GetParameterType();
			}
			if (cType2.IsParameterModifierType())
			{
				cType2 = cType2.AsParameterModifierType().GetParameterType();
			}
			LowerBoundInference(cType2, cType);
		}
		bool result = true;
		for (int j = 0; j < pMethodTypeParameters.size; j++)
		{
			if (!HasBound(j) || !Fix(j))
			{
				result = false;
			}
		}
		return result;
	}

	private SymbolLoader GetSymbolLoader()
	{
		return symbolLoader;
	}

	private TypeManager GetTypeManager()
	{
		return GetSymbolLoader().GetTypeManager();
	}

	private BSYMMGR GetGlobalSymbols()
	{
		return GetSymbolLoader().getBSymmgr();
	}

	public static bool CanObjectOfExtensionBeInferred(ExpressionBinder binder, SymbolLoader symbolLoader, MethodSymbol pMethod, TypeArray pClassTypeArguments, TypeArray pMethodFormalParameterTypes, ArgInfos pMethodArguments)
	{
		if (pMethodFormalParameterTypes.size < 1 || pMethod.InferenceMustFail())
		{
			return false;
		}
		if (pMethodArguments.carg < 1)
		{
			return false;
		}
		MethodTypeInferrer methodTypeInferrer = new MethodTypeInferrer(binder, symbolLoader, pMethodFormalParameterTypes, pMethodArguments, pMethod.typeVars, pClassTypeArguments);
		return methodTypeInferrer.CanInferExtensionObject();
	}

	private bool CanInferExtensionObject()
	{
		CType cType = pMethodFormalParameterTypes.Item(0);
		CType cType2 = pMethodArguments.types.Item(0);
		if (cType.IsParameterModifierType())
		{
			cType = cType.AsParameterModifierType().GetParameterType();
		}
		if (cType2.IsParameterModifierType())
		{
			cType2 = cType2.AsParameterModifierType().GetParameterType();
		}
		if (!IsReallyAType(cType2))
		{
			return false;
		}
		LowerBoundInference(cType2, cType);
		for (int i = 0; i < pMethodTypeParameters.size; i++)
		{
			TypeParameterType typeFind = pMethodTypeParameters.ItemAsTypeParameterType(i);
			if (TypeManager.TypeContainsType(cType, typeFind) && (!HasBound(i) || !Fix(i)))
			{
				return false;
			}
		}
		return true;
	}
}
