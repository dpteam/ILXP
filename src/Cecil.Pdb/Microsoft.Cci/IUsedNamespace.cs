namespace Microsoft.Cci;

public interface IUsedNamespace
{
	IName Alias { get; }

	IName NamespaceName { get; }
}
