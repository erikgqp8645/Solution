using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder;

internal class ExpressionTreeCallRewriter : ExprVisitorBase
{
	private class ExpressionEXPR : EXPR
	{
		public Expression Expression;

		public ExpressionEXPR(Expression e)
		{
			Expression = e;
		}
	}

	private Dictionary<EXPRCALL, Expression> DictionaryOfParameters;

	private IEnumerable<Expression> ListOfParameters;

	private TypeManager m_typeManager;

	private int currentParameterIndex;

	protected ExpressionTreeCallRewriter(TypeManager typeManager, IEnumerable<Expression> listOfParameters)
	{
		m_typeManager = typeManager;
		DictionaryOfParameters = new Dictionary<EXPRCALL, Expression>();
		ListOfParameters = listOfParameters;
	}

	public static Expression Rewrite(TypeManager typeManager, EXPR pExpr, IEnumerable<Expression> listOfParameters)
	{
		ExpressionTreeCallRewriter expressionTreeCallRewriter = new ExpressionTreeCallRewriter(typeManager, listOfParameters);
		expressionTreeCallRewriter.Visit(pExpr.asBIN().GetOptionalLeftChild());
		EXPRCALL pExpr2 = pExpr.asBIN().GetOptionalRightChild().asCALL();
		ExpressionEXPR expressionEXPR = expressionTreeCallRewriter.Visit(pExpr2) as ExpressionEXPR;
		return expressionEXPR.Expression;
	}

	protected override EXPR VisitSAVE(EXPRBINOP pExpr)
	{
		EXPRCALL eXPRCALL = pExpr.GetOptionalLeftChild().asCALL();
		EXPRTYPEOF eXPRTYPEOF = eXPRCALL.GetOptionalArguments().asLIST().GetOptionalElement()
			.asTYPEOF();
		Expression value = ListOfParameters.ElementAt(currentParameterIndex++);
		DictionaryOfParameters.Add(eXPRCALL, value);
		return null;
	}

	protected override EXPR VisitCAST(EXPRCAST pExpr)
	{
		return base.VisitCAST(pExpr);
	}

	protected override EXPR VisitCALL(EXPRCALL pExpr)
	{
		if (pExpr.PredefinedMethod != 0)
		{
			switch (pExpr.PredefinedMethod)
			{
			case PREDEFMETH.PM_EXPRESSION_LAMBDA:
				return GenerateLambda(pExpr);
			case PREDEFMETH.PM_EXPRESSION_CALL:
				return GenerateCall(pExpr);
			case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX:
			case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2:
				return GenerateArrayIndex(pExpr);
			case PREDEFMETH.PM_EXPRESSION_CONVERT:
			case PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED:
			case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED:
				return GenerateConvert(pExpr);
			case PREDEFMETH.PM_EXPRESSION_PROPERTY:
				return GenerateProperty(pExpr);
			case PREDEFMETH.PM_EXPRESSION_FIELD:
				return GenerateField(pExpr);
			case PREDEFMETH.PM_EXPRESSION_INVOKE:
				return GenerateInvoke(pExpr);
			case PREDEFMETH.PM_EXPRESSION_NEW:
				return GenerateNew(pExpr);
			case PREDEFMETH.PM_EXPRESSION_ADD:
			case PREDEFMETH.PM_EXPRESSION_ADDCHECKED:
			case PREDEFMETH.PM_EXPRESSION_AND:
			case PREDEFMETH.PM_EXPRESSION_ANDALSO:
			case PREDEFMETH.PM_EXPRESSION_DIVIDE:
			case PREDEFMETH.PM_EXPRESSION_EQUAL:
			case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR:
			case PREDEFMETH.PM_EXPRESSION_GREATERTHAN:
			case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL:
			case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT:
			case PREDEFMETH.PM_EXPRESSION_LESSTHAN:
			case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL:
			case PREDEFMETH.PM_EXPRESSION_MODULO:
			case PREDEFMETH.PM_EXPRESSION_MULTIPLY:
			case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED:
			case PREDEFMETH.PM_EXPRESSION_NOTEQUAL:
			case PREDEFMETH.PM_EXPRESSION_OR:
			case PREDEFMETH.PM_EXPRESSION_ORELSE:
			case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT:
			case PREDEFMETH.PM_EXPRESSION_SUBTRACT:
			case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED:
				return GenerateBinaryOperator(pExpr);
			case PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED:
				return GenerateUserDefinedBinaryOperator(pExpr);
			case PREDEFMETH.PM_EXPRESSION_NEGATE:
			case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED:
			case PREDEFMETH.PM_EXPRESSION_NOT:
				return GenerateUnaryOperator(pExpr);
			case PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED:
			case PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED:
				return GenerateUserDefinedUnaryOperator(pExpr);
			case PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE:
				return GenerateConstantType(pExpr);
			case PREDEFMETH.PM_EXPRESSION_ASSIGN:
				return GenerateAssignment(pExpr);
			default:
				throw Error.InternalCompilerError();
			}
		}
		return pExpr;
	}

