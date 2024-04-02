using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder;

internal class SymbolTable
{
	private sealed class NameHashKey
	{
		internal readonly Type type;

		internal readonly string name;

		public NameHashKey(Type type, string name)
		{
			this.type = type;
			this.name = name;
		}

		public override bool Equals(object obj)
		{
			if (obj is NameHashKey nameHashKey && type.Equals(nameHashKey.type))
			{
				return name.Equals(nameHashKey.name);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return type.GetHashCode() ^ name.GetHashCode();
		}
	}

	private HashSet<Type> m_typesWithConversionsLoaded;

	private HashSet<NameHashKey> m_namesLoadedForEachType;

	private SYMTBL m_symbolTable;

	private SymFactory m_symFactory;

	private NameManager m_nameManager;

	private TypeManager m_typeManager;

	private BSYMMGR m_bsymmgr;

	private CSemanticChecker m_semanticChecker;

	private NamespaceSymbol m_rootNamespace;

	private InputFile m_infile;

	private static Func<MethodBase, bool> s_IsInvokableDelegate = GetIsInvokableDelegate();

	private static Func<MethodBase, bool> GetIsInvokableDelegate()
	{
		Func<MethodBase, bool> result = null;
		MethodInfo method = typeof(MethodBase).GetMethod("get_IsDynamicallyInvokable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		if (method != null)
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			if (currentDomain.IsHomogenous && currentDomain.IsFullyTrusted)
			{
				try
				{
					result = (Func<MethodBase, bool>)method.CreateDelegate(typeof(Func<MethodBase, bool>));
				}
				catch (SecurityException)
				{
				}
			}
		}
		return result;
	}

	internal SymbolTable(SYMTBL symTable, SymFactory symFactory, NameManager nameManager, TypeManager typeManager, BSYMMGR bsymmgr, CSemanticChecker semanticChecker, InputFile infile)
	{
		m_symbolTable = symTable;
		m_symFactory = symFactory;
		m_nameManager = nameManager;
		m_typeManager = typeManager;
		m_bsymmgr = bsymmgr;
		m_semanticChecker = semanticChecker;
		m_infile = infile;
		ClearCache();
	}

	internal void ClearCache()
	{
		m_typesWithConversionsLoaded = new HashSet<Type>();
		m_namesLoadedForEachType = new HashSet<NameHashKey>();
		m_rootNamespace = m_bsymmgr.GetRootNS();
		LoadSymbolsFromType(typeof(object));
	}

	internal void PopulateSymbolTableWithName(string name, IEnumerable<Type> typeArguments, Type callingType)
	{
		if (callingType.IsGenericType)
		{
			callingType = callingType.GetGenericTypeDefinition();
		}
		if (name == "$Item$")
		{
			name = ((!(callingType == typeof(string))) ? "Item" : "Chars");
		}
		NameHashKey nameHashKey = new NameHashKey(callingType, name);
		if (m_namesLoadedForEachType.Contains(nameHashKey))
		{
			return;
		}
		IEnumerable<MemberInfo> enumerable = AddNamesOnType(nameHashKey, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (enumerable != null)
		{
			foreach (MemberInfo item in enumerable)
			{
				if (item is MethodInfo)
				{
					ParameterInfo[] parameters = (item as MethodInfo).GetParameters();
					foreach (ParameterInfo parameterInfo in parameters)
					{
						AddConversionsForType(parameterInfo.ParameterType);
					}
				}
				else if (item is ConstructorInfo)
				{
					ParameterInfo[] parameters2 = (item as ConstructorInfo).GetParameters();
					foreach (ParameterInfo parameterInfo2 in parameters2)
					{
						AddConversionsForType(parameterInfo2.ParameterType);
					}
				}
			}
		}
		if (typeArguments == null)
		{
			return;
		}
		foreach (Type typeArgument in typeArguments)
		{
			AddConversionsForType(typeArgument);
		}
	}

	internal SymWithType LookupMember(string name, EXPR callingObject, ParentSymbol context, int arity, MemberLookup mem, bool allowSpecialNames, bool requireInvocable)
	{
		CType cType = callingObject.type;
		if (cType.IsArrayType())
		{
			cType = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_ARRAY);
		}
		if (cType.IsNullableType())
		{
			cType = cType.AsNullableType().GetAts(m_semanticChecker.GetSymbolLoader().GetErrorContext());
		}
		if (!mem.Lookup(m_semanticChecker, cType, callingObject, context, GetName(name), arity, MemLookFlags.TypeVarsAllowed | ((!allowSpecialNames) ? MemLookFlags.UserCallable : MemLookFlags.None) | ((name == "$Item$") ? MemLookFlags.Indexer : MemLookFlags.None) | ((name == ".ctor") ? MemLookFlags.Ctor : MemLookFlags.None) | (requireInvocable ? MemLookFlags.MustBeInvocable : MemLookFlags.None)))
		{
			return null;
		}
		return mem.SwtFirst();
	}

	private IEnumerable<MemberInfo> AddNamesOnType(NameHashKey key, BindingFlags flags)
	{
		List<Type> inheritance = CreateInheritanceHierarchyList(key.type);
		return AddNamesInInheritanceHierarchy(key.name, flags, inheritance);
	}

	private IEnumerable<MemberInfo> AddNamesInInheritanceHierarchy(string name, BindingFlags flags, List<Type> inheritance)
	{
		IEnumerable<MemberInfo> enumerable = new MemberInfo[0];
		foreach (Type item2 in inheritance)
		{
			Type type = item2;
			if (type.IsGenericType)
			{
				type = type.GetGenericTypeDefinition();
			}
			NameHashKey item = new NameHashKey(type, name);
			IEnumerable<MemberInfo> enumerable2 = from member in type.GetMembers(flags)
				where member.Name == name && member.DeclaringType == type
				select member;
			IEnumerable<MemberInfo> enumerable3 = from member in type.GetMembers(flags)
				where member.Name == name && member.DeclaringType == type && member is EventInfo
				select member;
			if (enumerable2.Any())
			{
				CType cTypeFromType = GetCTypeFromType(type);
				if (!(cTypeFromType is AggregateType))
				{
					continue;
				}
				AggregateSymbol aggregate = (cTypeFromType as AggregateType).getAggregate();
				FieldSymbol addedField = null;
				foreach (MemberInfo item3 in enumerable2)
				{
					if (item3 is MethodInfo)
					{
						MethodKindEnum kind = MethodKindEnum.Actual;
						if (item3.Name == "Invoke")
						{
							kind = MethodKindEnum.Invoke;
						}
						else if (item3.Name == "op_Implicit")
						{
							kind = MethodKindEnum.ImplicitConv;
						}
						else if (item3.Name == "op_Explicit")
						{
							kind = MethodKindEnum.ExplicitConv;
						}
						AddMethodToSymbolTable(item3, aggregate, kind);
					}
					else if (item3 is ConstructorInfo)
					{
						AddMethodToSymbolTable(item3, aggregate, MethodKindEnum.Constructor);
					}
					else if (item3 is PropertyInfo)
					{
						AddPropertyToSymbolTable(item3 as PropertyInfo, aggregate);
					}
					else if (item3 is FieldInfo)
					{
						addedField = AddFieldToSymbolTable(item3 as FieldInfo, aggregate);
					}
				}
				foreach (EventInfo item4 in enumerable3)
				{
					AddEventToSymbolTable(item4, aggregate, addedField);
				}
				enumerable = enumerable.Concat(enumerable2);
			}
			m_namesLoadedForEachType.Add(item);
		}
		return enumerable;
	}

