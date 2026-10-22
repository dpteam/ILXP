using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

[SuppressIldasm]
public sealed class GClass9 : IComparer
{
	[DebuggerNonUserCode]
	public GClass9()
	{
	}

	int IComparer.Compare(object x, object y)
	{
		TreeNode treeNode = (TreeNode)x;
		TreeNode treeNode2 = (TreeNode)y;
		int num = 99;
		if (!string.IsNullOrEmpty(treeNode.ImageKey))
		{
			if (char.IsDigit(treeNode.ImageKey[1]))
			{
				num = int.Parse(treeNode.ImageKey.Substring(0, 2));
			}
			else if (char.IsDigit(treeNode.ImageKey[0]))
			{
				num = int.Parse(treeNode.ImageKey.Substring(0, 1));
			}
		}
		int value = 99;
		if (!string.IsNullOrEmpty(treeNode2.ImageKey))
		{
			if (char.IsDigit(treeNode2.ImageKey[1]))
			{
				value = int.Parse(treeNode2.ImageKey.Substring(0, 2));
			}
			else if (char.IsDigit(treeNode2.ImageKey[0]))
			{
				value = int.Parse(treeNode2.ImageKey.Substring(0, 1));
			}
		}
		int num2 = num.CompareTo(value);
		if (num2 == 0)
		{
			num2 = treeNode.Text.CompareTo(treeNode2.Text);
		}
		return num2;
	}
}
