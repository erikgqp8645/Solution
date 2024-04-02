using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class Symbol
{
	private SYMKIND kind;

	private bool isBogus;

	private bool checkedBogus;

	private ACCESS access;

	public Name name;

	public ParentSymbol parent;

	public Symbol nextChild;

	public Symbol nextSameName;

	public bool isStatic
	{
		get
		{
			bool result = false;
			if (IsFieldSymbol())
			{
				result = this.AsFieldSymbol().isStatic;
			}
			else if (IsEventSymbol())
			{
				result = this.AsEventSymbol().isStatic;
			}
			else if (IsMethodOrPropertySymbol())
			{
				result = this.AsMethodOrPropertySymbol().isStatic;
			}
			else if (IsAggregateSymbol())
			{
				result = true;
			}
			return result;
		}
	}

	public ACCESS GetAccess()
	{
		return access;
	}

	public void SetAccess(ACCESS access)
	{
		this.access = access;
	}

	public SYMKIND getKind()
	{
		return kind;
	}

	public void setKind(SYMKIND kind)
	{
		this.kind = kind;
	}

	public symbmask_t mask()
	{
		return (symbmask_t)(1 << (int)kind);
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

	public void initBogus()
	{
		isBogus = false;
		checkedBogus = false;
	}

	public bool computeCurrentBogusState()
	{
		if (hasBogus())
		{
			return checkBogus();
		}
		bool flag = false;
		switch (getKind())
		{
		case SYMKIND.SK_MethodSymbol:
		case SYMKIND.SK_PropertySymbol:
		{
			MethodOrPropertySymbol methodOrPropertySymbol = this.AsMethodOrPropertySymbol();
			if (methodOrPropertySymbol.RetType != null)
			{
				flag = methodOrPropertySymbol.RetType.computeCurrentBogusState();
			}
			if (methodOrPropertySymbol.Params != null)
			{
				int num = 0;
				while (!flag && num < methodOrPropertySymbol.Params.Size)
				{
					flag |= methodOrPropertySymbol.Params.Item(num).computeCurrentBogusState();
					num++;
				}
			}
			break;
		}
		case SYMKIND.SK_EventSymbol:
			if (this.AsEventSymbol().type != null)
			{
				flag = this.AsEventSymbol().type.computeCurrentBogusState();
			}
			break;
		case SYMKIND.SK_FieldSymbol:
			if (this.AsFieldSymbol().GetType() != null)
			{
				flag = this.AsFieldSymbol().GetType().computeCurrentBogusState();
			}
			break;
		case SYMKIND.SK_TypeParameterSymbol:
		case SYMKIND.SK_LocalVariableSymbol:
			setBogus(isBogus: false);
			break;
		case SYMKIND.SK_AggregateSymbol:
			flag = hasBogus() && checkBogus();
			break;
		default:
			setBogus(isBogus: false);
			break;
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

	public bool IsNamespaceSymbol()
	{
		return kind == SYMKIND.SK_NamespaceSymbol;
	}

	public bool IsNamespaceDeclaration()
	{
		return kind == SYMKIND.SK_NamespaceDeclaration;
	}

	public bool IsAggregateSymbol()
	{
		return kind == SYMKIND.SK_AggregateSymbol;
	}

	public bool IsAggregateDeclaration()
	{
		return kind == SYMKIND.SK_AggregateDeclaration;
	}

	public bool IsFieldSymbol()
	{
		return kind == SYMKIND.SK_FieldSymbol;
	}

	public bool IsLocalVariableSymbol()
	{
		return kind == SYMKIND.SK_LocalVariableSymbol;
	}

	public bool IsMethodSymbol()
	{
		return kind == SYMKIND.SK_MethodSymbol;
	}

	public bool IsPropertySymbol()
	{
		return kind == SYMKIND.SK_PropertySymbol;
	}

	public bool IsTypeParameterSymbol()
	{
		return kind == SYMKIND.SK_TypeParameterSymbol;
	}

	public bool IsEventSymbol()
	{
		return kind == SYMKIND.SK_EventSymbol;
	}

	public bool IsMethodOrPropertySymbol()
	{
		if (!IsMethodSymbol())
		{
			return IsPropertySymbol();
		}
		return true;
	}

	public bool IsFMETHSYM()
	{
		return IsMethodSymbol();
	}

	public CType getType()
	{
		CType result = null;
		if (IsMethodOrPropertySymbol())
		{
			result = this.AsMethodOrPropertySymbol().RetType;
		}
		else if (IsFieldSymbol())
		{
			result = this.AsFieldSymbol().GetType();
		}
		else if (IsEventSymbol())
		{
			result = this.AsEventSymbol().type;
		}
		return result;
	}

	public Assembly GetAssembly()
	{
		switch (kind)
		{
		case SYMKIND.SK_TypeParameterSymbol:
		case SYMKIND.SK_FieldSymbol:
		case SYMKIND.SK_MethodSymbol:
		case SYMKIND.SK_PropertySymbol:
		case SYMKIND.SK_EventSymbol:
			return parent.AsAggregateSymbol().AssociatedAssembly;
		case SYMKIND.SK_AggregateDeclaration:
			return this.AsAggregateDeclaration().GetAssembly();
		case SYMKIND.SK_AggregateSymbol:
			return this.AsAggregateSymbol().AssociatedAssembly;
		default:
			return null;
		}
	}

	public bool InternalsVisibleTo(Assembly assembly)
	{
		switch (kind)
		{
		case SYMKIND.SK_TypeParameterSymbol:
		case SYMKIND.SK_FieldSymbol:
		case SYMKIND.SK_MethodSymbol:
		case SYMKIND.SK_PropertySymbol:
		case SYMKIND.SK_EventSymbol:
			return parent.AsAggregateSymbol().InternalsVisibleTo(assembly);
		case SYMKIND.SK_AggregateDeclaration:
			return this.AsAggregateDeclaration().Agg().InternalsVisibleTo(assembly);
		case SYMKIND.SK_AggregateSymbol:
			return this.AsAggregateSymbol().InternalsVisibleTo(assembly);
		default:
			return false;
		}
	}

	public bool SameAssemOrFriend(Symbol sym)
	{
		Assembly assembly = GetAssembly();
		if (!(assembly == sym.GetAssembly()))
		{
			return sym.InternalsVisibleTo(assembly);
		}
		return true;
	}

	public InputFile getInputFile()
	{
		switch (kind)
		{
		case SYMKIND.SK_NamespaceSymbol:
		case SYMKIND.SK_AssemblyQualifiedNamespaceSymbol:
			return null;
		case SYMKIND.SK_NamespaceDeclaration:
			return null;
		case SYMKIND.SK_AggregateSymbol:
		{
			AggregateSymbol aggregateSymbol = this.AsAggregateSymbol();
			if (!aggregateSymbol.IsSource())
			{
				return aggregateSymbol.DeclOnly().getInputFile();
			}
			return null;
		}
		case SYMKIND.SK_AggregateDeclaration:
			return this.AsAggregateDeclaration().getInputFile();
		case SYMKIND.SK_TypeParameterSymbol:
			if (parent.IsAggregateSymbol())
			{
				return null;
			}
			if (parent.IsMethodSymbol())
			{
				return parent.AsMethodSymbol().getInputFile();
			}
			break;
		case SYMKIND.SK_FieldSymbol:
			return this.AsFieldSymbol().containingDeclaration().getInputFile();
		case SYMKIND.SK_MethodSymbol:
			return this.AsMethodSymbol().containingDeclaration().getInputFile();
		case SYMKIND.SK_PropertySymbol:
			return this.AsPropertySymbol().containingDeclaration().getInputFile();
		case SYMKIND.SK_EventSymbol:
			return this.AsEventSymbol().containingDeclaration().getInputFile();
		case SYMKIND.SK_GlobalAttributeDeclaration:
			return parent.getInputFile();
		}
		return null;
	}

	public bool IsVirtual()
	{
		switch (kind)
		{
		case SYMKIND.SK_MethodSymbol:
			return this.AsMethodSymbol().isVirtual;
		case SYMKIND.SK_EventSymbol:
			if (this.AsEventSymbol().methAdd != null)
			{
				return this.AsEventSymbol().methAdd.isVirtual;
			}
			return false;
		case SYMKIND.SK_PropertySymbol:
			if (this.AsPropertySymbol().methGet == null || !this.AsPropertySymbol().methGet.isVirtual)
			{
				if (this.AsPropertySymbol().methSet != null)
				{
					return this.AsPropertySymbol().methSet.isVirtual;
				}
				return false;
			}
			return true;
		default:
			return false;
		}
	}

	public bool IsOverride()
	{
		switch (kind)
		{
		case SYMKIND.SK_MethodSymbol:
		case SYMKIND.SK_PropertySymbol:
			return this.AsMethodOrPropertySymbol().isOverride;
		case SYMKIND.SK_EventSymbol:
			return this.AsEventSymbol().isOverride;
		default:
			return false;
		}
	}

	public bool IsHideByName()
	{
		switch (kind)
		{
		case SYMKIND.SK_MethodSymbol:
		case SYMKIND.SK_PropertySymbol:
			return this.AsMethodOrPropertySymbol().isHideByName;
		case SYMKIND.SK_EventSymbol:
			if (this.AsEventSymbol().methAdd != null)
			{
				return this.AsEventSymbol().methAdd.isHideByName;
			}
			return false;
		default:
			return true;
		}
	}

	public Symbol SymBaseVirtual()
	{
		switch (kind)
		{
		case SYMKIND.SK_MethodSymbol:
		case SYMKIND.SK_PropertySymbol:
			return this.AsMethodOrPropertySymbol().swtSlot.Sym;
		default:
			return null;
		}
	}

	public bool isUserCallable()
	{
		SYMKIND sYMKIND = kind;
		if (sYMKIND == SYMKIND.SK_MethodSymbol)
		{
			return this.AsMethodSymbol().isUserCallable();
		}
		return true;
	}
}