	private List<Type> CreateInheritanceHierarchyList(Type type)
	{
		List<Type> list = new List<Type>();
		list.Insert(0, type);
		Type baseType = type.BaseType;
		while (baseType != null)
		{
			LoadSymbolsFromType(baseType);
			list.Insert(0, baseType);
			baseType = baseType.BaseType;
		}
		CType cTypeFromType = GetCTypeFromType(type);
		if (cTypeFromType.IsWindowsRuntimeType())
		{
			TypeArray winRTCollectionIfacesAll = cTypeFromType.AsAggregateType().GetWinRTCollectionIfacesAll(m_semanticChecker.GetSymbolLoader());
			for (int i = 0; i < winRTCollectionIfacesAll.size; i++)
			{
				CType cType = winRTCollectionIfacesAll.Item(i);
				list.Insert(0, cType.AssociatedSystemType);
			}
		}
		return list;
	}

	private Name GetName(string p)
	{
		if (p == null)
		{
			p = string.Empty;
		}
		return GetName(p, m_nameManager);
	}

	private Name GetName(Type type)
	{
		string text = type.Name;
		if (type.IsGenericType)
		{
			text = text.Split('`')[0];
		}
		return GetName(text, m_nameManager);
	}

	internal static Name GetName(string p, NameManager nameManager)
	{
		Name name = nameManager.Lookup(p);
		if (name == null)
		{
			return nameManager.Add(p);
		}
		return name;
	}

	private TypeArray GetMethodTypeParameters(MethodInfo method, MethodSymbol parent)
	{
		if (method.IsGenericMethod)
		{
			Type[] genericArguments = method.GetGenericArguments();
			CType[] array = new CType[genericArguments.Length];
			for (int i = 0; i < genericArguments.Length; i++)
			{
				Type t = genericArguments[i];
				array[i] = LoadMethodTypeParameter(parent, t);
			}
			for (int j = 0; j < genericArguments.Length; j++)
			{
				Type type = genericArguments[j];
				array[j].AsTypeParameterType().GetTypeParameterSymbol().SetBounds(m_bsymmgr.AllocParams(GetCTypeArrayFromTypes(type.GetGenericParameterConstraints())));
			}
			return m_bsymmgr.AllocParams(array.Length, array);
		}
		return BSYMMGR.EmptyTypeArray();
	}

	private TypeArray GetAggregateTypeParameters(Type type, AggregateSymbol agg)
	{
		if (type.IsGenericType)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			Type[] genericArguments = genericTypeDefinition.GetGenericArguments();
			List<CType> list = new List<CType>();
			int num = (agg.isNested() ? agg.GetOuterAgg().GetTypeVarsAll().size : 0);
			foreach (Type type2 in genericArguments)
			{
				if (type2.GenericParameterPosition >= num)
				{
					CType cType = null;
					cType = ((!type2.IsGenericParameter || !(type2.DeclaringType == genericTypeDefinition)) ? GetCTypeFromType(type2) : LoadClassTypeParameter(agg, type2));
					if (cType.AsTypeParameterType().GetOwningSymbol() == agg)
					{
						list.Add(cType);
					}
				}
			}
			return m_bsymmgr.AllocParams(list.Count, list.ToArray());
		}
		return BSYMMGR.EmptyTypeArray();
	}

	private TypeParameterType LoadClassTypeParameter(AggregateSymbol parent, Type t)
	{
		for (AggregateSymbol aggregateSymbol = parent; aggregateSymbol != null; aggregateSymbol = (aggregateSymbol.parent.IsAggregateSymbol() ? aggregateSymbol.parent.AsAggregateSymbol() : null))
		{
			for (TypeParameterSymbol typeParameterSymbol = m_bsymmgr.LookupAggMember(GetName(t), aggregateSymbol, symbmask_t.MASK_TypeParameterSymbol) as TypeParameterSymbol; typeParameterSymbol != null; typeParameterSymbol = BSYMMGR.LookupNextSym(typeParameterSymbol, aggregateSymbol, symbmask_t.MASK_TypeParameterSymbol) as TypeParameterSymbol)
			{
				if (AreTypeParametersEquivalent(typeParameterSymbol.GetTypeParameterType().AssociatedSystemType, t))
				{
					return typeParameterSymbol.GetTypeParameterType();
				}
			}
		}
		return AddTypeParameterToSymbolTable(parent, null, t, bIsAggregate: true);
	}

	private bool AreTypeParametersEquivalent(Type t1, Type t2)
	{
		if (t1 == t2)
		{
			return true;
		}
		Type originalTypeParameterType = GetOriginalTypeParameterType(t1);
		Type originalTypeParameterType2 = GetOriginalTypeParameterType(t2);
		return originalTypeParameterType == originalTypeParameterType2;
	}

	private Type GetOriginalTypeParameterType(Type t)
	{
		int genericParameterPosition = t.GenericParameterPosition;
		Type type = t.DeclaringType;
		if (type != null && type.IsGenericType)
		{
			type = type.GetGenericTypeDefinition();
		}
		if (t.DeclaringMethod != null)
		{
			MethodBase declaringMethod = t.DeclaringMethod;
			if (type.GetGenericArguments() == null || genericParameterPosition >= type.GetGenericArguments().Length)
			{
				return t;
			}
		}
		while (type.GetGenericArguments().Length > genericParameterPosition)
		{
			Type type2 = type.DeclaringType;
			if (type2 != null && type2.IsGenericType)
			{
				type2 = type2.GetGenericTypeDefinition();
			}
			if (!(type2 != null) || type2.GetGenericArguments() == null || type2.GetGenericArguments().Length <= genericParameterPosition)
			{
				break;
			}
			type = type2;
		}
		return type.GetGenericArguments()[genericParameterPosition];
	}

	private TypeParameterType LoadMethodTypeParameter(MethodSymbol parent, Type t)
	{
		for (Symbol symbol = parent.firstChild; symbol != null; symbol = symbol.nextChild)
		{
			if (symbol.IsTypeParameterSymbol() && AreTypeParametersEquivalent(symbol.AsTypeParameterSymbol().GetTypeParameterType().AssociatedSystemType, t))
			{
				return symbol.AsTypeParameterSymbol().GetTypeParameterType();
			}
		}
		return AddTypeParameterToSymbolTable(null, parent, t, bIsAggregate: false);
	}