	private ExpressionEXPR GenerateLambda(EXPRCALL pExpr)
	{
		ExpressionEXPR expressionEXPR = Visit(pExpr.GetOptionalArguments().asLIST().GetOptionalElement()) as ExpressionEXPR;
		Expression expression = expressionEXPR.Expression;
		return new ExpressionEXPR(expression);
	}

	private ExpressionEXPR GenerateCall(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.GetOptionalArguments().asLIST();
		EXPRMETHODINFO methinfo;
		EXPRARRINIT arrinit;
		if (eXPRLIST.GetOptionalNextListNode().isLIST())
		{
			methinfo = eXPRLIST.GetOptionalNextListNode().asLIST().GetOptionalElement()
				.asMETHODINFO();
			arrinit = eXPRLIST.GetOptionalNextListNode().asLIST().GetOptionalNextListNode()
				.asARRINIT();
		}
		else
		{
			methinfo = eXPRLIST.GetOptionalNextListNode().asMETHODINFO();
			arrinit = null;
		}
		Expression instance = null;
		MethodInfo methodInfoFromExpr = GetMethodInfoFromExpr(methinfo);
		Expression[] argumentsFromArrayInit = GetArgumentsFromArrayInit(arrinit);
		if (methodInfoFromExpr == null)
		{
			throw Error.InternalCompilerError();
		}
		if (!methodInfoFromExpr.IsStatic)
		{
			instance = GetExpression(pExpr.GetOptionalArguments().asLIST().GetOptionalElement());
		}
		return new ExpressionEXPR(Expression.Call(instance, methodInfoFromExpr, argumentsFromArrayInit));
	}

	private ExpressionEXPR GenerateArrayIndex(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.GetOptionalArguments().asLIST();
		Expression expression = GetExpression(eXPRLIST.GetOptionalElement());
		Expression[] indexes = ((pExpr.PredefinedMethod != PREDEFMETH.PM_EXPRESSION_ARRAYINDEX) ? GetArgumentsFromArrayInit(eXPRLIST.GetOptionalNextListNode().asARRINIT()) : new Expression[1] { GetExpression(eXPRLIST.GetOptionalNextListNode()) });
		return new ExpressionEXPR(Expression.ArrayAccess(expression, indexes));
	}

	private ExpressionEXPR GenerateConvert(EXPRCALL pExpr)
	{
		PREDEFMETH predefinedMethod = pExpr.PredefinedMethod;
		Expression expression;
		Type associatedSystemType;
		if (predefinedMethod == PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED || predefinedMethod == PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED)
		{
			EXPRLIST eXPRLIST = pExpr.asCALL().GetOptionalArguments().asLIST();
			EXPRLIST eXPRLIST2 = eXPRLIST.GetOptionalNextListNode().asLIST();
			expression = GetExpression(eXPRLIST.GetOptionalElement());
			associatedSystemType = eXPRLIST2.GetOptionalElement().asTYPEOF().SourceType.type.AssociatedSystemType;
			if (expression.Type.MakeByRefType() == associatedSystemType)
			{
				return new ExpressionEXPR(expression);
			}
			MethodInfo methodInfoFromExpr = GetMethodInfoFromExpr(eXPRLIST2.GetOptionalNextListNode().asMETHODINFO());
			if (predefinedMethod == PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED)
			{
				return new ExpressionEXPR(Expression.Convert(expression, associatedSystemType, methodInfoFromExpr));
			}
			return new ExpressionEXPR(Expression.ConvertChecked(expression, associatedSystemType, methodInfoFromExpr));
		}
		EXPRLIST eXPRLIST3 = pExpr.asCALL().GetOptionalArguments().asLIST();
		expression = GetExpression(eXPRLIST3.GetOptionalElement());
		associatedSystemType = eXPRLIST3.GetOptionalNextListNode().asTYPEOF().SourceType.type.AssociatedSystemType;
		if (expression.Type.MakeByRefType() == associatedSystemType)
		{
			return new ExpressionEXPR(expression);
		}
		if ((pExpr.flags & EXPRFLAG.EXF_USERCALLABLE) != 0)
		{
			return new ExpressionEXPR(Expression.Unbox(expression, associatedSystemType));
		}
		if (predefinedMethod == PREDEFMETH.PM_EXPRESSION_CONVERT)
		{
			return new ExpressionEXPR(Expression.Convert(expression, associatedSystemType));
		}
		return new ExpressionEXPR(Expression.ConvertChecked(expression, associatedSystemType));
	}

