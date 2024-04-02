using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class PredefinedMembers
{
	private SymbolLoader m_loader;

	internal SymbolTable RuntimeBinderSymbolTable;

	private MethodSymbol[] m_methods = new MethodSymbol[109];

	private PropertySymbol[] m_properties = new PropertySymbol[3];

	private static int[] g_DelegateCtorSignature1 = new int[4] { 139, 2, 15, 13 };

	private static int[] g_DelegateCtorSignature2 = new int[4] { 139, 2, 15, 14 };

	private static PredefinedPropertyInfo[] g_predefinedProperties = new PredefinedPropertyInfo[3]
	{
		new PredefinedPropertyInfo(PREDEFPROP.PP_FIRST, MethodRequiredEnum.Optional, PredefinedName.PN_COUNT, PREDEFMETH.PM_COUNT, PREDEFMETH.PM_COUNT),
		new PredefinedPropertyInfo(PREDEFPROP.PP_ARRAY_LENGTH, MethodRequiredEnum.Optional, PredefinedName.PN_LENGTH, PREDEFMETH.PM_ARRAY_GETLENGTH, PREDEFMETH.PM_COUNT),
		new PredefinedPropertyInfo(PREDEFPROP.PP_G_OPTIONAL_VALUE, MethodRequiredEnum.Optional, PredefinedName.PN_CAP_VALUE, PREDEFMETH.PM_G_OPTIONAL_GETVALUE, PREDEFMETH.PM_COUNT)
	};

	private static PredefinedMethodInfo[] g_predefinedMethods = new PredefinedMethodInfo[109]
	{
		new PredefinedMethodInfo(PREDEFMETH.PM_FIRST, MethodRequiredEnum.Optional, PredefinedType.PT_COUNT, PredefinedName.PN_COUNT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[2] { 139, 0 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_ARRAY_GETLENGTH, MethodRequiredEnum.Optional, PredefinedType.PT_ARRAY, PredefinedName.PN_GETLENGTH, MethodCallingConventionEnum.Instance, ACCESS.ACC_PUBLIC, 0, new int[2] { 2, 0 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPDECREMENT, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPDECREMENT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 6, 1, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPDIVISION, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPDIVISION, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 6, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPEQUALITY, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPEQUALITY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPGREATERTHAN, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPGREATERTHAN, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPGREATERTHANOREQUAL, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPGREATERTHANOREQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPINCREMENT, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPINCREMENT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 6, 1, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPINEQUALITY, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPINEQUALITY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPLESSTHAN, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPLESSTHAN, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPLESSTHANOREQUAL, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPLESSTHANOREQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPMINUS, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPMINUS, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 6, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPMODULUS, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPMODULUS, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 6, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPMULTIPLY, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPMULTIPLY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 6, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPPLUS, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPPLUS, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 6, 2, 6, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPUNARYMINUS, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPUNARYMINUS, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 6, 1, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DECIMAL_OPUNARYPLUS, MethodRequiredEnum.Optional, PredefinedType.PT_DECIMAL, PredefinedName.PN_OPUNARYPLUS, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 6, 1, 6 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DELEGATE_COMBINE, MethodRequiredEnum.Optional, PredefinedType.PT_DELEGATE, PredefinedName.PN_COMBINE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 17, 2, 17, 17 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DELEGATE_OPEQUALITY, MethodRequiredEnum.Optional, PredefinedType.PT_DELEGATE, PredefinedName.PN_OPEQUALITY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 17, 17 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DELEGATE_OPINEQUALITY, MethodRequiredEnum.Optional, PredefinedType.PT_DELEGATE, PredefinedName.PN_OPINEQUALITY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 17, 17 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DELEGATE_REMOVE, MethodRequiredEnum.Optional, PredefinedType.PT_DELEGATE, PredefinedName.PN_REMOVE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 17, 2, 17, 17 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ADD, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ADD, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ADD, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ADDCHECKED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ADDCHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ADDCHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_AND, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_AND, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_AND, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ANDALSO, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ANDALSO, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ANDALSO, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ARRAYINDEX, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ARRAYINDEX, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ARRAYINDEX, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 111, 2, 103, 142, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ASSIGN, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ASSIGN, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_CONDITION, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_CONDITION, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 107, 3, 103, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_CONSTANT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 108, 2, 15, 21 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_CONVERT, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_CONVERT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 106, 2, 103, 21 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_CONVERT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 106, 3, 103, 21, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_CONVERTCHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 106, 2, 103, 21 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_CONVERTCHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 106, 3, 103, 21, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_DIVIDE, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_DIVIDE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_DIVIDE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_EQUAL, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_EQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_EQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[6] { 105, 4, 103, 103, 8, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_EXCLUSIVEOR, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_EXCLUSIVEOR, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_FIELD, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_CAP_FIELD, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 110, 2, 103, 122 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_GREATERTHAN, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_GREATERTHAN, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_GREATERTHAN, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[6] { 105, 4, 103, 103, 8, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_GREATERTHANOREQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_GREATERTHANOREQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[6] { 105, 4, 103, 103, 8, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_LAMBDA, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_LAMBDA, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 1, new int[7] { 102, 141, 0, 2, 103, 142, 109 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_LEFTSHIFT, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_LEFTSHIFT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_LEFTSHIFT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_LESSTHAN, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_LESSTHAN, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_LESSTHAN, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[6] { 105, 4, 103, 103, 8, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_LESSTHANOREQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_LESSTHANOREQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[6] { 105, 4, 103, 103, 8, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_MODULO, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_MODULO, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_MODULO, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_MULTIPLY, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_MULTIPLY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_MULTIPLY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_MULTIPLYCHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_MULTIPLYCHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NOTEQUAL, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NOTEQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NOTEQUAL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[6] { 105, 4, 103, 103, 8, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_OR, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_OR, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_OR, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ORELSE, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ORELSE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ORELSE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_PARAMETER, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_PARAMETER, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 109, 2, 21, 16 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_RIGHTSHIFT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_RIGHTSHIFT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_SUBTRACT, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_SUBTRACT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_SUBTRACT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_SUBTRACTCHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 105, 2, 103, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_SUBTRACTCHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 105, 3, 103, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_PLUS, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 106, 2, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NEGATE, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NEGATE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 106, 1, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NEGATE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 106, 2, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NEGATECHECKED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NEGATECHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 106, 1, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NEGATECHECKED, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 106, 2, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_CALL, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_CALL, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[6] { 111, 3, 103, 123, 142, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NEW, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NEW, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 112, 2, 124, 142, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NEW_MEMBERS, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NEW, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[7] { 112, 3, 124, 76, 103, 142, 127 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NEW_TYPE, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NEW, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 112, 1, 21 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_QUOTE, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_QUOTE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 106, 1, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_ARRAYLENGTH, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_ARRAYLENGTH, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 106, 1, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NOT, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NOT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 106, 1, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NOT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 106, 2, 103, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_NEWARRAYINIT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 117, 2, 21, 142, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_PROPERTY, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_EXPRESSION_PROPERTY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 110, 2, 103, 125 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_EXPRESSION_INVOKE, MethodRequiredEnum.Optional, PredefinedType.PT_EXPRESSION, PredefinedName.PN_INVOKE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 121, 2, 103, 142, 103 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_DELEGATE_CREATEDELEGATE_TYPE_OBJ_METHINFO, MethodRequiredEnum.Optional, PredefinedType.PT_DELEGATE, PredefinedName.PN_CREATEDELEGATE, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 17, 3, 21, 15, 123 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_G_OPTIONAL_CTOR, MethodRequiredEnum.Optional, PredefinedType.PT_G_OPTIONAL, PredefinedName.PN_CTOR, MethodCallingConventionEnum.Instance, ACCESS.ACC_PUBLIC, 0, new int[4] { 139, 1, 140, 0 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_G_OPTIONAL_GETHASVALUE, MethodRequiredEnum.Optional, PredefinedType.PT_G_OPTIONAL, PredefinedName.PN_GETHASVALUE, MethodCallingConventionEnum.Instance, ACCESS.ACC_PUBLIC, 0, new int[2] { 8, 0 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_G_OPTIONAL_GETVALUE, MethodRequiredEnum.Optional, PredefinedType.PT_G_OPTIONAL, PredefinedName.PN_GETVALUE, MethodCallingConventionEnum.Instance, ACCESS.ACC_PUBLIC, 0, new int[3] { 140, 0, 0 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_G_OPTIONAL_GET_VALUE_OR_DEF, MethodRequiredEnum.Optional, PredefinedType.PT_G_OPTIONAL, PredefinedName.PN_GET_VALUE_OR_DEF, MethodCallingConventionEnum.Instance, ACCESS.ACC_PUBLIC, 0, new int[3] { 140, 0, 0 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_OBJECT_1, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 16, 1, 15 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_OBJECT_2, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 16, 2, 15, 15 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_OBJECT_3, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 16, 3, 15, 15, 15 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_STRING_1, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[3] { 16, 1, 16 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_STRING_2, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 16, 2, 16, 16 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_STRING_3, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[5] { 16, 3, 16, 16, 16 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_STRING_4, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[6] { 16, 4, 16, 16, 16, 16 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_SZ_OBJECT, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 16, 1, 142, 15 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_CONCAT_SZ_STRING, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_CONCAT, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 16, 1, 142, 16 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_GETCHARS, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_GETCHARS, MethodCallingConventionEnum.Instance, ACCESS.ACC_PUBLIC, 0, new int[3] { 7, 1, 2 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_GETLENGTH, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_GETLENGTH, MethodCallingConventionEnum.Instance, ACCESS.ACC_PUBLIC, 0, new int[2] { 2, 0 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_OPEQUALITY, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_OPEQUALITY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 16, 16 }),
		new PredefinedMethodInfo(PREDEFMETH.PM_STRING_OPINEQUALITY, MethodRequiredEnum.Optional, PredefinedType.PT_STRING, PredefinedName.PN_OPINEQUALITY, MethodCallingConventionEnum.Static, ACCESS.ACC_PUBLIC, 0, new int[4] { 8, 2, 16, 16 })
	};

	protected static void RETAILVERIFY(bool f)
	{
	}

	private Name GetMethName(PREDEFMETH method)
	{
		return GetPredefName(GetMethPredefName(method));
	}

	private AggregateSymbol GetMethParent(PREDEFMETH method)
	{
		return GetOptPredefAgg(GetMethPredefType(method));
	}

	private MethodSymbol FindDelegateConstructor(AggregateSymbol delegateType, int[] signature)
	{
		return LoadMethod(delegateType, signature, 0, GetPredefName(PredefinedName.PN_CTOR), ACCESS.ACC_PUBLIC, isStatic: false, isVirtual: false);
	}

	private MethodSymbol FindDelegateConstructor(AggregateSymbol delegateType)
	{
		MethodSymbol methodSymbol = FindDelegateConstructor(delegateType, g_DelegateCtorSignature1);
		if (methodSymbol == null)
		{
			methodSymbol = FindDelegateConstructor(delegateType, g_DelegateCtorSignature2);
		}
		return methodSymbol;
	}

	public MethodSymbol FindDelegateConstructor(AggregateSymbol delegateType, bool fReportErrors)
	{
		MethodSymbol methodSymbol = FindDelegateConstructor(delegateType);
		if (methodSymbol == null && fReportErrors)
		{
			GetErrorContext().Error(ErrorCode.ERR_BadDelegateConstructor, delegateType);
		}
		return methodSymbol;
	}

	private PropertySymbol EnsureProperty(PREDEFPROP property)
	{
		RETAILVERIFY(property > PREDEFPROP.PP_FIRST && property < (PREDEFPROP)109);
		if (m_properties[(int)property] == null)
		{
			m_properties[(int)property] = LoadProperty(property);
		}
		return m_properties[(int)property];
	}

	private PropertySymbol LoadProperty(PREDEFPROP property)
	{
		return LoadProperty(property, GetPropName(property), GetPropGetter(property), GetPropSetter(property));
	}

	private Name GetPropName(PREDEFPROP property)
	{
		return GetPredefName(GetPropPredefName(property));
	}

	private PropertySymbol LoadProperty(PREDEFPROP predefProp, Name propertyName, PREDEFMETH propertyGetter, PREDEFMETH propertySetter)
	{
		MethodSymbol optionalMethod = GetOptionalMethod(propertyGetter);
		MethodSymbol methodSymbol = null;
		if (propertySetter != PREDEFMETH.PM_COUNT)
		{
			methodSymbol = GetOptionalMethod(propertySetter);
		}
		if (optionalMethod == null && methodSymbol == null)
		{
			RuntimeBinderSymbolTable.AddPredefinedPropertyToSymbolTable(GetOptPredefAgg(GetPropPredefType(predefProp)), propertyName);
			optionalMethod = GetOptionalMethod(propertyGetter);
			if (propertySetter != PREDEFMETH.PM_COUNT)
			{
				methodSymbol = GetOptionalMethod(propertySetter);
			}
		}
		methodSymbol?.SetMethKind(MethodKindEnum.PropAccessor);
		PropertySymbol propertySymbol = null;
		if (optionalMethod != null)
		{
			optionalMethod.SetMethKind(MethodKindEnum.PropAccessor);
			propertySymbol = optionalMethod.getProperty();
			if (propertySymbol == null)
			{
				RuntimeBinderSymbolTable.AddPredefinedPropertyToSymbolTable(GetOptPredefAgg(GetPropPredefType(predefProp)), propertyName);
			}
			propertySymbol = optionalMethod.getProperty();
			if (propertySymbol.name != propertyName || (propertySetter != PREDEFMETH.PM_COUNT && (methodSymbol == null || !methodSymbol.isPropertyAccessor() || methodSymbol.getProperty() != propertySymbol)) || propertySymbol.getBogus())
			{
				propertySymbol = null;
			}
		}
		return propertySymbol;
	}

	private SymbolLoader GetSymbolLoader()
	{
		return m_loader;
	}

	private ErrorHandling GetErrorContext()
	{
		return GetSymbolLoader().GetErrorContext();
	}

	private NameManager GetNameManager()
	{
		return GetSymbolLoader().GetNameManager();
	}

	private TypeManager GetTypeManager()
	{
		return GetSymbolLoader().GetTypeManager();
	}

	private BSYMMGR getBSymmgr()
	{
		return GetSymbolLoader().getBSymmgr();
	}

	private Name GetPredefName(PredefinedName pn)
	{
		return GetNameManager().GetPredefName(pn);
	}

	private AggregateSymbol GetOptPredefAgg(PredefinedType pt)
	{
		return GetSymbolLoader().GetOptPredefAgg(pt);
	}

	private CType LoadTypeFromSignature(int[] signature, ref int indexIntoSignatures, TypeArray classTyVars)
	{
		MethodSignatureEnum methodSignatureEnum = (MethodSignatureEnum)signature[indexIntoSignatures];
		indexIntoSignatures++;
		switch (methodSignatureEnum)
		{
		case MethodSignatureEnum.SIG_REF:
		{
			CType cType3 = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
			if (cType3 == null)
			{
				return null;
			}
			return GetTypeManager().GetParameterModifier(cType3, isOut: false);
		}
		case MethodSignatureEnum.SIG_OUT:
		{
			CType cType2 = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
			if (cType2 == null)
			{
				return null;
			}
			return GetTypeManager().GetParameterModifier(cType2, isOut: true);
		}
		case MethodSignatureEnum.SIG_SZ_ARRAY:
		{
			CType cType = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
			if (cType == null)
			{
				return null;
			}
			return GetTypeManager().GetArray(cType, 1);
		}
		case MethodSignatureEnum.SIG_METH_TYVAR:
		{
			int iv = signature[indexIntoSignatures];
			indexIntoSignatures++;
			return GetTypeManager().GetStdMethTypeVar(iv);
		}
		case MethodSignatureEnum.SIG_CLASS_TYVAR:
		{
			int i2 = signature[indexIntoSignatures];
			indexIntoSignatures++;
			return classTyVars.Item(i2);
		}
		case (MethodSignatureEnum)139:
			return GetTypeManager().GetVoid();
		default:
		{
			AggregateSymbol optPredefAgg = GetOptPredefAgg((PredefinedType)methodSignatureEnum);
			if (optPredefAgg != null)
			{
				CType[] array = new CType[optPredefAgg.GetTypeVars().size];
				for (int i = 0; i < optPredefAgg.GetTypeVars().size; i++)
				{
					array[i] = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
					if (array[i] == null)
					{
						return null;
					}
				}
				AggregateType aggregate = GetTypeManager().GetAggregate(optPredefAgg, getBSymmgr().AllocParams(optPredefAgg.GetTypeVars().size, array));
				if (aggregate.isPredefType(PredefinedType.PT_G_OPTIONAL))
				{
					return GetTypeManager().GetNubFromNullable(aggregate);
				}
				return aggregate;
			}
			return null;
		}
		}
	}

	private TypeArray LoadTypeArrayFromSignature(int[] signature, ref int indexIntoSignatures, TypeArray classTyVars)
	{
		int num = signature[indexIntoSignatures];
		indexIntoSignatures++;
		CType[] array = new CType[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
			if (array[i] == null)
			{
				return null;
			}
		}
		return getBSymmgr().AllocParams(num, array);
	}

	public PredefinedMembers(SymbolLoader loader)
	{
		m_loader = loader;
		m_methods = new MethodSymbol[109];
		m_properties = new PropertySymbol[3];
	}

	public PropertySymbol GetProperty(PREDEFPROP property)
	{
		PropertySymbol propertySymbol = EnsureProperty(property);
		if (propertySymbol == null)
		{
			ReportError(property);
		}
		return propertySymbol;
	}

	public MethodSymbol GetMethod(PREDEFMETH method)
	{
		MethodSymbol methodSymbol = EnsureMethod(method);
		if (methodSymbol == null)
		{
			ReportError(method);
		}
		return methodSymbol;
	}

	public MethodSymbol GetOptionalMethod(PREDEFMETH method)
	{
		return EnsureMethod(method);
	}

	private MethodSymbol EnsureMethod(PREDEFMETH method)
	{
		RETAILVERIFY(method > PREDEFMETH.PM_FIRST && method < PREDEFMETH.PM_COUNT);
		if (m_methods[(int)method] == null)
		{
			m_methods[(int)method] = LoadMethod(method);
		}
		return m_methods[(int)method];
	}

	private MethodSymbol LoadMethod(AggregateSymbol type, int[] signature, int cMethodTyVars, Name methodName, ACCESS methodAccess, bool isStatic, bool isVirtual)
	{
		if (type == null)
		{
			return null;
		}
		TypeArray typeVarsAll = type.GetTypeVarsAll();
		int indexIntoSignatures = 0;
		CType cType = LoadTypeFromSignature(signature, ref indexIntoSignatures, typeVarsAll);
		if (cType == null)
		{
			return null;
		}
		TypeArray typeArray = LoadTypeArrayFromSignature(signature, ref indexIntoSignatures, typeVarsAll);
		if (typeArray == null)
		{
			return null;
		}
		TypeArray stdMethTyVarArray = GetTypeManager().GetStdMethTyVarArray(cMethodTyVars);
		MethodSymbol methodSymbol = LookupMethodWhileLoading(type, cMethodTyVars, methodName, methodAccess, isStatic, isVirtual, cType, typeArray);
		if (methodSymbol == null)
		{
			RuntimeBinderSymbolTable.AddPredefinedMethodToSymbolTable(type, methodName);
			methodSymbol = LookupMethodWhileLoading(type, cMethodTyVars, methodName, methodAccess, isStatic, isVirtual, cType, typeArray);
		}
		return methodSymbol;
	}

	private MethodSymbol LookupMethodWhileLoading(AggregateSymbol type, int cMethodTyVars, Name methodName, ACCESS methodAccess, bool isStatic, bool isVirtual, CType returnType, TypeArray argumentTypes)
	{
		for (Symbol symbol = GetSymbolLoader().LookupAggMember(methodName, type, symbmask_t.MASK_ALL); symbol != null; symbol = GetSymbolLoader().LookupNextSym(symbol, type, symbmask_t.MASK_ALL))
		{
			if (symbol.IsMethodSymbol())
			{
				MethodSymbol methodSymbol = symbol.AsMethodSymbol();
				if ((methodSymbol.GetAccess() == methodAccess || methodAccess == ACCESS.ACC_UNKNOWN) && methodSymbol.isStatic == isStatic && methodSymbol.isVirtual == isVirtual && methodSymbol.typeVars.size == cMethodTyVars && GetTypeManager().SubstEqualTypes(methodSymbol.RetType, returnType, null, methodSymbol.typeVars, SubstTypeFlags.DenormMeth) && GetTypeManager().SubstEqualTypeArrays(methodSymbol.Params, argumentTypes, null, methodSymbol.typeVars, SubstTypeFlags.DenormMeth) && !methodSymbol.getBogus())
				{
					return methodSymbol;
				}
			}
		}
		return null;
	}

	private MethodSymbol LoadMethod(PREDEFMETH method)
	{
		return LoadMethod(GetMethParent(method), GetMethSignature(method), GetMethTyVars(method), GetMethName(method), GetMethAccess(method), IsMethStatic(method), IsMethVirtual(method));
	}

	private void ReportError(PREDEFMETH method)
	{
		ReportError(GetMethPredefType(method), GetMethPredefName(method));
	}

	private void ReportError(PredefinedType type, PredefinedName name)
	{
		GetErrorContext().Error(ErrorCode.ERR_MissingPredefinedMember, PredefinedTypes.GetFullName(type), GetPredefName(name));
	}

	private static PredefinedName GetPropPredefName(PREDEFPROP property)
	{
		return GetPropInfo(property).name;
	}

	private static PREDEFMETH GetPropGetter(PREDEFPROP property)
	{
		return GetPropInfo(property).getter;
	}

	private static PredefinedType GetPropPredefType(PREDEFPROP property)
	{
		return GetMethInfo(GetPropGetter(property)).type;
	}

	private static PREDEFMETH GetPropSetter(PREDEFPROP property)
	{
		PREDEFMETH setter = GetPropInfo(property).setter;
		return GetPropInfo(property).setter;
	}

	private void ReportError(PREDEFPROP property)
	{
		ReportError(GetPropPredefType(property), GetPropPredefName(property));
	}

	public static PredefinedPropertyInfo GetPropInfo(PREDEFPROP property)
	{
		RETAILVERIFY(property > PREDEFPROP.PP_FIRST && property < PREDEFPROP.PP_COUNT);
		RETAILVERIFY(g_predefinedProperties[(int)property].property == property);
		return g_predefinedProperties[(int)property];
	}

	public static PredefinedMethodInfo GetMethInfo(PREDEFMETH method)
	{
		RETAILVERIFY(method > PREDEFMETH.PM_FIRST && method < PREDEFMETH.PM_COUNT);
		RETAILVERIFY(g_predefinedMethods[(int)method].method == method);
		return g_predefinedMethods[(int)method];
	}

	private static PredefinedName GetMethPredefName(PREDEFMETH method)
	{
		return GetMethInfo(method).name;
	}

	private static PredefinedType GetMethPredefType(PREDEFMETH method)
	{
		return GetMethInfo(method).type;
	}

	private static bool IsMethStatic(PREDEFMETH method)
	{
		return GetMethInfo(method).callingConvention == MethodCallingConventionEnum.Static;
	}

	private static bool IsMethVirtual(PREDEFMETH method)
	{
		return GetMethInfo(method).callingConvention == MethodCallingConventionEnum.Virtual;
	}

	private static ACCESS GetMethAccess(PREDEFMETH method)
	{
		return GetMethInfo(method).access;
	}

	private static int GetMethTyVars(PREDEFMETH method)
	{
		return GetMethInfo(method).cTypeVars;
	}

	private static int[] GetMethSignature(PREDEFMETH method)
	{
		return GetMethInfo(method).signature;
	}
}
