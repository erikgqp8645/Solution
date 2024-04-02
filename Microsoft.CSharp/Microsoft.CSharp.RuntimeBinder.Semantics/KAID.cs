namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum KAID
{
	kaidNil = -1,
	kaidGlobal = 0,
	kaidErrorAssem = 1,
	kaidThisAssembly = 2,
	kaidUnresolved = 3,
	kaidStartAssigning = 4,
	kaidMinModule = 268435456
}