	private ExpressionEXPR GenerateProperty(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.asCALL().GetOptionalArguments().asLIST();
		EXPR optionalElement = eXPRLIST.GetOptionalElement();
		EXPRPropertyInfo propinfo = (eXPRLIST.GetOptionalNextListNode().isLIST() ? eXPRLIST.GetOptionalNextListNode().asLIST().GetOptionalElement()
			.asPropertyInfo() : eXPRLIST.GetOptionalNextListNode().asPropertyInfo());
		EXPRARRINIT eXPRARRINIT = (eXPRLIST.GetOptionalNextListNode().isLIST() ? eXPRLIST.GetOptionalNextListNode().asLIST().GetOptionalNextListNode()
			.asARRINIT() : null);
		PropertyInfo propertyInfoFromExpr = GetPropertyInfoFromExpr(propinfo);
		if (propertyInfoFromExpr == null)
		{
			throw Error.InternalCompilerError();
		}
		if (eXPRARRINIT == null)
		{
			return new ExpressionEXPR(Expression.Property(GetExpression(optionalElement), propertyInfoFromExpr));
		}
		return new ExpressionEXPR(Expression.Property(GetExpression(optionalElement), propertyInfoFromExpr, GetArgumentsFromArrayInit(eXPRARRINIT)));
	}

