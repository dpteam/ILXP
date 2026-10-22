namespace Microsoft.Cci;

public interface IName
{
	int UniqueKey { get; }

	int UniqueKeyIgnoringCase { get; }

	string Value { get; }
}
