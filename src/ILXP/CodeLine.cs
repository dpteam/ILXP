namespace ILXP
{
    /// <summary>
    /// A rendered line of source code: its tokens plus the character range the line
    /// occupies in the full document.
    /// </summary>
    public sealed class CodeLine
    {
        public CodeLine()
        {
            Tokens = new TokenList();
        }

        public TokenList Tokens { get; private set; }

        /// <summary>Zero-based character offset of the first character of the line.</summary>
        public int Offset { get; set; }

        /// <summary>Number of characters in the line (excluding the line break).</summary>
        public int Length { get; set; }
    }
}
