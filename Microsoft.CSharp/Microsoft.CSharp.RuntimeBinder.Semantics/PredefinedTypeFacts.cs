using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal static class PredefinedTypeFacts
{
	private class PredefinedTypeInfo
	{
		internal PredefinedType type;

		internal string name;

		internal bool required;

		internal FUNDTYPE fundType;

		internal Type AssociatedSystemType;

		internal PredefinedTypeInfo(PredefinedType type, Type associatedSystemType, string name, bool required, int arity, AggKindEnum aggKind, FUNDTYPE fundType, bool inMscorlib)
		{
			this.type = type;
			this.name = name;
			this.required = required;
			this.fundType = fundType;
			AssociatedSystemType = associatedSystemType;
		}

		internal PredefinedTypeInfo(PredefinedType type, Type associatedSystemType, string name, bool required, int arity, bool inMscorlib)
			: this(type, associatedSystemType, name, required, arity, AggKindEnum.Class, FUNDTYPE.FT_REF, inMscorlib)
		{
		}
	}

	private static readonly Dictionary<string, PredefinedType> pdTypeNames;

	private static readonly PredefinedTypeInfo[] pdTypes;

	internal static string GetName(PredefinedType type)
	{
		return pdTypes[(uint)type].name;
	}

	internal static bool IsRequired(PredefinedType type)
	{
		return pdTypes[(uint)type].required;
	}

	internal static FUNDTYPE GetFundType(PredefinedType type)
	{
		return pdTypes[(uint)type].fundType;
	}

	internal static Type GetAssociatedSystemType(PredefinedType type)
	{
		return pdTypes[(uint)type].AssociatedSystemType;
	}

	internal static bool IsSimpleType(PredefinedType type)
	{
		if (type <= PredefinedType.PT_ULONG)
		{
			return true;
		}
		return false;
	}

	internal static bool IsNumericType(PredefinedType type)
	{
		if (type <= PredefinedType.PT_DECIMAL || type - 9 <= PredefinedType.PT_LONG)
		{
			return true;
		}
		return false;
	}

	internal static string GetNiceName(PredefinedType type)
	{
		return type switch
		{
			PredefinedType.PT_BYTE => "byte", 
			PredefinedType.PT_SHORT => "short", 
			PredefinedType.PT_INT => "int", 
			PredefinedType.PT_LONG => "long", 
			PredefinedType.PT_FLOAT => "float", 
			PredefinedType.PT_DOUBLE => "double", 
			PredefinedType.PT_DECIMAL => "decimal", 
			PredefinedType.PT_CHAR => "char", 
			PredefinedType.PT_BOOL => "bool", 
			PredefinedType.PT_SBYTE => "sbyte", 
			PredefinedType.PT_USHORT => "ushort", 
			PredefinedType.PT_UINT => "uint", 
			PredefinedType.PT_ULONG => "ulong", 
			PredefinedType.PT_OBJECT => "object", 
			PredefinedType.PT_STRING => "string", 
			_ => null, 
		};
	}

	internal static bool IsPredefinedType(string name)
	{
		return pdTypeNames.ContainsKey(name);
	}

	internal static PredefinedType GetPredefTypeIndex(string name)
	{
		return pdTypeNames[name];
	}

	static PredefinedTypeFacts()
	{
		pdTypeNames = new Dictionary<string, PredefinedType>();
		pdTypes = new PredefinedTypeInfo[138]
		{
			new PredefinedTypeInfo(PredefinedType.PT_BYTE, typeof(byte), "System.Byte", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U1, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_SHORT, typeof(short), "System.Int16", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I2, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_INT, typeof(int), "System.Int32", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I4, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_LONG, typeof(long), "System.Int64", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I8, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_FLOAT, typeof(float), "System.Single", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_R4, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DOUBLE, typeof(double), "System.Double", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_R8, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DECIMAL, typeof(decimal), "System.Decimal", required: false, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_CHAR, typeof(char), "System.Char", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U2, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_BOOL, typeof(bool), "System.Boolean", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I1, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_SBYTE, typeof(sbyte), "System.SByte", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I1, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_USHORT, typeof(ushort), "System.UInt16", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U2, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_UINT, typeof(uint), "System.UInt32", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U4, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ULONG, typeof(ulong), "System.UInt64", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U8, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_INTPTR, typeof(IntPtr), "System.IntPtr", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_UINTPTR, typeof(UIntPtr), "System.UIntPtr", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_OBJECT, typeof(object), "System.Object", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_STRING, typeof(string), "System.String", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DELEGATE, typeof(Delegate), "System.Delegate", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_MULTIDEL, typeof(MulticastDelegate), "System.MulticastDelegate", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ARRAY, typeof(Array), "System.Array", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_EXCEPTION, typeof(Exception), "System.Exception", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_TYPE, typeof(Type), "System.Type", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_MONITOR, typeof(Monitor), "System.Threading.Monitor", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_VALUE, typeof(ValueType), "System.ValueType", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ENUM, typeof(Enum), "System.Enum", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DATETIME, typeof(DateTime), "System.DateTime", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_SECURITYATTRIBUTE, typeof(CodeAccessSecurityAttribute), "System.Security.Permissions.CodeAccessSecurityAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_SECURITYPERMATTRIBUTE, typeof(SecurityPermissionAttribute), "System.Security.Permissions.SecurityPermissionAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_UNVERIFCODEATTRIBUTE, typeof(UnverifiableCodeAttribute), "System.Security.UnverifiableCodeAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEBUGGABLEATTRIBUTE, typeof(DebuggableAttribute), "System.Diagnostics.DebuggableAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEBUGGABLEATTRIBUTE_DEBUGGINGMODES, typeof(DebuggableAttribute.DebuggingModes), "System.Diagnostics.DebuggableAttribute.DebuggingModes", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_MARSHALBYREF, typeof(MarshalByRefObject), "System.MarshalByRefObject", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_CONTEXTBOUND, typeof(ContextBoundObject), "System.ContextBoundObject", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_IN, typeof(InAttribute), "System.Runtime.InteropServices.InAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_OUT, typeof(OutAttribute), "System.Runtime.InteropServices.OutAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTE, typeof(Attribute), "System.Attribute", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTEUSAGE, typeof(AttributeUsageAttribute), "System.AttributeUsageAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTETARGETS, typeof(AttributeTargets), "System.AttributeTargets", required: false, 0, AggKindEnum.Enum, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_OBSOLETE, typeof(ObsoleteAttribute), "System.ObsoleteAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_CONDITIONAL, typeof(ConditionalAttribute), "System.Diagnostics.ConditionalAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_CLSCOMPLIANT, typeof(CLSCompliantAttribute), "System.CLSCompliantAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_GUID, typeof(GuidAttribute), "System.Runtime.InteropServices.GuidAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEFAULTMEMBER, typeof(DefaultMemberAttribute), "System.Reflection.DefaultMemberAttribute", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_PARAMS, typeof(ParamArrayAttribute), "System.ParamArrayAttribute", required: true, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_COMIMPORT, typeof(ComImportAttribute), "System.Runtime.InteropServices.ComImportAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_FIELDOFFSET, typeof(FieldOffsetAttribute), "System.Runtime.InteropServices.FieldOffsetAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_STRUCTLAYOUT, typeof(StructLayoutAttribute), "System.Runtime.InteropServices.StructLayoutAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_LAYOUTKIND, typeof(LayoutKind), "System.Runtime.InteropServices.LayoutKind", required: false, 0, AggKindEnum.Enum, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_MARSHALAS, typeof(MarshalAsAttribute), "System.Runtime.InteropServices.MarshalAsAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DLLIMPORT, typeof(DllImportAttribute), "System.Runtime.InteropServices.DllImportAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_INDEXERNAME, typeof(IndexerNameAttribute), "System.Runtime.CompilerServices.IndexerNameAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DECIMALCONSTANT, typeof(DecimalConstantAttribute), "System.Runtime.CompilerServices.DecimalConstantAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_REQUIRED, typeof(RequiredAttributeAttribute), "System.Runtime.CompilerServices.RequiredAttributeAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEFAULTVALUE, typeof(DefaultParameterValueAttribute), "System.Runtime.InteropServices.DefaultParameterValueAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_UNMANAGEDFUNCTIONPOINTER, typeof(UnmanagedFunctionPointerAttribute), "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_CALLINGCONVENTION, typeof(CallingConvention), "System.Runtime.InteropServices.CallingConvention", required: false, 0, AggKindEnum.Enum, FUNDTYPE.FT_I4, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_CHARSET, typeof(CharSet), "System.Runtime.InteropServices.CharSet", required: false, 0, AggKindEnum.Enum, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_REFANY, typeof(TypedReference), "System.TypedReference", required: false, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ARGITERATOR, typeof(ArgIterator), "System.ArgIterator", required: false, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_TYPEHANDLE, typeof(RuntimeTypeHandle), "System.RuntimeTypeHandle", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_FIELDHANDLE, typeof(RuntimeFieldHandle), "System.RuntimeFieldHandle", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_METHODHANDLE, typeof(RuntimeMethodHandle), "System.RuntimeMethodHandle", required: false, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ARGUMENTHANDLE, typeof(RuntimeArgumentHandle), "System.RuntimeArgumentHandle", required: false, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_HASHTABLE, typeof(Hashtable), "System.Collections.Hashtable", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_DICTIONARY, typeof(Dictionary<, >), "System.Collections.Generic.Dictionary", required: false, 2, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_IASYNCRESULT, typeof(IAsyncResult), "System.IAsyncResult", required: false, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ASYNCCBDEL, typeof(AsyncCallback), "System.AsyncCallback", required: false, 0, AggKindEnum.Delegate, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_SECURITYACTION, typeof(SecurityAction), "System.Security.Permissions.SecurityAction", required: false, 0, AggKindEnum.Enum, FUNDTYPE.FT_I4, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_IDISPOSABLE, typeof(IDisposable), "System.IDisposable", required: true, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_IENUMERABLE, typeof(IEnumerable), "System.Collections.IEnumerable", required: true, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_IENUMERATOR, typeof(IEnumerator), "System.Collections.IEnumerator", required: true, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_SYSTEMVOID, typeof(void), "System.Void", required: true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_RUNTIMEHELPERS, typeof(RuntimeHelpers), "System.Runtime.CompilerServices.RuntimeHelpers", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_VOLATILEMOD, typeof(IsVolatile), "System.Runtime.CompilerServices.IsVolatile", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_COCLASS, typeof(CoClassAttribute), "System.Runtime.InteropServices.CoClassAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ACTIVATOR, typeof(Activator), "System.Activator", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_IENUMERABLE, typeof(IEnumerable<>), "System.Collections.Generic.IEnumerable", required: false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_IENUMERATOR, typeof(IEnumerator<>), "System.Collections.Generic.IEnumerator", required: false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_OPTIONAL, typeof(Nullable<>), "System.Nullable", required: false, 1, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_FIXEDBUFFER, typeof(FixedBufferAttribute), "System.Runtime.CompilerServices.FixedBufferAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEFAULTCHARSET, typeof(DefaultCharSetAttribute), "System.Runtime.InteropServices.DefaultCharSetAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_COMPILATIONRELAXATIONS, typeof(CompilationRelaxationsAttribute), "System.Runtime.CompilerServices.CompilationRelaxationsAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_RUNTIMECOMPATIBILITY, typeof(RuntimeCompatibilityAttribute), "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_FRIENDASSEMBLY, typeof(InternalsVisibleToAttribute), "System.Runtime.CompilerServices.InternalsVisibleToAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERHIDDEN, typeof(DebuggerHiddenAttribute), "System.Diagnostics.DebuggerHiddenAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_TYPEFORWARDER, typeof(TypeForwardedToAttribute), "System.Runtime.CompilerServices.TypeForwardedToAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_KEYFILE, typeof(AssemblyKeyFileAttribute), "System.Reflection.AssemblyKeyFileAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_KEYNAME, typeof(AssemblyKeyNameAttribute), "System.Reflection.AssemblyKeyNameAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DELAYSIGN, typeof(AssemblyDelaySignAttribute), "System.Reflection.AssemblyDelaySignAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_NOTSUPPORTEDEXCEPTION, typeof(NotSupportedException), "System.NotSupportedException", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_THREAD, typeof(Thread), "System.Threading.Thread", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_COMPILERGENERATED, typeof(CompilerGeneratedAttribute), "System.Runtime.CompilerServices.CompilerGeneratedAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_UNSAFEVALUETYPE, typeof(UnsafeValueTypeAttribute), "System.Runtime.CompilerServices.UnsafeValueTypeAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYFLAGS, typeof(AssemblyFlagsAttribute), "System.Reflection.AssemblyFlagsAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYVERSION, typeof(AssemblyVersionAttribute), "System.Reflection.AssemblyVersionAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYCULTURE, typeof(AssemblyCultureAttribute), "System.Reflection.AssemblyCultureAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_IQUERYABLE, typeof(IQueryable<>), "System.Linq.IQueryable`1", required: false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_IQUERYABLE, typeof(IQueryable), "System.Linq.IQueryable", required: false, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_STRINGBUILDER, typeof(StringBuilder), "System.Text.StringBuilder", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_ICOLLECTION, typeof(ICollection<>), "System.Collections.Generic.ICollection", required: false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_ILIST, typeof(IList<>), "System.Collections.Generic.IList", required: false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_EXTENSION, typeof(ExtensionAttribute), "System.Runtime.CompilerServices.ExtensionAttribute", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_G_EXPRESSION, typeof(Expression<>), "System.Linq.Expressions.Expression", required: false, 1, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_EXPRESSION, typeof(Expression), "System.Linq.Expressions.Expression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_LAMBDAEXPRESSION, typeof(LambdaExpression), "System.Linq.Expressions.LambdaExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_BINARYEXPRESSION, typeof(BinaryExpression), "System.Linq.Expressions.BinaryExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_UNARYEXPRESSION, typeof(UnaryExpression), "System.Linq.Expressions.UnaryExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_CONDITIONALEXPRESSION, typeof(ConditionalExpression), "System.Linq.Expressions.ConditionalExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_CONSTANTEXPRESSION, typeof(ConstantExpression), "System.Linq.Expressions.ConstantExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_PARAMETEREXPRESSION, typeof(ParameterExpression), "System.Linq.Expressions.ParameterExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_MEMBEREXPRESSION, typeof(MemberExpression), "System.Linq.Expressions.MemberExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_METHODCALLEXPRESSION, typeof(MethodCallExpression), "System.Linq.Expressions.MethodCallExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_NEWEXPRESSION, typeof(NewExpression), "System.Linq.Expressions.NewExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_BINDING, typeof(MemberBinding), "System.Linq.Expressions.MemberBinding", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_MEMBERINITEXPRESSION, typeof(MemberInitExpression), "System.Linq.Expressions.MemberInitExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_LISTINITEXPRESSION, typeof(ListInitExpression), "System.Linq.Expressions.ListInitExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_TYPEBINARYEXPRESSION, typeof(TypeBinaryExpression), "System.Linq.Expressions.TypeBinaryExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_NEWARRAYEXPRESSION, typeof(NewArrayExpression), "System.Linq.Expressions.NewArrayExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_MEMBERASSIGNMENT, typeof(MemberAssignment), "System.Linq.Expressions.MemberAssignment", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_MEMBERLISTBINDING, typeof(MemberListBinding), "System.Linq.Expressions.MemberListBinding", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_MEMBERMEMBERBINDING, typeof(MemberMemberBinding), "System.Linq.Expressions.MemberMemberBinding", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_INVOCATIONEXPRESSION, typeof(InvocationExpression), "System.Linq.Expressions.InvocationExpression", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_FIELDINFO, typeof(FieldInfo), "System.Reflection.FieldInfo", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_METHODINFO, typeof(MethodInfo), "System.Reflection.MethodInfo", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_CONSTRUCTORINFO, typeof(ConstructorInfo), "System.Reflection.ConstructorInfo", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_PROPERTYINFO, typeof(PropertyInfo), "System.Reflection.PropertyInfo", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_METHODBASE, typeof(MethodBase), "System.Reflection.MethodBase", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_MEMBERINFO, typeof(MemberInfo), "System.Reflection.MemberInfo", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERDISPLAY, typeof(DebuggerDisplayAttribute), "System.Diagnostics.DebuggerDisplayAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERBROWSABLE, typeof(DebuggerBrowsableAttribute), "System.Diagnostics.DebuggerBrowsableAttribute", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERBROWSABLESTATE, typeof(DebuggerBrowsableState), "System.Diagnostics.DebuggerBrowsableState", required: false, 0, AggKindEnum.Enum, FUNDTYPE.FT_I4, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_EQUALITYCOMPARER, typeof(EqualityComparer<>), "System.Collections.Generic.EqualityComparer", required: false, 1, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_ELEMENTINITIALIZER, typeof(ElementInit), "System.Linq.Expressions.ElementInit", required: false, 0, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_UNKNOWNWRAPPER, typeof(UnknownWrapper), "System.Runtime.InteropServices.UnknownWrapper", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_DISPATCHWRAPPER, typeof(DispatchWrapper), "System.Runtime.InteropServices.DispatchWrapper", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_MISSING, typeof(Missing), "System.Reflection.Missing", required: false, 0, inMscorlib: true),
			new PredefinedTypeInfo(PredefinedType.PT_G_IREADONLYLIST, typeof(IReadOnlyList<>), "System.Collections.Generic.IReadOnlyList", required: false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: false),
			new PredefinedTypeInfo(PredefinedType.PT_G_IREADONLYCOLLECTION, typeof(IReadOnlyCollection<>), "System.Collections.Generic.IReadOnlyCollection", required: false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, inMscorlib: false)
		};
		for (int i = 0; i < 138; i++)
		{
			pdTypeNames.Add(pdTypes[i].AssociatedSystemType.FullName, (PredefinedType)i);
		}
	}
}
