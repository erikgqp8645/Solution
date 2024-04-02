using System;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class AggregateSymbol : NamespaceOrAggregateSymbol
{
	public Type AssociatedSystemType;

	public Assembly AssociatedAssembly;

	private InputFile infile;

	private AggregateType atsInst;

	private AggregateType m_pBaseClass;

	private AggregateType m_pUnderlyingType;

	private TypeArray m_ifaces;

	private TypeArray m_ifacesAll;

	private TypeArray m_typeVarsThis;

	private TypeArray m_typeVarsAll;

	private TypeManager m_pTypeManager;

	private MethodSymbol m_pConvFirst;

	private AggKindEnum aggKind;

	private bool m_isLayoutError;

	private bool m_isSource;

	private bool m_isPredefined;

	private PredefinedType m_iPredef;

	private bool m_isAbstract;

	private bool m_isSealed;

	private bool m_isUnmanagedStruct;

	private bool m_isManagedStruct;

	private bool m_hasPubNoArgCtor;

	private bool m_hasExternReference;

	private bool m_isSkipUDOps;

	private bool m_isComImport;

	private bool isAnonymousType;

	private bool? m_hasConversion;

	public NamespaceOrAggregateSymbol Parent => parent.AsNamespaceOrAggregateSymbol();

	public AggregateSymbol GetBaseAgg()
	{
		if (m_pBaseClass != null)
		{
			return m_pBaseClass.getAggregate();
		}
		return null;
	}

	public AggregateType getThisType()
	{
		if (atsInst == null)
		{
			AggregateType atsOuter = (isNested() ? GetOuterAgg().getThisType() : null);
			atsInst = m_pTypeManager.GetAggregate(this, atsOuter, GetTypeVars());
		}
		return atsInst;
	}

	public void InitFromInfile(InputFile infile)
	{
		this.infile = infile;
		m_isSource = infile.isSource;
	}

	public bool FindBaseAgg(AggregateSymbol agg)
	{
		for (AggregateSymbol aggregateSymbol = this; aggregateSymbol != null; aggregateSymbol = aggregateSymbol.GetBaseAgg())
		{
			if (aggregateSymbol == agg)
			{
				return true;
			}
		}
		return false;
	}

	public new AggregateDeclaration DeclFirst()
	{
		return (AggregateDeclaration)base.DeclFirst();
	}

	public AggregateDeclaration DeclOnly()
	{
		return DeclFirst();
	}

	public bool InAlias(KAID aid)
	{
		if (aid < KAID.kaidMinModule)
		{
			return infile.InAlias(aid);
		}
		return aid == GetModuleID();
	}

	public KAID GetModuleID()
	{
		return KAID.kaidGlobal;
	}

	public KAID GetAssemblyID()
	{
		return infile.GetAssemblyID();
	}

	public bool IsUnresolved()
	{
		if (infile != null)
		{
			return infile.GetAssemblyID() == KAID.kaidUnresolved;
		}
		return false;
	}

	public bool isNested()
	{
		if (parent != null)
		{
			return parent.IsAggregateSymbol();
		}
		return false;
	}

	public AggregateSymbol GetOuterAgg()
	{
		if (parent == null || !parent.IsAggregateSymbol())
		{
			return null;
		}
		return parent.AsAggregateSymbol();
	}

	public bool isPredefAgg(PredefinedType pt)
	{
		if (m_isPredefined)
		{
			return m_iPredef == pt;
		}
		return false;
	}

	public AggKindEnum AggKind()
	{
		return aggKind;
	}

	public void SetAggKind(AggKindEnum aggKind)
	{
		this.aggKind = aggKind;
		if (aggKind == AggKindEnum.Interface)
		{
			SetAbstract(@abstract: true);
		}
	}

	public bool IsClass()
	{
		return AggKind() == AggKindEnum.Class;
	}

	public bool IsDelegate()
	{
		return AggKind() == AggKindEnum.Delegate;
	}

	public bool IsInterface()
	{
		return AggKind() == AggKindEnum.Interface;
	}

	public bool IsStruct()
	{
		return AggKind() == AggKindEnum.Struct;
	}

	public bool IsEnum()
	{
		return AggKind() == AggKindEnum.Enum;
	}

	public bool IsValueType()
	{
		if (AggKind() != AggKindEnum.Struct)
		{
			return AggKind() == AggKindEnum.Enum;
		}
		return true;
	}

	public bool IsRefType()
	{
		if (AggKind() != AggKindEnum.Class && AggKind() != AggKindEnum.Interface)
		{
			return AggKind() == AggKindEnum.Delegate;
		}
		return true;
	}

	public bool IsStatic()
	{
		if (m_isAbstract)
		{
			return m_isSealed;
		}
		return false;
	}

	public bool IsAnonymousType()
	{
		return isAnonymousType;
	}

	public void SetAnonymousType(bool isAnonymousType)
	{
		this.isAnonymousType = isAnonymousType;
	}

	public bool IsAbstract()
	{
		return m_isAbstract;
	}

	public void SetAbstract(bool @abstract)
	{
		m_isAbstract = @abstract;
	}

	public bool IsPredefined()
	{
		return m_isPredefined;
	}

	public void SetPredefined(bool predefined)
	{
		m_isPredefined = predefined;
	}

	public PredefinedType GetPredefType()
	{
		return m_iPredef;
	}

	public void SetPredefType(PredefinedType predef)
	{
		m_iPredef = predef;
	}

	public bool IsLayoutError()
	{
		return m_isLayoutError;
	}

	public void SetLayoutError(bool layoutError)
	{
		m_isLayoutError = layoutError;
	}

	public bool IsSealed()
	{
		return m_isSealed;
	}

	public void SetSealed(bool @sealed)
	{
		m_isSealed = @sealed;
	}

	public bool HasConversion(SymbolLoader pLoader)
	{
		pLoader.RuntimeBinderSymbolTable.AddConversionsForType(AssociatedSystemType);
		if (!m_hasConversion.HasValue)
		{
			m_hasConversion = GetBaseAgg() != null && GetBaseAgg().HasConversion(pLoader);
		}
		return m_hasConversion.Value;
	}

	public void SetHasConversion()
	{
		m_hasConversion = true;
	}

	public bool IsUnmanagedStruct()
	{
		return m_isUnmanagedStruct;
	}

	public void SetUnmanagedStruct(bool unmanagedStruct)
	{
		m_isUnmanagedStruct = unmanagedStruct;
	}

	public bool IsManagedStruct()
	{
		return m_isManagedStruct;
	}

	public void SetManagedStruct(bool managedStruct)
	{
		m_isManagedStruct = managedStruct;
	}

	public bool IsKnownManagedStructStatus()
	{
		if (!IsManagedStruct())
		{
			return IsUnmanagedStruct();
		}
		return true;
	}

	public bool HasPubNoArgCtor()
	{
		return m_hasPubNoArgCtor;
	}

	public void SetHasPubNoArgCtor(bool hasPubNoArgCtor)
	{
		m_hasPubNoArgCtor = hasPubNoArgCtor;
	}

	public bool HasExternReference()
	{
		return m_hasExternReference;
	}

	public void SetHasExternReference(bool hasExternReference)
	{
		m_hasExternReference = hasExternReference;
	}

	public bool IsSkipUDOps()
	{
		return m_isSkipUDOps;
	}

	public void SetSkipUDOps(bool skipUDOps)
	{
		m_isSkipUDOps = skipUDOps;
	}

	public void SetComImport(bool comImport)
	{
		m_isComImport = comImport;
	}

	public bool IsSource()
	{
		return m_isSource;
	}

	public TypeArray GetTypeVars()
	{
		return m_typeVarsThis;
	}

	public void SetTypeVars(TypeArray typeVars)
	{
		if (typeVars == null)
		{
			m_typeVarsThis = null;
			m_typeVarsAll = null;
		}
		else
		{
			TypeArray pTypeArray = ((GetOuterAgg() == null) ? BSYMMGR.EmptyTypeArray() : GetOuterAgg().GetTypeVarsAll());
			m_typeVarsThis = typeVars;
			m_typeVarsAll = m_pTypeManager.ConcatenateTypeArrays(pTypeArray, typeVars);
		}
	}

	public TypeArray GetTypeVarsAll()
	{
		return m_typeVarsAll;
	}

	public AggregateType GetBaseClass()
	{
		return m_pBaseClass;
	}

	public void SetBaseClass(AggregateType baseClass)
	{
		m_pBaseClass = baseClass;
	}

	public AggregateType GetUnderlyingType()
	{
		return m_pUnderlyingType;
	}

	public void SetUnderlyingType(AggregateType underlyingType)
	{
		m_pUnderlyingType = underlyingType;
	}

	public TypeArray GetIfaces()
	{
		return m_ifaces;
	}

	public void SetIfaces(TypeArray ifaces)
	{
		m_ifaces = ifaces;
	}

	public TypeArray GetIfacesAll()
	{
		return m_ifacesAll;
	}

	public void SetIfacesAll(TypeArray ifacesAll)
	{
		m_ifacesAll = ifacesAll;
	}

	public TypeManager GetTypeManager()
	{
		return m_pTypeManager;
	}

	public void SetTypeManager(TypeManager typeManager)
	{
		m_pTypeManager = typeManager;
	}

	public MethodSymbol GetFirstUDConversion()
	{
		return m_pConvFirst;
	}

	public void SetFirstUDConversion(MethodSymbol conv)
	{
		m_pConvFirst = conv;
	}

	public new bool InternalsVisibleTo(Assembly assembly)
	{
		return m_pTypeManager.InternalsVisibleTo(AssociatedAssembly, assembly);
	}
}
