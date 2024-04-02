using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class BSYMMGR
{
	protected struct TypeArrayKey : IEquatable<TypeArrayKey>
	{
		private CType[] types;

		private int hashCode;

		public TypeArrayKey(CType[] types)
		{
			this.types = types;
			hashCode = 0;
			int i = 0;
			for (int num = types.Length; i < num; i++)
			{
				hashCode ^= types[i].GetHashCode();
			}
		}

		public bool Equals(TypeArrayKey other)
		{
			if (other.types == types)
			{
				return true;
			}
			if (other.types.Length != types.Length)
			{
				return false;
			}
			if (other.hashCode != hashCode)
			{
				return false;
			}
			int i = 0;
			for (int num = types.Length; i < num; i++)
			{
				if (!types[i].Equals(other.types[i]))
				{
					return false;
				}
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj is TypeArrayKey)
			{
				return Equals((TypeArrayKey)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return hashCode;
		}
	}

	internal HashSet<KAID> bsetGlobalAssemblies;

	public PropertySymbol propNubValue;

	public MethodSymbol methNubCtor;

	private SymFactory symFactory;

	private MiscSymFactory miscSymFactory;

	private NamespaceSymbol rootNS;

	protected List<AidContainer> ssetAssembly;

	protected NameManager m_nameTable;

	protected SYMTBL tableGlobal;

	protected Dictionary<TypeArrayKey, TypeArray> tableTypeArrays;

	private InputFile m_infileUnres;

	private const int LOG2_SYMTBL_INITIAL_BUCKET_CNT = 13;

	private static readonly TypeArray taEmpty = new TypeArray(new CType[0]);

	public BSYMMGR(NameManager nameMgr, TypeManager typeManager)
	{
		m_nameTable = nameMgr;
		tableGlobal = new SYMTBL();
		symFactory = new SymFactory(tableGlobal, m_nameTable);
		miscSymFactory = new MiscSymFactory(tableGlobal);
		ssetAssembly = new List<AidContainer>();
		m_infileUnres = new InputFile();
		m_infileUnres.isSource = false;
		m_infileUnres.SetAssemblyID(KAID.kaidUnresolved);
		ssetAssembly.Add(new AidContainer(m_infileUnres));
		bsetGlobalAssemblies = new HashSet<KAID>();
		bsetGlobalAssemblies.Add(KAID.kaidThisAssembly);
		tableTypeArrays = new Dictionary<TypeArrayKey, TypeArray>();
		rootNS = symFactory.CreateNamespace(m_nameTable.Add(""), null);
		GetNsAid(rootNS, KAID.kaidGlobal);
	}

	public void Init()
	{
		InitPreLoad();
	}

	public NameManager GetNameManager()
	{
		return m_nameTable;
	}

	public SYMTBL GetSymbolTable()
	{
		return tableGlobal;
	}

	public static TypeArray EmptyTypeArray()
	{
		return taEmpty;
	}

	public AssemblyQualifiedNamespaceSymbol GetRootNsAid(KAID aid)
	{
		return GetNsAid(rootNS, aid);
	}

	public NamespaceSymbol GetRootNS()
	{
		return rootNS;
	}

	public KAID AidAlloc(InputFile sym)
	{
		ssetAssembly.Add(new AidContainer(sym));
		return (KAID)(ssetAssembly.Count - 1 + 3);
	}

	public BetterType CompareTypes(TypeArray ta1, TypeArray ta2)
	{
		if (ta1 == ta2)
		{
			return BetterType.Same;
		}
		if (ta1.Size != ta2.Size)
		{
			if (ta1.Size <= ta2.Size)
			{
				return BetterType.Right;
			}
			return BetterType.Left;
		}
		BetterType betterType = BetterType.Neither;
		for (int i = 0; i < ta1.Size; i++)
		{
			CType cType = ta1.Item(i);
			CType cType2 = ta2.Item(i);
			BetterType betterType2 = BetterType.Neither;
			while (true)
			{
				if (cType.GetTypeKind() != cType2.GetTypeKind())
				{
					if (cType.IsTypeParameterType())
					{
						betterType2 = BetterType.Right;
					}
					else if (cType2.IsTypeParameterType())
					{
						betterType2 = BetterType.Left;
					}
				}
				else
				{
					switch (cType.GetTypeKind())
					{
					case TypeKind.TK_ArrayType:
					case TypeKind.TK_PointerType:
					case TypeKind.TK_ParameterModifierType:
					case TypeKind.TK_NullableType:
						goto IL_00a1;
					case TypeKind.TK_AggregateType:
						betterType2 = CompareTypes(cType.AsAggregateType().GetTypeArgsAll(), cType2.AsAggregateType().GetTypeArgsAll());
						break;
					}
				}
				break;
				IL_00a1:
				cType = cType.GetBaseOrParameterOrElementType();
				cType2 = cType2.GetBaseOrParameterOrElementType();
			}
			if (betterType2 == BetterType.Right || betterType2 == BetterType.Left)
			{
				if (betterType == BetterType.Same || betterType == BetterType.Neither)
				{
					betterType = betterType2;
				}
				else if (betterType2 != betterType)
				{
					return BetterType.Neither;
				}
			}
		}
		return betterType;
	}

	public SymFactory GetSymFactory()
	{
		return symFactory;
	}

	public MiscSymFactory GetMiscSymFactory()
	{
		return miscSymFactory;
	}

	private void InitPreLoad()
	{
		for (int i = 0; i < 138; i++)
		{
			NamespaceSymbol parent = GetRootNS();
			string name = PredefinedTypeFacts.GetName((PredefinedType)i);
			string text;
			for (int j = 0; j < name.Length; j += text.Length + 1)
			{
				int num = name.IndexOf('.', j);
				if (num == -1)
				{
					break;
				}
				text = ((num > j) ? name.Substring(j, num - j) : name.Substring(j));
				Name name2 = GetNameManager().Add(text);
				NamespaceSymbol namespaceSymbol = LookupGlobalSymCore(name2, parent, symbmask_t.MASK_NamespaceSymbol).AsNamespaceSymbol();
				parent = ((namespaceSymbol != null) ? namespaceSymbol : symFactory.CreateNamespace(name2, parent));
			}
		}
	}

	public Symbol LookupGlobalSymCore(Name name, ParentSymbol parent, symbmask_t kindmask)
	{
		return tableGlobal.LookupSym(name, parent, kindmask);
	}

	public Symbol LookupAggMember(Name name, AggregateSymbol agg, symbmask_t mask)
	{
		return tableGlobal.LookupSym(name, agg, mask);
	}

	public static Symbol LookupNextSym(Symbol sym, ParentSymbol parent, symbmask_t kindmask)
	{
		for (sym = sym.nextSameName; sym != null; sym = sym.nextSameName)
		{
			if ((kindmask & sym.mask()) > ~symbmask_t.MASK_ALL)
			{
				return sym;
			}
		}
		return null;
	}

	public Name GetNameFromPtrs(object u1, object u2)
	{
		if (u2 != null)
		{
			return m_nameTable.Add(string.Format(CultureInfo.InvariantCulture, "{0:X}-{1:X}", new object[2]
			{
				u1.GetHashCode(),
				u2.GetHashCode()
			}));
		}
		return m_nameTable.Add(string.Format(CultureInfo.InvariantCulture, "{0:X}", new object[1] { u1.GetHashCode() }));
	}

	public AssemblyQualifiedNamespaceSymbol GetNsAid(NamespaceSymbol ns, KAID aid)
	{
		Name nameFromPtrs = GetNameFromPtrs(aid, 0);
		AssemblyQualifiedNamespaceSymbol assemblyQualifiedNamespaceSymbol = LookupGlobalSymCore(nameFromPtrs, ns, symbmask_t.MASK_AssemblyQualifiedNamespaceSymbol).AsAssemblyQualifiedNamespaceSymbol();
		if (assemblyQualifiedNamespaceSymbol == null)
		{
			assemblyQualifiedNamespaceSymbol = symFactory.CreateNamespaceAid(nameFromPtrs, ns, aid);
		}
		return assemblyQualifiedNamespaceSymbol;
	}

	public TypeArray AllocParams(int ctype, CType[] prgtype)
	{
		if (ctype == 0)
		{
			return taEmpty;
		}
		return AllocParams(prgtype);
	}

	public TypeArray AllocParams(int ctype, TypeArray array, int offset)
	{
		CType[] sourceArray = array.ToArray();
		CType[] array2 = new CType[ctype];
		Array.ConstrainedCopy(sourceArray, offset, array2, 0, ctype);
		return AllocParams(array2);
	}

	public TypeArray AllocParams(params CType[] types)
	{
		if (types == null || types.Length == 0)
		{
			return taEmpty;
		}
		TypeArrayKey key = new TypeArrayKey(types);
		if (!tableTypeArrays.TryGetValue(key, out var value))
		{
			value = new TypeArray(types);
			tableTypeArrays.Add(key, value);
		}
		return value;
	}

	public TypeArray ConcatParams(CType[] prgtype1, CType[] prgtype2)
	{
		CType[] array = new CType[prgtype1.Length + prgtype2.Length];
		Array.Copy(prgtype1, array, prgtype1.Length);
		Array.Copy(prgtype2, 0, array, prgtype1.Length, prgtype2.Length);
		return AllocParams(array);
	}

	public TypeArray ConcatParams(TypeArray pta1, TypeArray pta2)
	{
		return ConcatParams(pta1.ToArray(), pta2.ToArray());
	}
}