	private ExpressionEXPR GenerateField(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.asCALL().GetOptionalArguments().asLIST();
		Type type = eXPRLIST.GetOptionalNextListNode().asFIELDINFO().FieldType()
			.AssociatedSystemType;
		FieldInfo fieldInfo = eXPRLIST.GetOptionalNextListNode().asFIELDINFO().Field()
			.AssociatedFieldInfo;
		if (!type.IsGenericType && !type.IsNested)
		{
			type = fieldInfo.DeclaringType;
		}
		if (type.IsGenericType)
		{
			fieldInfo = type.GetField(fieldInfo.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
		return new ExpressionEXPR(Expression.Field(GetExpression(eXPRLIST.GetOptionalElement()), fieldInfo));
	}

	private ExpressionEXPR GenerateInvoke(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.asCALL().GetOptionalArguments().asLIST();
		return new ExpressionEXPR(Expression.Invoke(GetExpression(eXPRLIST.GetOptionalElement()), GetArgumentsFromArrayInit(eXPRLIST.GetOptionalNextListNode().asARRINIT())));
	}

	private ExpressionEXPR GenerateNew(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.asCALL().GetOptionalArguments().asLIST();
		ConstructorInfo constructorInfoFromExpr = GetConstructorInfoFromExpr(eXPRLIST.GetOptionalElement().asMETHODINFO());
		Expression[] argumentsFromArrayInit = GetArgumentsFromArrayInit(eXPRLIST.GetOptionalNextListNode().asARRINIT());
		return new ExpressionEXPR(Expression.New(constructorInfoFromExpr, argumentsFromArrayInit));
	}

	private ExpressionEXPR GenerateConstantType(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.GetOptionalArguments().asLIST();
		return new ExpressionEXPR(Expression.Constant(GetObject(eXPRLIST.GetOptionalElement()), eXPRLIST.GetOptionalNextListNode().asTYPEOF().SourceType.type.AssociatedSystemType));
	}

	private ExpressionEXPR GenerateAssignment(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.GetOptionalArguments().asLIST();
		return new ExpressionEXPR(Expression.Assign(GetExpression(eXPRLIST.GetOptionalElement()), GetExpression(eXPRLIST.GetOptionalNextListNode())));
	}

	private ExpressionEXPR GenerateBinaryOperator(EXPRCALL pExpr)
	{
		Expression expression = GetExpression(pExpr.GetOptionalArguments().asLIST().GetOptionalElement());
		Expression expression2 = GetExpression(pExpr.GetOptionalArguments().asLIST().GetOptionalNextListNode());
		return pExpr.PredefinedMethod switch
		{
			PREDEFMETH.PM_EXPRESSION_ADD => new ExpressionEXPR(Expression.Add(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_AND => new ExpressionEXPR(Expression.And(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_DIVIDE => new ExpressionEXPR(Expression.Divide(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_EQUAL => new ExpressionEXPR(Expression.Equal(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR => new ExpressionEXPR(Expression.ExclusiveOr(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_GREATERTHAN => new ExpressionEXPR(Expression.GreaterThan(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL => new ExpressionEXPR(Expression.GreaterThanOrEqual(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_LEFTSHIFT => new ExpressionEXPR(Expression.LeftShift(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_LESSTHAN => new ExpressionEXPR(Expression.LessThan(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL => new ExpressionEXPR(Expression.LessThanOrEqual(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_MODULO => new ExpressionEXPR(Expression.Modulo(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_MULTIPLY => new ExpressionEXPR(Expression.Multiply(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_NOTEQUAL => new ExpressionEXPR(Expression.NotEqual(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_OR => new ExpressionEXPR(Expression.Or(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT => new ExpressionEXPR(Expression.RightShift(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_SUBTRACT => new ExpressionEXPR(Expression.Subtract(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_ORELSE => new ExpressionEXPR(Expression.OrElse(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_ANDALSO => new ExpressionEXPR(Expression.AndAlso(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_ADDCHECKED => new ExpressionEXPR(Expression.AddChecked(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED => new ExpressionEXPR(Expression.MultiplyChecked(expression, expression2)), 
			PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED => new ExpressionEXPR(Expression.SubtractChecked(expression, expression2)), 
			_ => throw Error.InternalCompilerError(), 
		};
	}

	private ExpressionEXPR GenerateUserDefinedBinaryOperator(EXPRCALL pExpr)
	{
		EXPRLIST eXPRLIST = pExpr.GetOptionalArguments().asLIST();
		Expression expression = GetExpression(eXPRLIST.GetOptionalElement());
		Expression expression2 = GetExpression(eXPRLIST.GetOptionalNextListNode().asLIST().GetOptionalElement());
		eXPRLIST = eXPRLIST.GetOptionalNextListNode().asLIST();
		bool liftToNull = false;
		MethodInfo methodInfoFromExpr;
		if (eXPRLIST.GetOptionalNextListNode().isLIST())
		{
			EXPRCONSTANT eXPRCONSTANT = eXPRLIST.GetOptionalNextListNode().asLIST().GetOptionalElement()
				.asCONSTANT();
			liftToNull = eXPRCONSTANT.getVal().iVal == 1;
			methodInfoFromExpr = GetMethodInfoFromExpr(eXPRLIST.GetOptionalNextListNode().asLIST().GetOptionalNextListNode()
				.asMETHODINFO());
		}
		else
		{
			methodInfoFromExpr = GetMethodInfoFromExpr(eXPRLIST.GetOptionalNextListNode().asMETHODINFO());
		}
		return pExpr.PredefinedMethod switch
		{
			PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED => new ExpressionEXPR(Expression.Add(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED => new ExpressionEXPR(Expression.And(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED => new ExpressionEXPR(Expression.Divide(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED => new ExpressionEXPR(Expression.Equal(expression, expression2, liftToNull, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED => new ExpressionEXPR(Expression.ExclusiveOr(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED => new ExpressionEXPR(Expression.GreaterThan(expression, expression2, liftToNull, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED => new ExpressionEXPR(Expression.GreaterThanOrEqual(expression, expression2, liftToNull, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED => new ExpressionEXPR(Expression.LeftShift(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED => new ExpressionEXPR(Expression.LessThan(expression, expression2, liftToNull, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED => new ExpressionEXPR(Expression.LessThanOrEqual(expression, expression2, liftToNull, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED => new ExpressionEXPR(Expression.Modulo(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED => new ExpressionEXPR(Expression.Multiply(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED => new ExpressionEXPR(Expression.NotEqual(expression, expression2, liftToNull, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED => new ExpressionEXPR(Expression.Or(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED => new ExpressionEXPR(Expression.RightShift(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED => new ExpressionEXPR(Expression.Subtract(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED => new ExpressionEXPR(Expression.OrElse(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED => new ExpressionEXPR(Expression.AndAlso(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED => new ExpressionEXPR(Expression.AddChecked(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED => new ExpressionEXPR(Expression.MultiplyChecked(expression, expression2, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED => new ExpressionEXPR(Expression.SubtractChecked(expression, expression2, methodInfoFromExpr)), 
			_ => throw Error.InternalCompilerError(), 
		};
	}

	private ExpressionEXPR GenerateUnaryOperator(EXPRCALL pExpr)
	{
		PREDEFMETH predefinedMethod = pExpr.PredefinedMethod;
		Expression expression = GetExpression(pExpr.GetOptionalArguments());
		return predefinedMethod switch
		{
			PREDEFMETH.PM_EXPRESSION_NOT => new ExpressionEXPR(Expression.Not(expression)), 
			PREDEFMETH.PM_EXPRESSION_NEGATE => new ExpressionEXPR(Expression.Negate(expression)), 
			PREDEFMETH.PM_EXPRESSION_NEGATECHECKED => new ExpressionEXPR(Expression.NegateChecked(expression)), 
			_ => throw Error.InternalCompilerError(), 
		};
	}

	private ExpressionEXPR GenerateUserDefinedUnaryOperator(EXPRCALL pExpr)
	{
		PREDEFMETH predefinedMethod = pExpr.PredefinedMethod;
		EXPRLIST eXPRLIST = pExpr.GetOptionalArguments().asLIST();
		Expression expression = GetExpression(eXPRLIST.GetOptionalElement());
		MethodInfo methodInfoFromExpr = GetMethodInfoFromExpr(eXPRLIST.GetOptionalNextListNode().asMETHODINFO());
		return predefinedMethod switch
		{
			PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED => new ExpressionEXPR(Expression.Not(expression, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED => new ExpressionEXPR(Expression.Negate(expression, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED => new ExpressionEXPR(Expression.UnaryPlus(expression, methodInfoFromExpr)), 
			PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED => new ExpressionEXPR(Expression.NegateChecked(expression, methodInfoFromExpr)), 
			_ => throw Error.InternalCompilerError(), 
		};
	}

	private Expression GetExpression(EXPR pExpr)
	{
		if (pExpr.isWRAP())
		{
			return DictionaryOfParameters[pExpr.asWRAP().GetOptionalExpression().asCALL()];
		}
		if (pExpr.isCONSTANT())
		{
			return null;
		}
		EXPRCALL eXPRCALL = pExpr.asCALL();
		switch (eXPRCALL.PredefinedMethod)
		{
		case PREDEFMETH.PM_EXPRESSION_CALL:
			return GenerateCall(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_CONVERT:
		case PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED:
		case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED:
			return GenerateConvert(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT:
		{
			EXPRLIST eXPRLIST = eXPRCALL.GetOptionalArguments().asLIST();
			return Expression.NewArrayInit(eXPRLIST.GetOptionalElement().asTYPEOF().SourceType.type.AssociatedSystemType, GetArgumentsFromArrayInit(eXPRLIST.GetOptionalNextListNode().asARRINIT()));
		}
		case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX:
		case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2:
			return GenerateArrayIndex(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_NEW:
			return GenerateNew(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_PROPERTY:
			return GenerateProperty(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_FIELD:
			return GenerateField(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE:
			return GenerateConstantType(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_ASSIGN:
			return GenerateAssignment(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_ADD:
		case PREDEFMETH.PM_EXPRESSION_ADDCHECKED:
		case PREDEFMETH.PM_EXPRESSION_AND:
		case PREDEFMETH.PM_EXPRESSION_ANDALSO:
		case PREDEFMETH.PM_EXPRESSION_DIVIDE:
		case PREDEFMETH.PM_EXPRESSION_EQUAL:
		case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR:
		case PREDEFMETH.PM_EXPRESSION_GREATERTHAN:
		case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL:
		case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT:
		case PREDEFMETH.PM_EXPRESSION_LESSTHAN:
		case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL:
		case PREDEFMETH.PM_EXPRESSION_MODULO:
		case PREDEFMETH.PM_EXPRESSION_MULTIPLY:
		case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED:
		case PREDEFMETH.PM_EXPRESSION_NOTEQUAL:
		case PREDEFMETH.PM_EXPRESSION_OR:
		case PREDEFMETH.PM_EXPRESSION_ORELSE:
		case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT:
		case PREDEFMETH.PM_EXPRESSION_SUBTRACT:
		case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED:
			return GenerateBinaryOperator(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED:
			return GenerateUserDefinedBinaryOperator(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_NEGATE:
		case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED:
		case PREDEFMETH.PM_EXPRESSION_NOT:
			return GenerateUnaryOperator(eXPRCALL).Expression;
		case PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED:
		case PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED:
			return GenerateUserDefinedUnaryOperator(eXPRCALL).Expression;
		default:
			throw Error.InternalCompilerError();
		}
	}

	private object GetObject(EXPR pExpr)
	{
		if (pExpr.isCAST())
		{
			return GetObject(pExpr.asCAST().GetArgument());
		}
		if (pExpr.isTYPEOF())
		{
			return pExpr.asTYPEOF().SourceType.type.AssociatedSystemType;
		}
		if (pExpr.isMETHODINFO())
		{
			return GetMethodInfoFromExpr(pExpr.asMETHODINFO());
		}
		if (pExpr.isCONSTANT())
		{
			CONSTVAL val = pExpr.asCONSTANT().Val;
			CType cType = pExpr.type;
			if (pExpr.type.IsNullType())
			{
				return null;
			}
			if (pExpr.type.isEnumType())
			{
				cType = cType.getAggregate().GetUnderlyingType();
			}
			object obj = Type.GetTypeCode(cType.AssociatedSystemType) switch
			{
				TypeCode.Boolean => val.boolVal, 
				TypeCode.SByte => val.sbyteVal, 
				TypeCode.Byte => val.byteVal, 
				TypeCode.Int16 => val.shortVal, 
				TypeCode.UInt16 => val.ushortVal, 
				TypeCode.Int32 => val.iVal, 
				TypeCode.UInt32 => val.uiVal, 
				TypeCode.Int64 => val.longVal, 
				TypeCode.UInt64 => val.ulongVal, 
				TypeCode.Single => val.floatVal, 
				TypeCode.Double => val.doubleVal, 
				TypeCode.Decimal => val.decVal, 
				TypeCode.Char => val.cVal, 
				TypeCode.String => val.strVal, 
				_ => val.objectVal, 
			};
			if (pExpr.type.isEnumType())
			{
				obj = Enum.ToObject(pExpr.type.AssociatedSystemType, obj);
			}
			return obj;
		}
		if (pExpr.isZEROINIT())
		{
			if (pExpr.asZEROINIT().OptionalArgument != null)
			{
				return GetObject(pExpr.asZEROINIT().OptionalArgument);
			}
			return Activator.CreateInstance(pExpr.type.AssociatedSystemType);
		}
		throw Error.InternalCompilerError();
	}

	private Expression[] GetArgumentsFromArrayInit(EXPRARRINIT arrinit)
	{
		List<Expression> list = new List<Expression>();
		if (arrinit != null)
		{
			EXPR eXPR = arrinit.GetOptionalArguments();
			EXPR eXPR2 = eXPR;
			while (eXPR != null)
			{
				if (eXPR.isLIST())
				{
					eXPR2 = eXPR.asLIST().GetOptionalElement();
					eXPR = eXPR.asLIST().GetOptionalNextListNode();
				}
				else
				{
					eXPR2 = eXPR;
					eXPR = null;
				}
				list.Add(GetExpression(eXPR2));
			}
		}
		return list.ToArray();
	}

	private MethodInfo GetMethodInfoFromExpr(EXPRMETHODINFO methinfo)
	{
		AggregateType ats = methinfo.Method.Ats;
		MethodSymbol methodSymbol = methinfo.Method.Meth();
		TypeArray typeArray = m_typeManager.SubstTypeArray(methodSymbol.Params, ats, methodSymbol.typeVars);
		CType cType = m_typeManager.SubstType(methodSymbol.RetType, ats, methodSymbol.typeVars);
		Type type = ats.AssociatedSystemType;
		MethodInfo methodInfo = methodSymbol.AssociatedMemberInfo as MethodInfo;
		if (!type.IsGenericType && !type.IsNested)
		{
			type = methodInfo.DeclaringType;
		}
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo2 in methods)
		{
			if (methodInfo2.MetadataToken != methodInfo.MetadataToken || methodInfo2.Module != methodInfo.Module)
			{
				continue;
			}
			bool flag = true;
			ParameterInfo[] parameters = methodInfo2.GetParameters();
			for (int j = 0; j < typeArray.size; j++)
			{
				if (!TypesAreEqual(parameters[j].ParameterType, typeArray.Item(j).AssociatedSystemType))
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			if (methodInfo2.IsGenericMethod)
			{
				int num = ((methinfo.Method.TypeArgs != null) ? methinfo.Method.TypeArgs.size : 0);
				Type[] array = new Type[num];
				if (num > 0)
				{
					for (int k = 0; k < methinfo.Method.TypeArgs.size; k++)
					{
						array[k] = methinfo.Method.TypeArgs[k].AssociatedSystemType;
					}
				}
				return methodInfo2.MakeGenericMethod(array);
			}
			return methodInfo2;
		}
		throw Error.InternalCompilerError();
	}

	private ConstructorInfo GetConstructorInfoFromExpr(EXPRMETHODINFO methinfo)
	{
		AggregateType ats = methinfo.Method.Ats;
		MethodSymbol methodSymbol = methinfo.Method.Meth();
		TypeArray typeArray = m_typeManager.SubstTypeArray(methodSymbol.Params, ats);
		Type type = ats.AssociatedSystemType;
		ConstructorInfo constructorInfo = (ConstructorInfo)methodSymbol.AssociatedMemberInfo;
		if (!type.IsGenericType && !type.IsNested)
		{
			type = constructorInfo.DeclaringType;
		}
		ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (ConstructorInfo constructorInfo2 in constructors)
		{
			if (constructorInfo2.MetadataToken != constructorInfo.MetadataToken || constructorInfo2.Module != constructorInfo.Module)
			{
				continue;
			}
			bool flag = true;
			ParameterInfo[] parameters = constructorInfo2.GetParameters();
			for (int j = 0; j < typeArray.size; j++)
			{
				if (!TypesAreEqual(parameters[j].ParameterType, typeArray.Item(j).AssociatedSystemType))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return constructorInfo2;
			}
		}
		throw Error.InternalCompilerError();
	}

	private PropertyInfo GetPropertyInfoFromExpr(EXPRPropertyInfo propinfo)
	{
		AggregateType ats = propinfo.Property.Ats;
		PropertySymbol propertySymbol = propinfo.Property.Prop();
		TypeArray typeArray = m_typeManager.SubstTypeArray(propertySymbol.Params, ats, null);
		CType cType = m_typeManager.SubstType(propertySymbol.RetType, ats, null);
		Type type = ats.AssociatedSystemType;
		PropertyInfo associatedPropertyInfo = propertySymbol.AssociatedPropertyInfo;
		if (!type.IsGenericType && !type.IsNested)
		{
			type = associatedPropertyInfo.DeclaringType;
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.MetadataToken != associatedPropertyInfo.MetadataToken || propertyInfo.Module != associatedPropertyInfo.Module)
			{
				continue;
			}
			bool flag = true;
			ParameterInfo[] array = ((propertyInfo.GetSetMethod(nonPublic: true) != null) ? propertyInfo.GetSetMethod(nonPublic: true).GetParameters() : propertyInfo.GetGetMethod(nonPublic: true).GetParameters());
			for (int j = 0; j < typeArray.size; j++)
			{
				if (!TypesAreEqual(array[j].ParameterType, typeArray.Item(j).AssociatedSystemType))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return propertyInfo;
			}
		}
		throw Error.InternalCompilerError();
	}

	private bool TypesAreEqual(Type t1, Type t2)
	{
		if (t1 == t2)
		{
			return true;
		}
		return t1.IsEquivalentTo(t2);
	}
}
