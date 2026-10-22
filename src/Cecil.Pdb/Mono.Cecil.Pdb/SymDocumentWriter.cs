namespace Mono.Cecil.Pdb;

internal class SymDocumentWriter
{
	private readonly ISymUnmanagedDocumentWriter m_unmanagedDocumentWriter;

	public SymDocumentWriter(ISymUnmanagedDocumentWriter unmanagedDocumentWriter)
	{
		m_unmanagedDocumentWriter = unmanagedDocumentWriter;
	}

	public ISymUnmanagedDocumentWriter GetUnmanaged()
	{
		return m_unmanagedDocumentWriter;
	}
}
