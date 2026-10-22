using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Resources;
using System.Text;
using System.Xml;

namespace ILXP
{
    /// <summary>Kind of payload stored in an embedded resource.</summary>
    public enum FileContentKind
    {
        Binary,
        Text,
        Xml,
        Image,
        ResX
    }

    /// <summary>
    /// Detects what an embedded resource contains and renders it as text for the
    /// source viewer. Everything is defensive: damaged or truncated resources must
    /// still produce a readable hex dump instead of an exception.
    /// </summary>
    public static class FileContentInspector
    {
        /// <summary>Detects the content kind, using the file extension as a hint.</summary>
        public static FileContentKind Detect(string fileName, byte[] data)
        {
            if (data == null)
            {
                return FileContentKind.Binary;
            }

            string extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            switch (extension)
            {
                case ".resources":
                    if (IsResX(data))
                    {
                        return FileContentKind.ResX;
                    }
                    break;
                case ".png":
                case ".gif":
                case ".bmp":
                case ".jpg":
                case ".jpeg":
                case ".ico":
                    using (Image image = TryLoadImage(data))
                    {
                        if (image != null)
                        {
                            return FileContentKind.Image;
                        }
                    }
                    break;
            }

            return DetectFromContent(data);
        }

        /// <summary>Detects the content kind from the raw bytes alone.</summary>
        public static FileContentKind DetectFromContent(byte[] data)
        {
            if (data == null || data.Length < 2)
            {
                return FileContentKind.Binary;
            }

            StreamReader reader;
            int bom = (data[0] << 8) | data[1];
            if (bom == 0xEFBB)
            {
                if (data.Length < 3 || data[2] != 0xBF)
                {
                    return FileContentKind.Binary;
                }
                reader = new StreamReader(new MemoryStream(data, 2, data.Length - 2), Encoding.UTF8);
            }
            else if (bom == 0xFFFE || bom == 0xFEFF)
            {
                reader = new StreamReader(new MemoryStream(data), true);
            }
            else
            {
                if (!IsUtf8(data))
                {
                    return FileContentKind.Binary;
                }
                reader = new StreamReader(new MemoryStream(data), Encoding.UTF8);
            }

            using (reader)
            {
                try
                {
                    XmlTextReader xml = new XmlTextReader(reader);
                    xml.XmlResolver = null;
                    xml.MoveToContent();
                    return FileContentKind.Xml;
                }
                catch (XmlException)
                {
                    return FileContentKind.Text;
                }
            }
        }

        /// <summary>Checks whether the buffer looks like valid UTF-8 text.</summary>
        private static bool IsUtf8(byte[] data)
        {
            int length = Math.Min(data.Length, 500000);
            int state = 0;
            int continuation = 0;

            for (int i = 0; i < length; i++)
            {
                byte value = data[i];
                if (value < 0x80)
                {
                    if (state == 3)
                    {
                        return false;
                    }
                }
                else if (value < 0xC0)
                {
                    if (state != 3)
                    {
                        return false;
                    }
                    continuation--;
                    if (continuation < 0)
                    {
                        return false;
                    }
                    if (continuation == 0)
                    {
                        state = 2;
                    }
                }
                else
                {
                    if (value < 0xC2 || value >= 0xF5)
                    {
                        return false;
                    }
                    if (state != 2 && state != 0)
                    {
                        return false;
                    }
                    state = 3;
                    continuation = value < 0xE0 ? 1 : (value >= 0xF0 ? 3 : 2);
                }
            }
            return state != 1;
        }

        /// <summary>Decodes text, falling back to a hex dump when the bytes are not text.</summary>
        public static string DecodeText(byte[] data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            try
            {
                if (data.Length < 2)
                {
                    return FormatHexDump(data);
                }

                StreamReader reader;
                int bom = (data[0] << 8) | data[1];
                if (bom == 0xEFBB && data.Length >= 3 && data[2] == 0xBF)
                {
                    reader = new StreamReader(new MemoryStream(data, 2, data.Length - 2), Encoding.UTF8);
                }
                else if (bom == 0xFFFE || bom == 0xFEFF)
                {
                    reader = new StreamReader(new MemoryStream(data), true);
                }
                else if (IsUtf8(data))
                {
                    reader = new StreamReader(new MemoryStream(data), Encoding.UTF8);
                }
                else
                {
                    return FormatHexDump(data);
                }

                using (reader)
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to decode resource as text; falling back to hex dump.", ex);
                return FormatHexDump(data);
            }
        }

        /// <summary>Classic 16-bytes-per-line hex dump (first 4 KiB only).</summary>
        public static string FormatHexDump(byte[] data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            StringBuilder text = new StringBuilder();
            for (int offset = 0; offset < data.Length; offset += 16)
            {
                text.Append(offset.ToString("x6")).Append(": ");
                int count = Math.Min(16, data.Length - offset);
                for (int i = 0; i < count; i++)
                {
                    text.Append(data[offset + i].ToString("x2")).Append(' ');
                }

                if (count < 16)
                {
                    text.Append(new string(' ', (16 - count) * 3));
                }

                text.Append("   ");
                for (int i = 0; i < count; i++)
                {
                    byte value = data[offset + i];
                    text.Append(value == 0 ? '.' : (value == '\t' ? ' ' : (char)value));
                }
                text.AppendLine();

                if (offset >= 4096)
                {
                    text.AppendLine("......");
                    break;
                }
            }
            return text.ToString();
        }

        /// <summary>Loads an image from a buffer, returning null for unsupported data.</summary>
        public static Image TryLoadImage(byte[] data)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (Bitmap bitmap = new Bitmap(stream))
                    {
                        return new Bitmap(bitmap);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Renders the entries of a .resources file as a readable list.</summary>
        public static string FormatResourceTable(byte[] data)
        {
            try
            {
                StringBuilder text = new StringBuilder();
                using (MemoryStream stream = new MemoryStream(data))
                using (ResourceReader reader = new ResourceReader(stream))
                {
                    IDictionaryEnumerator enumerator = reader.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        DictionaryEntry entry = enumerator.Entry;
                        text.Append(Convert.ToString(entry.Key).PadRight(64, ' '));
                        if (entry.Value is string || entry.Value is int)
                        {
                            text.Append(Convert.ToString(entry.Value));
                        }
                        else if (entry.Value != null)
                        {
                            text.Append(string.Format("<{0}>", entry.Value.GetType().FullName));
                        }
                        text.AppendLine();
                    }
                }
                return text.ToString();
            }
            catch (Exception ex)
            {
                Log.Warn("Unable to read .resources payload; falling back to hex dump.", ex);
                return FormatHexDump(data);
            }
        }

        private static bool IsResX(byte[] data)
        {
            return data.Length > 10 &&
                   data[0] == 0xCE && data[1] == 0xCA && data[2] == 0xEF && data[3] == 0xBE && data[4] == 0x01;
        }
    }
}
