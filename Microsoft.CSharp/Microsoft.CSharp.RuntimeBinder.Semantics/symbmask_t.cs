using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

[Flags]
internal enum symbmask_t : long
{
	MASK_NamespaceSymbol = 1L,
	MASK_NamespaceDeclaration = 2L,
	MASK_AssemblyQualifiedNamespaceSymbol = 4L,
	MASK_AggregateSymbol = 8L,
	MASK_AggregateDeclaration = 0x10L,
	MASK_TypeParameterSymbol = 0x20L,
	MASK_FieldSymbol = 0x40L,
	MASK_LocalVariableSymbol = 0x80L,
	MASK_MethodSymbol = 0x100L,
	MASK_PropertySymbol = 0x200L,
	MASK_EventSymbol = 0x400L,
	MASK_TransparentIdentifierMemberSymbol = 0x800L,
	MASK_Scope = 0x4000L,
	MASK_CachedNameSymbol = 0x8000L,
	MASK_LabelSymbol = 0x10000L,
	MASK_GlobalAttributeDeclaration = 0x20000L,
	MASK_LambdaScope = 0x40000L,
	MASK_ALL = -1L,
	LOOKUPMASK = 0x3C4L
}