	private TypeParameterType AddTypeParameterToSymbolTable(AggregateSymbol agg, MethodSymbol meth, Type t, bool bIsAggregate)
	{
		TypeParameterSymbol typeParameterSymbol = ((!bIsAggregate) ? m_symFactory.CreateMethodTypeParameter(GetName(t), meth, t.GenericParameterPosition, t.GenericParameterPosition) : m_symFactory.CreateClassTypeParameter(GetName(t), agg, t.GenericParameterPosition, t.GenericParameterPosition));
		if ((t.GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0)
		{
			typeParameterSymbol.Covariant = true;
		}
		if ((t.GenericParameterAttributes & GenericParameterAttributes.Contravariant) != 0)
		{
			typeParameterSymbol.Contravariant = true;
		}
		SpecCons specCons = SpecCons.None;
		if ((t.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
		{
			specCons |= SpecCons.New;
		}
		if ((t.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
		{
			specCons |= SpecCons.Ref;
		}
		if ((t.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
		{
			specCons |= SpecCons.Val;
		}
		typeParameterSymbol.SetConstraints(specCons);
		typeParameterSymbol.SetAccess(ACCESS.ACC_PUBLIC);
		return m_typeManager.GetTypeParameter(typeParameterSymbol);
	}

	private CType LoadSymbolsFromType(Type originalType)
	{
		List<object> list = BuildDeclarationChain(originalType);
		Type type = originalType;
		CType cType = null;
		bool isByRef = type.IsByRef;
		if (isByRef)
		{
			type = type.GetElementType();
		}
		NamespaceOrAggregateSymbol namespaceOrAggregateSymbol = m_rootNamespace;
		NamespaceOrAggregateSymbol namespaceOrAggregateSymbol2 = null;
		for (int i = 0; i < list.Count; i++)
		{
			object obj = list[i];
			if (obj is Type)
			{
				Type type2 = obj as Type;
				Name name = null;
				name = GetName(type2);
				namespaceOrAggregateSymbol2 = m_symbolTable.LookupSym(name, namespaceOrAggregateSymbol, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol();
				if (namespaceOrAggregateSymbol2 != null)
				{
					namespaceOrAggregateSymbol2 = FindSymWithMatchingArity(namespaceOrAggregateSymbol2 as AggregateSymbol, type2);
				}
				if (namespaceOrAggregateSymbol2 != null && namespaceOrAggregateSymbol2 is AggregateSymbol)
				{
					Type associatedSystemType = (namespaceOrAggregateSymbol2 as AggregateSymbol).AssociatedSystemType;
					Type other = (type2.IsGenericType ? type2.GetGenericTypeDefinition() : type2);
					if (!associatedSystemType.IsEquivalentTo(other))
					{
						throw new ResetBindException();
					}
				}
				if (namespaceOrAggregateSymbol2 == null || type2.IsNullableType())
				{
					CType cType2 = ProcessSpecialTypeInChain(namespaceOrAggregateSymbol, type2);
					if (cType2 != null)
					{
						if (!cType2.IsAggregateType())
						{
							cType = cType2;
							break;
						}
						namespaceOrAggregateSymbol2 = cType2.AsAggregateType().GetOwningAggregate();
					}
					else
					{
						namespaceOrAggregateSymbol2 = AddAggregateToSymbolTable(namespaceOrAggregateSymbol, type2);
					}
				}
				if (type2 == type)
				{
					cType = GetConstructedType(type, namespaceOrAggregateSymbol2.AsAggregateSymbol());
					break;
				}
			}
			else
			{
				if (obj is MethodInfo)
				{
					cType = ProcessMethodTypeParameter(obj as MethodInfo, list[++i] as Type, namespaceOrAggregateSymbol as AggregateSymbol);
					break;
				}
				namespaceOrAggregateSymbol2 = AddNamespaceToSymbolTable(namespaceOrAggregateSymbol, obj as string);
			}
			namespaceOrAggregateSymbol = namespaceOrAggregateSymbol2;
		}
		if (isByRef)
		{
			cType = m_typeManager.GetParameterModifier(cType, isOut: false);
		}
		return cType;
	}

	private TypeParameterType ProcessMethodTypeParameter(MethodInfo methinfo, Type t, AggregateSymbol parent)
	{
		MethodSymbol methodSymbol = FindMatchingMethod(methinfo, parent);
		if (methodSymbol == null)
		{
			methodSymbol = AddMethodToSymbolTable(methinfo, parent, MethodKindEnum.Actual);
		}
		return LoadMethodTypeParameter(methodSymbol, t);
	}

	private CType GetConstructedType(Type type, AggregateSymbol agg)
	{
		if (type.IsGenericType)
		{
			List<CType> list = new List<CType>();
			Type[] genericArguments = type.GetGenericArguments();
			foreach (Type t in genericArguments)
			{
				list.Add(GetCTypeFromType(t));
			}
			TypeArray typeArgsAll = m_bsymmgr.AllocParams(list.ToArray());
			return m_typeManager.GetAggregate(agg, typeArgsAll);
		}
		return agg.getThisType();
	}

	private CType ProcessSpecialTypeInChain(NamespaceOrAggregateSymbol parent, Type t)
	{
		if (t.IsGenericParameter)
		{
			AggregateSymbol parent2 = parent as AggregateSymbol;
			return LoadClassTypeParameter(parent2, t);
		}
		if (t.IsArray)
		{
			return m_typeManager.GetArray(GetCTypeFromType(t.GetElementType()), t.GetArrayRank());
		}
		if (t.IsPointer)
		{
			return m_typeManager.GetPointer(GetCTypeFromType(t.GetElementType()));
		}
		if (t.IsNullableType())
		{
			if (t.GetGenericArguments()[0].DeclaringType == t)
			{
				AggregateSymbol aggregateSymbol = m_symbolTable.LookupSym(GetName(t), parent, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol();
				if (aggregateSymbol != null)
				{
					aggregateSymbol = FindSymWithMatchingArity(aggregateSymbol, t);
					if (aggregateSymbol != null)
					{
						return aggregateSymbol.getThisType();
					}
				}
				return AddAggregateToSymbolTable(parent, t).getThisType();
			}
			return m_typeManager.GetNullable(GetCTypeFromType(t.GetGenericArguments()[0]));
		}
		return null;
	}

	private static List<object> BuildDeclarationChain(Type callingType)
	{
		if (callingType.IsByRef)
		{
			callingType = callingType.GetElementType();
		}
		List<object> list = new List<object>();
		Type type = callingType;
		while (type != null)
		{
			list.Add(type);
			if (type.IsGenericParameter && type.DeclaringMethod != null)
			{
				MethodBase methodBase = type.DeclaringMethod;
				ParameterInfo[] parameters = methodBase.GetParameters();
				bool flag = false;
				foreach (MethodInfo item2 in from m in type.DeclaringType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
					where m.MetadataToken == methodBase.MetadataToken
					select m)
				{
					if (item2.IsGenericMethod)
					{
						list.Add(item2);
						flag = true;
					}
				}
			}
			type = type.DeclaringType;
		}
		list.Reverse();
		if (callingType.Namespace != null)
		{
			string[] array = callingType.Namespace.Split('.');
			int num = 0;
			string[] array2 = array;
			foreach (string item in array2)
			{
				list.Insert(num++, item);
			}
		}
		return list;
	}

	private AggregateSymbol FindSymWithMatchingArity(AggregateSymbol aggregateSymbol, Type type)
	{
		for (AggregateSymbol aggregateSymbol2 = aggregateSymbol; aggregateSymbol2 != null; aggregateSymbol2 = BSYMMGR.LookupNextSym(aggregateSymbol2, aggregateSymbol2.Parent, symbmask_t.MASK_AggregateSymbol) as AggregateSymbol)
		{
			if (aggregateSymbol2.GetTypeVarsAll().size == type.GetGenericArguments().Length)
			{
				return aggregateSymbol2;
			}
		}
		return null;
	}

	private NamespaceSymbol AddNamespaceToSymbolTable(NamespaceOrAggregateSymbol parent, string sz)
	{
		Name name = GetName(sz);
		NamespaceSymbol namespaceSymbol = m_symbolTable.LookupSym(name, parent, symbmask_t.MASK_NamespaceSymbol).AsNamespaceSymbol();
		if (namespaceSymbol == null)
		{
			namespaceSymbol = m_symFactory.CreateNamespace(name, parent as NamespaceSymbol);
		}
		namespaceSymbol.AddAid(KAID.kaidGlobal);
		namespaceSymbol.AddAid(KAID.kaidThisAssembly);
		namespaceSymbol.AddAid(m_infile.GetAssemblyID());
		return namespaceSymbol;
	}

	internal CType[] GetCTypeArrayFromTypes(IList<Type> types)
	{
		if (types == null)
		{
			return null;
		}
		CType[] array = new CType[types.Count];
		int num = 0;
		foreach (Type type in types)
		{
			array[num++] = GetCTypeFromType(type);
		}
		return array;
	}

	internal CType GetCTypeFromType(Type t)
	{
		return LoadSymbolsFromType(t);
	}

	private AggregateSymbol AddAggregateToSymbolTable(NamespaceOrAggregateSymbol parent, Type type)
	{
		AggregateSymbol aggregateSymbol = m_symFactory.CreateAggregate(GetName(type), parent, m_infile, m_typeManager);
		aggregateSymbol.AssociatedSystemType = (type.IsGenericType ? type.GetGenericTypeDefinition() : type);
		aggregateSymbol.AssociatedAssembly = type.Assembly;
		AggKindEnum aggKind;
		if (type.IsInterface)
		{
			aggKind = AggKindEnum.Interface;
		}
		else if (!type.IsEnum)
		{
			aggKind = (type.IsValueType ? AggKindEnum.Struct : ((!(type.BaseType != null) || (!(type.BaseType.FullName == "System.MulticastDelegate") && !(type.BaseType.FullName == "System.Delegate")) || !(type.FullName != "System.MulticastDelegate")) ? AggKindEnum.Class : AggKindEnum.Delegate));
		}
		else
		{
			aggKind = AggKindEnum.Enum;
			aggregateSymbol.SetUnderlyingType(GetCTypeFromType(Enum.GetUnderlyingType(type)).AsAggregateType());
		}
		aggregateSymbol.SetAggKind(aggKind);
		aggregateSymbol.SetTypeVars(BSYMMGR.EmptyTypeArray());
		ACCESS access = (type.IsPublic ? ACCESS.ACC_PUBLIC : ((!type.IsNested) ? ACCESS.ACC_INTERNAL : ((!type.IsNestedAssembly && !type.IsNestedFamANDAssem) ? (type.IsNestedFamORAssem ? ACCESS.ACC_INTERNALPROTECTED : (type.IsNestedPrivate ? ACCESS.ACC_PRIVATE : ((!type.IsNestedFamily) ? ACCESS.ACC_PUBLIC : ACCESS.ACC_PROTECTED))) : ACCESS.ACC_INTERNAL)));
		aggregateSymbol.SetAccess(access);
		if (!type.IsGenericParameter)
		{
			aggregateSymbol.SetTypeVars(GetAggregateTypeParameters(type, aggregateSymbol));
		}
		if (type.IsGenericType)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			Type[] genericArguments = genericTypeDefinition.GetGenericArguments();
			for (int i = 0; i < aggregateSymbol.GetTypeVars().size; i++)
			{
				Type type2 = genericArguments[i];
				if (aggregateSymbol.GetTypeVars().Item(i).IsTypeParameterType())
				{
					aggregateSymbol.GetTypeVars().Item(i).AsTypeParameterType()
						.GetTypeParameterSymbol()
						.SetBounds(m_bsymmgr.AllocParams(GetCTypeArrayFromTypes(type2.GetGenericParameterConstraints())));
				}
			}
		}
		aggregateSymbol.SetAnonymousType(isAnonymousType: false);
		aggregateSymbol.SetAbstract(type.IsAbstract);
		string fullName = type.FullName;
		if (type.IsGenericType)
		{
			fullName = type.GetGenericTypeDefinition().FullName;
		}
		if (fullName != null && PredefinedTypeFacts.IsPredefinedType(fullName))
		{
			PredefinedTypes.InitializePredefinedType(aggregateSymbol, PredefinedTypeFacts.GetPredefTypeIndex(fullName));
		}
		aggregateSymbol.SetLayoutError(layoutError: false);
		aggregateSymbol.SetSealed(type.IsSealed);
		aggregateSymbol.SetUnmanagedStruct(unmanagedStruct: false);
		aggregateSymbol.SetManagedStruct(managedStruct: false);
		aggregateSymbol.SetHasExternReference(hasExternReference: false);
		aggregateSymbol.SetComImport(type.IsImport);
		AggregateType thisType = aggregateSymbol.getThisType();
		if (type.BaseType != null)
		{
			Type type3 = type.BaseType;
			if (type3.IsGenericType)
			{
				type3 = type3.GetGenericTypeDefinition();
			}
			aggregateSymbol.SetBaseClass(GetCTypeFromType(type3).AsAggregateType());
		}
		aggregateSymbol.SetTypeManager(m_typeManager);
		aggregateSymbol.SetFirstUDConversion(null);
		SetInterfacesOnAggregate(aggregateSymbol, type);
		aggregateSymbol.SetHasPubNoArgCtor(type.GetConstructors().Any((ConstructorInfo c) => c.GetParameters().Length == 0));
		if (aggregateSymbol.IsDelegate())
		{
			PopulateSymbolTableWithName(".ctor", null, type);
			PopulateSymbolTableWithName("Invoke", null, type);
		}
		return aggregateSymbol;
	}

	private void SetInterfacesOnAggregate(AggregateSymbol aggregate, Type type)
	{
		if (type.IsGenericType)
		{
			type = type.GetGenericTypeDefinition();
		}
		Type[] interfaces = type.GetInterfaces();
		aggregate.SetIfaces(m_bsymmgr.AllocParams(interfaces.Length, GetCTypeArrayFromTypes(interfaces)));
		aggregate.SetIfacesAll(aggregate.GetIfaces());
	}

	private FieldSymbol AddFieldToSymbolTable(FieldInfo fieldInfo, AggregateSymbol aggregate)
	{
		if (m_symbolTable.LookupSym(GetName(fieldInfo.Name), aggregate, symbmask_t.MASK_FieldSymbol) is FieldSymbol result)
		{
			return result;
		}
		FieldSymbol fieldSymbol = m_symFactory.CreateMemberVar(GetName(fieldInfo.Name), aggregate, null, 0);
		fieldSymbol.AssociatedFieldInfo = fieldInfo;
		fieldSymbol.isStatic = fieldInfo.IsStatic;
		ACCESS access = (fieldInfo.IsPublic ? ACCESS.ACC_PUBLIC : (fieldInfo.IsPrivate ? ACCESS.ACC_PRIVATE : (fieldInfo.IsFamily ? ACCESS.ACC_PROTECTED : ((!fieldInfo.IsAssembly && !fieldInfo.IsFamilyAndAssembly) ? ACCESS.ACC_INTERNALPROTECTED : ACCESS.ACC_INTERNAL))));
		fieldSymbol.SetAccess(access);
		fieldSymbol.isReadOnly = fieldInfo.IsInitOnly;
		fieldSymbol.isEvent = false;
		fieldSymbol.isAssigned = true;
		fieldSymbol.SetType(GetCTypeFromType(fieldInfo.FieldType));
		return fieldSymbol;
	}

	private EventSymbol AddEventToSymbolTable(EventInfo eventInfo, AggregateSymbol aggregate, FieldSymbol addedField)
	{
		if (m_symbolTable.LookupSym(GetName(eventInfo.Name), aggregate, symbmask_t.MASK_EventSymbol) is EventSymbol result)
		{
			return result;
		}
		EventSymbol eventSymbol = m_symFactory.CreateEvent(GetName(eventInfo.Name), aggregate, null);
		eventSymbol.AssociatedEventInfo = eventInfo;
		ACCESS access = ACCESS.ACC_PRIVATE;
		if (eventInfo.GetAddMethod(nonPublic: true) != null)
		{
			eventSymbol.methAdd = AddMethodToSymbolTable(eventInfo.GetAddMethod(nonPublic: true), aggregate, MethodKindEnum.EventAccessor);
			eventSymbol.methAdd.SetEvent(eventSymbol);
			eventSymbol.isOverride = eventSymbol.methAdd.IsOverride();
			access = eventSymbol.methAdd.GetAccess();
		}
		if (eventInfo.GetRemoveMethod(nonPublic: true) != null)
		{
			eventSymbol.methRemove = AddMethodToSymbolTable(eventInfo.GetRemoveMethod(nonPublic: true), aggregate, MethodKindEnum.EventAccessor);
			eventSymbol.methRemove.SetEvent(eventSymbol);
			eventSymbol.isOverride = eventSymbol.methRemove.IsOverride();
			access = eventSymbol.methRemove.GetAccess();
		}
		eventSymbol.isStatic = false;
		eventSymbol.type = GetCTypeFromType(eventInfo.EventHandlerType);
		eventSymbol.SetAccess(access);
		if (eventSymbol.methAdd.RetType.AssociatedSystemType == typeof(EventRegistrationToken) && eventSymbol.methRemove.Params.Item(0).AssociatedSystemType == typeof(EventRegistrationToken))
		{
			eventSymbol.IsWindowsRuntimeEvent = true;
		}
		Type type = typeof(EventRegistrationTokenTable<>).MakeGenericType(eventSymbol.type.AssociatedSystemType);
		if (addedField != null && addedField.GetType() != null && (addedField.GetType() == eventSymbol.type || addedField.GetType().AssociatedSystemType == type))
		{
			addedField.isEvent = true;
		}
		return eventSymbol;
	}

	internal void AddPredefinedPropertyToSymbolTable(AggregateSymbol type, Name property)
	{
		AggregateType thisType = type.getThisType();
		Type associatedSystemType = thisType.AssociatedSystemType;
		IEnumerable<PropertyInfo> enumerable = from x in associatedSystemType.GetProperties()
			where x.Name == property.Text
			select x;
		foreach (PropertyInfo item in enumerable)
		{
			AddPropertyToSymbolTable(item, type);
		}
	}

	private PropertySymbol AddPropertyToSymbolTable(PropertyInfo property, AggregateSymbol aggregate)
	{
		bool flag = property.GetIndexParameters() != null && property.GetIndexParameters().Length != 0;
		Name name = ((!flag) ? GetName(property.Name) : GetName("$Item$"));
		PropertySymbol propertySymbol = m_symbolTable.LookupSym(name, aggregate, symbmask_t.MASK_PropertySymbol) as PropertySymbol;
		if (propertySymbol != null)
		{
			PropertySymbol propertySymbol2 = null;
			while (propertySymbol != null)
			{
				if (propertySymbol.AssociatedPropertyInfo.IsEquivalentTo(property))
				{
					return propertySymbol;
				}
				propertySymbol2 = propertySymbol;
				propertySymbol = m_semanticChecker.SymbolLoader.LookupNextSym(propertySymbol, propertySymbol.parent, symbmask_t.MASK_PropertySymbol).AsPropertySymbol();
			}
			propertySymbol = propertySymbol2;
			if (flag)
			{
				propertySymbol = null;
			}
		}
		if (propertySymbol == null)
		{
			if (flag)
			{
				propertySymbol = m_semanticChecker.GetSymbolLoader().GetGlobalMiscSymFactory().CreateIndexer(name, aggregate, GetName(property.Name), null);
				propertySymbol.Params = CreateParameterArray(null, property.GetIndexParameters());
			}
			else
			{
				propertySymbol = m_symFactory.CreateProperty(GetName(property.Name), aggregate, null);
				propertySymbol.Params = BSYMMGR.EmptyTypeArray();
			}
		}
		propertySymbol.AssociatedPropertyInfo = property;
		propertySymbol.isStatic = ((property.GetGetMethod(nonPublic: true) != null) ? property.GetGetMethod(nonPublic: true).IsStatic : property.GetSetMethod(nonPublic: true).IsStatic);
		propertySymbol.isParamArray = DoesMethodHaveParameterArray(property.GetIndexParameters());
		propertySymbol.swtSlot = null;
		propertySymbol.RetType = GetCTypeFromType(property.PropertyType);
		propertySymbol.isOperator = flag;
		if (property.GetAccessors(nonPublic: true) != null)
		{
			MethodInfo methodInfo = property.GetAccessors(nonPublic: true)[0];
			propertySymbol.isOverride = methodInfo.IsVirtual && methodInfo.IsHideBySig && methodInfo.GetBaseDefinition() != methodInfo;
			propertySymbol.isHideByName = !methodInfo.IsHideBySig;
		}
		SetParameterDataForMethProp(propertySymbol, property.GetIndexParameters());
		MethodInfo getMethod = property.GetGetMethod(nonPublic: true);
		MethodInfo setMethod = property.GetSetMethod(nonPublic: true);
		ACCESS aCCESS = ACCESS.ACC_PRIVATE;
		if (getMethod != null)
		{
			propertySymbol.methGet = AddMethodToSymbolTable(getMethod, aggregate, MethodKindEnum.PropAccessor);
			if (flag || propertySymbol.methGet.Params.size == 0)
			{
				propertySymbol.methGet.SetProperty(propertySymbol);
			}
			else
			{
				propertySymbol.setBogus(isBogus: true);
				propertySymbol.methGet.SetMethKind(MethodKindEnum.Actual);
			}
			if (propertySymbol.methGet.GetAccess() > aCCESS)
			{
				aCCESS = propertySymbol.methGet.GetAccess();
			}
		}
		if (setMethod != null)
		{
			propertySymbol.methSet = AddMethodToSymbolTable(setMethod, aggregate, MethodKindEnum.PropAccessor);
			if (flag || propertySymbol.methSet.Params.size == 1)
			{
				propertySymbol.methSet.SetProperty(propertySymbol);
			}
			else
			{
				propertySymbol.setBogus(isBogus: true);
				propertySymbol.methSet.SetMethKind(MethodKindEnum.Actual);
			}
			if (propertySymbol.methSet.GetAccess() > aCCESS)
			{
				aCCESS = propertySymbol.methSet.GetAccess();
			}
		}
		propertySymbol.SetAccess(aCCESS);
		return propertySymbol;
	}

	internal void AddPredefinedMethodToSymbolTable(AggregateSymbol type, Name methodName)
	{
		Type t = type.getThisType().AssociatedSystemType;
		if (methodName == m_nameManager.GetPredefinedName(PredefinedName.PN_CTOR))
		{
			IEnumerable<ConstructorInfo> enumerable = from m in t.GetConstructors()
				where m.Name == methodName.Text
				select m;
			{
				foreach (ConstructorInfo item in enumerable)
				{
					AddMethodToSymbolTable(item, type, MethodKindEnum.Constructor);
				}
				return;
			}
		}
		IEnumerable<MethodInfo> enumerable2 = from m in t.GetMethods()
			where m.Name == methodName.Text && m.DeclaringType == t
			select m;
		foreach (MethodInfo item2 in enumerable2)
		{
			AddMethodToSymbolTable(item2, type, (item2.Name == "Invoke") ? MethodKindEnum.Invoke : MethodKindEnum.Actual);
		}
	}

	private static bool IsMethodDynamicallyInvokable(MethodBase method)
	{
		if (s_IsInvokableDelegate != null)
		{
			return s_IsInvokableDelegate(method);
		}
		return true;
	}

	private MethodSymbol AddMethodToSymbolTable(MemberInfo member, AggregateSymbol callingAggregate, MethodKindEnum kind)
	{
		MethodInfo methodInfo = member as MethodInfo;
		ConstructorInfo constructorInfo = member as ConstructorInfo;
		if (kind == MethodKindEnum.Actual && (methodInfo == null || (!methodInfo.IsStatic && methodInfo.IsSpecialName)))
		{
			return null;
		}
		MethodSymbol methodSymbol = FindMatchingMethod(member, callingAggregate);
		if (methodSymbol != null)
		{
			return methodSymbol;
		}
		ParameterInfo[] parameters = ((methodInfo != null) ? methodInfo.GetParameters() : constructorInfo.GetParameters());
		methodSymbol = m_symFactory.CreateMethod(GetName(member.Name), callingAggregate, null);
		methodSymbol.AssociatedMemberInfo = member;
		methodSymbol.SetMethKind(kind);
		if (kind == MethodKindEnum.ExplicitConv || kind == MethodKindEnum.ImplicitConv)
		{
			callingAggregate.SetHasConversion();
			methodSymbol.SetConvNext(callingAggregate.GetFirstUDConversion());
			callingAggregate.SetFirstUDConversion(methodSymbol);
		}
		ACCESS access = ((methodInfo != null) ? ((methodInfo.IsPublic && IsMethodDynamicallyInvokable(methodInfo)) ? ACCESS.ACC_PUBLIC : ((methodInfo.IsPrivate || (methodInfo.IsPublic && !IsMethodDynamicallyInvokable(methodInfo))) ? ACCESS.ACC_PRIVATE : (methodInfo.IsFamily ? ACCESS.ACC_PROTECTED : ((!methodInfo.IsAssembly && !methodInfo.IsFamilyAndAssembly) ? ACCESS.ACC_INTERNALPROTECTED : ACCESS.ACC_INTERNAL)))) : ((constructorInfo.IsPublic && IsMethodDynamicallyInvokable(constructorInfo)) ? ACCESS.ACC_PUBLIC : ((constructorInfo.IsPrivate || (constructorInfo.IsPublic && !IsMethodDynamicallyInvokable(constructorInfo))) ? ACCESS.ACC_PRIVATE : (constructorInfo.IsFamily ? ACCESS.ACC_PROTECTED : ((!constructorInfo.IsAssembly && !constructorInfo.IsFamilyAndAssembly) ? ACCESS.ACC_INTERNALPROTECTED : ACCESS.ACC_INTERNAL)))));
		methodSymbol.SetAccess(access);
		methodSymbol.isExtension = false;
		methodSymbol.isExternal = false;
		methodSymbol.MetadataToken = member.MetadataToken;
		if (methodInfo != null)
		{
			methodSymbol.typeVars = GetMethodTypeParameters(methodInfo, methodSymbol);
			methodSymbol.isVirtual = methodInfo.IsVirtual;
			methodSymbol.isAbstract = methodInfo.IsAbstract;
			methodSymbol.isStatic = methodInfo.IsStatic;
			methodSymbol.isOverride = methodInfo.IsVirtual && methodInfo.IsHideBySig && methodInfo.GetBaseDefinition() != methodInfo;
			methodSymbol.isOperator = IsOperator(methodInfo);
			methodSymbol.swtSlot = GetSlotForOverride(methodInfo);
			methodSymbol.isVarargs = (methodInfo.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs;
			methodSymbol.RetType = GetCTypeFromType(methodInfo.ReturnType);
		}
		else
		{
			methodSymbol.typeVars = BSYMMGR.EmptyTypeArray();
			methodSymbol.isVirtual = constructorInfo.IsVirtual;
			methodSymbol.isAbstract = constructorInfo.IsAbstract;
			methodSymbol.isStatic = constructorInfo.IsStatic;
			methodSymbol.isOverride = false;
			methodSymbol.isOperator = false;
			methodSymbol.swtSlot = null;
			methodSymbol.isVarargs = false;
			methodSymbol.RetType = m_typeManager.GetVoid();
		}
		methodSymbol.modOptCount = GetCountOfModOpts(parameters);
		methodSymbol.useMethInstead = false;
		methodSymbol.isParamArray = DoesMethodHaveParameterArray(parameters);
		methodSymbol.isHideByName = false;
		methodSymbol.errExpImpl = null;
		methodSymbol.Params = CreateParameterArray(methodSymbol.AssociatedMemberInfo, parameters);
		methodSymbol.declaration = null;
		SetParameterDataForMethProp(methodSymbol, parameters);
		return methodSymbol;
	}

	private void SetParameterDataForMethProp(MethodOrPropertySymbol methProp, ParameterInfo[] parameters)
	{
		if (parameters.Length == 0)
		{
			return;
		}
		object[] customAttributes = parameters[parameters.Length - 1].GetCustomAttributes(inherit: false);
		if (customAttributes != null)
		{
			object[] array = customAttributes;
			foreach (object obj in array)
			{
				if (obj is ParamArrayAttribute)
				{
					methProp.isParamArray = true;
				}
			}
		}
		for (int j = 0; j < parameters.Length; j++)
		{
			SetParameterAttributes(methProp, parameters, j);
			methProp.ParameterNames.Add(GetName(parameters[j].Name));
		}
	}

	private void SetParameterAttributes(MethodOrPropertySymbol methProp, ParameterInfo[] parameters, int i)
	{
		if ((parameters[i].Attributes & ParameterAttributes.Optional) != 0 && !parameters[i].ParameterType.IsByRef)
		{
			methProp.SetOptionalParameter(i);
			PopulateSymbolTableWithName("Value", new Type[1] { typeof(Missing) }, typeof(Missing));
		}
		object[] customAttributes;
		if ((parameters[i].Attributes & ParameterAttributes.HasFieldMarshal) != 0 && (customAttributes = parameters[i].GetCustomAttributes(typeof(MarshalAsAttribute), inherit: false)) != null && customAttributes.Length != 0)
		{
			MarshalAsAttribute marshalAsAttribute = (MarshalAsAttribute)customAttributes[0];
			methProp.SetMarshalAsParameter(i, marshalAsAttribute.Value);
		}
		if ((customAttributes = parameters[i].GetCustomAttributes(typeof(IUnknownConstantAttribute), inherit: false)) != null && customAttributes.Length != 0)
		{
			methProp.SetUnknownConstantParameter(i);
		}
		if ((customAttributes = parameters[i].GetCustomAttributes(typeof(IDispatchConstantAttribute), inherit: false)) != null && customAttributes.Length != 0)
		{
			methProp.SetDispatchConstantParameter(i);
		}
		if ((customAttributes = parameters[i].GetCustomAttributes(typeof(DateTimeConstantAttribute), inherit: false)) != null && customAttributes.Length != 0)
		{
			DateTimeConstantAttribute dateTimeConstantAttribute = (DateTimeConstantAttribute)customAttributes[0];
			ConstValFactory constValFactory = new ConstValFactory();
			CONSTVAL cv = constValFactory.Create(((DateTime)dateTimeConstantAttribute.Value).Ticks);
			CType reqPredefType = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_DATETIME);
			methProp.SetDefaultParameterValue(i, reqPredefType, cv);
		}
		else if ((customAttributes = parameters[i].GetCustomAttributes(typeof(DecimalConstantAttribute), inherit: false)) != null && customAttributes.Length != 0)
		{
			DecimalConstantAttribute decimalConstantAttribute = (DecimalConstantAttribute)customAttributes[0];
			ConstValFactory constValFactory2 = new ConstValFactory();
			CONSTVAL cv2 = constValFactory2.Create(decimalConstantAttribute.Value);
			CType optPredefType = m_semanticChecker.GetSymbolLoader().GetOptPredefType(PredefinedType.PT_DECIMAL);
			methProp.SetDefaultParameterValue(i, optPredefType, cv2);
		}
		else
		{
			if ((parameters[i].Attributes & ParameterAttributes.HasDefault) == 0 || parameters[i].ParameterType.IsByRef)
			{
				return;
			}
			ConstValFactory constValFactory3 = new ConstValFactory();
			CONSTVAL nullRef = (nullRef = ConstValFactory.GetNullRef());
			CType reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_OBJECT);
			if (parameters[i].RawDefaultValue != null)
			{
				object rawDefaultValue = parameters[i].RawDefaultValue;
				Type type = rawDefaultValue.GetType();
				if (type == typeof(byte))
				{
					nullRef = constValFactory3.Create((byte)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_BYTE);
				}
				else if (type == typeof(short))
				{
					nullRef = constValFactory3.Create((short)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_SHORT);
				}
				else if (type == typeof(int))
				{
					nullRef = constValFactory3.Create((int)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_INT);
				}
				else if (type == typeof(long))
				{
					nullRef = constValFactory3.Create((long)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_LONG);
				}
				else if (type == typeof(float))
				{
					nullRef = constValFactory3.Create((float)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_FLOAT);
				}
				else if (type == typeof(double))
				{
					nullRef = constValFactory3.Create((double)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_DOUBLE);
				}
				else if (type == typeof(decimal))
				{
					nullRef = constValFactory3.Create((decimal)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_DECIMAL);
				}
				else if (type == typeof(char))
				{
					nullRef = constValFactory3.Create((char)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_CHAR);
				}
				else if (type == typeof(bool))
				{
					nullRef = constValFactory3.Create((bool)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_BOOL);
				}
				else if (type == typeof(sbyte))
				{
					nullRef = constValFactory3.Create((sbyte)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_SBYTE);
				}
				else if (type == typeof(ushort))
				{
					nullRef = constValFactory3.Create((ushort)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_USHORT);
				}
				else if (type == typeof(uint))
				{
					nullRef = constValFactory3.Create((uint)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_UINT);
				}
				else if (type == typeof(ulong))
				{
					nullRef = constValFactory3.Create((ulong)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_ULONG);
				}
				else if (type == typeof(string))
				{
					nullRef = constValFactory3.Create((string)rawDefaultValue);
					reqPredefType2 = m_semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_STRING);
				}
			}
			methProp.SetDefaultParameterValue(i, reqPredefType2, nullRef);
		}
	}

	private MethodSymbol FindMatchingMethod(MemberInfo method, AggregateSymbol callingAggregate)
	{
		for (MethodSymbol methodSymbol = m_bsymmgr.LookupAggMember(GetName(method.Name), callingAggregate, symbmask_t.MASK_MethodSymbol).AsMethodSymbol(); methodSymbol != null; methodSymbol = BSYMMGR.LookupNextSym(methodSymbol, callingAggregate, symbmask_t.MASK_MethodSymbol).AsMethodSymbol())
		{
			if (methodSymbol.AssociatedMemberInfo.IsEquivalentTo(method))
			{
				return methodSymbol;
			}
		}
		return null;
	}

	private uint GetCountOfModOpts(ParameterInfo[] parameters)
	{
		uint num = 0u;
		foreach (ParameterInfo parameterInfo in parameters)
		{
			if (parameterInfo.GetOptionalCustomModifiers() != null)
			{
				num += (uint)parameterInfo.GetOptionalCustomModifiers().Length;
			}
		}
		return num;
	}

	private TypeArray CreateParameterArray(MemberInfo associatedInfo, ParameterInfo[] parameters)
	{
		List<CType> list = new List<CType>();
		foreach (ParameterInfo p in parameters)
		{
			list.Add(GetTypeOfParameter(p, associatedInfo));
		}
		MethodInfo methodInfo = associatedInfo as MethodInfo;
		if (methodInfo != null && (methodInfo.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			list.Add(m_typeManager.GetArgListType());
		}
		return m_bsymmgr.AllocParams(list.Count, list.ToArray());
	}

	private CType GetTypeOfParameter(ParameterInfo p, MemberInfo m)
	{
		Type parameterType = p.ParameterType;
		CType cType = ((!parameterType.IsGenericParameter || !(parameterType.DeclaringMethod != null) || !(parameterType.DeclaringMethod == m)) ? GetCTypeFromType(parameterType) : LoadMethodTypeParameter(FindMethodFromMemberInfo(m), parameterType));
		if (cType.IsParameterModifierType() && p.IsOut && !p.IsIn)
		{
			CType parameterType2 = cType.AsParameterModifierType().GetParameterType();
			cType = m_typeManager.GetParameterModifier(parameterType2, isOut: true);
		}
		return cType;
	}

	private bool DoesMethodHaveParameterArray(ParameterInfo[] parameters)
	{
		if (parameters.Length == 0)
		{
			return false;
		}
		ParameterInfo parameterInfo = parameters[parameters.Length - 1];
		object[] customAttributes = parameterInfo.GetCustomAttributes(inherit: false);
		object[] array = customAttributes;
		foreach (object obj in array)
		{
			if (obj is ParamArrayAttribute)
			{
				return true;
			}
		}
		return false;
	}

	private SymWithType GetSlotForOverride(MethodInfo method)
	{
		if (method.IsVirtual && method.IsHideBySig)
		{
			MethodInfo baseDefinition = method.GetBaseDefinition();
			if (baseDefinition == method)
			{
				return null;
			}
			AggregateSymbol aggregate = GetCTypeFromType(baseDefinition.DeclaringType).getAggregate();
			MethodSymbol sym = FindMethodFromMemberInfo(baseDefinition);
			return new SymWithType(sym, aggregate.getThisType());
		}
		return null;
	}

	private MethodSymbol FindMethodFromMemberInfo(MemberInfo baseMemberInfo)
	{
		CType cTypeFromType = GetCTypeFromType(baseMemberInfo.DeclaringType);
		AggregateSymbol aggregate = cTypeFromType.getAggregate();
		MethodSymbol methodSymbol = m_semanticChecker.SymbolLoader.LookupAggMember(GetName(baseMemberInfo.Name), aggregate, symbmask_t.MASK_MethodSymbol).AsMethodSymbol();
		while (methodSymbol != null && !methodSymbol.AssociatedMemberInfo.IsEquivalentTo(baseMemberInfo))
		{
			methodSymbol = m_semanticChecker.SymbolLoader.LookupNextSym(methodSymbol, aggregate, symbmask_t.MASK_MethodSymbol).AsMethodSymbol();
		}
		return methodSymbol;
	}

	internal bool AggregateContainsMethod(AggregateSymbol agg, string szName, symbmask_t mask)
	{
		return m_semanticChecker.SymbolLoader.LookupAggMember(GetName(szName), agg, mask) != null;
	}

	internal void AddConversionsForType(Type type)
	{
		Type type2 = type;
		while (type2.BaseType != null)
		{
			AddConversionsForOneType(type2);
			type2 = type2.BaseType;
		}
	}

	private void AddConversionsForOneType(Type type)
	{
		if (type.IsGenericType)
		{
			type = type.GetGenericTypeDefinition();
		}
		if (m_typesWithConversionsLoaded.Contains(type))
		{
			return;
		}
		m_typesWithConversionsLoaded.Add(type);
		CType cType = GetCTypeFromType(type);
		if (!cType.IsAggregateType())
		{
			CType baseOrParameterOrElementType;
			while ((baseOrParameterOrElementType = cType.GetBaseOrParameterOrElementType()) != null)
			{
				cType = baseOrParameterOrElementType;
			}
		}
		if (cType.IsTypeParameterType())
		{
			CType[] array = cType.AsTypeParameterType().GetBounds().ToArray();
			foreach (CType cType2 in array)
			{
				AddConversionsForType(cType2.AssociatedSystemType);
			}
			return;
		}
		AggregateSymbol aggregate = cType.AsAggregateType().getAggregate();
		IEnumerable<MethodInfo> enumerable = from conversion in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
			where (conversion.Name == "op_Implicit" || conversion.Name == "op_Explicit") && conversion.DeclaringType == type && conversion.IsSpecialName && !conversion.IsGenericMethod
			select conversion;
		foreach (MethodInfo item in enumerable)
		{
			MethodSymbol methodSymbol = AddMethodToSymbolTable(item, aggregate, (item.Name == "op_Implicit") ? MethodKindEnum.ImplicitConv : MethodKindEnum.ExplicitConv);
		}
	}

	private bool IsOperator(MethodInfo method)
	{
		if (method.IsSpecialName && method.IsStatic)
		{
			if (!(method.Name == "op_Implicit") && !(method.Name == "op_Explicit") && !(method.Name == "op_Addition") && !(method.Name == "op_Subtraction") && !(method.Name == "op_Multiply") && !(method.Name == "op_Division") && !(method.Name == "op_Modulus") && !(method.Name == "op_LeftShift") && !(method.Name == "op_RightShift") && !(method.Name == "op_LessThan") && !(method.Name == "op_GreaterThan") && !(method.Name == "op_LessThanOrEqual") && !(method.Name == "op_GreaterThanOrEqual") && !(method.Name == "op_Equality") && !(method.Name == "op_Inequality") && !(method.Name == "op_BitwiseAnd") && !(method.Name == "op_ExclusiveOr") && !(method.Name == "op_BitwiseOr") && !(method.Name == "op_LogicalNot") && !(method.Name == "op_Addition") && !(method.Name == "op_Subtraction") && !(method.Name == "op_Multiply") && !(method.Name == "op_Division") && !(method.Name == "op_Modulus") && !(method.Name == "op_BitwiseAnd") && !(method.Name == "op_ExclusiveOr") && !(method.Name == "op_BitwiseOr") && !(method.Name == "op_LeftShift") && !(method.Name == "op_RightShift") && !(method.Name == "op_UnaryNegation") && !(method.Name == "op_UnaryPlus") && !(method.Name == "op_OnesComplement") && !(method.Name == "op_True") && !(method.Name == "op_False") && !(method.Name == "op_Increment") && !(method.Name == "op_Increment") && !(method.Name == "op_Decrement"))
			{
				return method.Name == "op_Decrement";
			}
			return true;
		}
		return false;
	}
}
