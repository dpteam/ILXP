using System.Drawing;

namespace ILXP
{
    /// <summary>
    /// A single styled run of text inside a rendered source line. Tokens may carry a
    /// link target, which the editor turns into a clickable hyperlink.
    /// </summary>
    public sealed class CodeToken
    {
        private string text;
        private Color color = Color.Black;
        private string link;
        private Rectangle bounds;

        public CodeToken()
        {
        }

        public CodeToken(string text)
        {
            this.text = text;
        }

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public Rectangle Bounds
        {
            get { return bounds; }
            set { bounds = value; }
        }

        public string Link
        {
            get { return link; }
            set { link = value; }
        }

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }
    }
}
