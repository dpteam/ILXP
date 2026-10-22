using System.Collections.Generic;

namespace Microsoft.Cci;

public interface INamespaceScope
{
	IEnumerable<IUsedNamespace> UsedNamespaces { get; }
}
