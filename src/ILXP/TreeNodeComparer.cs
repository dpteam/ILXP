using System;
using System.Collections;
using System.Globalization;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>
    /// Orders tree nodes by the numeric prefix of their image key (the original
    /// designer icons are named like <c>7VSObject_Class.bmp</c>, where the leading
    /// number defines the sort group) and then by their text.
    /// </summary>
    public sealed class TreeNodeComparer : IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            TreeNode left = x as TreeNode;
            TreeNode right = y as TreeNode;
            int result = GetSortGroup(left).CompareTo(GetSortGroup(right));
            if (result == 0)
            {
                result = string.Compare(
                    left == null ? string.Empty : left.Text,
                    right == null ? string.Empty : right.Text,
                    StringComparison.CurrentCulture);
            }
            return result;
        }

        private static int GetSortGroup(TreeNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.ImageKey))
            {
                return 99;
            }

            string key = node.ImageKey;
            int length = key.Length > 1 && char.IsDigit(key[1]) ? 2 : 1;
            int value;
            if (char.IsDigit(key[0]) && int.TryParse(key.Substring(0, length), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return 99;
        }
    }
}
