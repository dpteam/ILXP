using System;
using System.Collections.Generic;

namespace ILXP
{
    /// <summary>
    /// All rendered lines of the document, in order.
    /// </summary>
    public sealed class LineCollection : List<CodeLine>
    {
        public override string ToString()
        {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            foreach (CodeLine line in this)
            {
                foreach (CodeToken token in line.Tokens)
                {
                    text.Append(token.Text);
                }
                text.AppendLine();
            }
            return text.ToString();
        }
    }
}
