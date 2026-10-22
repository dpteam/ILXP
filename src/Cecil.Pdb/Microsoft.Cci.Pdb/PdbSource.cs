using System;

namespace Microsoft.Cci.Pdb;

internal class PdbSource
{
	internal string name;

	internal Guid doctype;

	internal Guid language;

	internal Guid vendor;

	internal PdbSource(string name, Guid doctype, Guid language, Guid vendor)
	{
		this.name = name;
		this.doctype = doctype;
		this.language = language;
		this.vendor = vendor;
	}
}
