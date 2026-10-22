using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace ILXP
{
    public sealed class AssemblyResolver
    {
    	[DebuggerNonUserCode]
    	public AssemblyResolver()
    	{
    	}

    	public static string smethod_0(string string_0, TreeNodeCollection treeNodeCollection_0)
    	{
    		List<string> list = new List<string>();
    		foreach (TreeNode item in treeNodeCollection_0)
    		{
    			if (item.Tag is AssemblyItem)
    			{
    				list.Add(Path.GetDirectoryName(((AssemblyItem)item.Tag).method_0()));
    			}
    		}
    		return smethod_1(string_0, list);
    	}

    	public static string smethod_1(string string_0, List<string> list_0)
    	{
    		string text = null;
    		string text2 = null;
    		string text3 = null;
    		if (string.IsNullOrEmpty(string_0))
    		{
    			throw new Exception("Invalid assembly name");
    		}
    		string[] array = string_0.Split(',');
    		checked
    		{
    			int num = array.Length - 1;
    			int num2 = 0;
    			while (true)
    			{
    				if (num2 <= num)
    				{
    					string text4 = array[num2].Trim();
    					if (num2 == 0)
    					{
    						text = text4;
    					}
    					else
    					{
    						string[] array2 = text4.Split('=');
    						if (array2.Length != 2)
    						{
    							break;
    						}
    						if (Operators.CompareString(array2[0], "PublicKeyToken", TextCompare: false) == 0)
    						{
    							text2 = array2[1];
    						}
    						if (Operators.CompareString(array2[0], "Version", TextCompare: false) == 0)
    						{
    							text3 = array2[1];
    						}
    					}
    					num2++;
    					continue;
    				}
    				List<string> list = new List<string>();
    				string directoryName = Path.GetDirectoryName(Path.GetDirectoryName(typeof(object).Module.FullyQualifiedName));
    				switch (text3.Substring(0, 1))
    				{
    				case "1":
    					if (text3.StartsWith("1.3300"))
    					{
    						list.Add(Path.Combine(directoryName, "v1.0.3705"));
    					}
    					else
    					{
    						list.Add(Path.Combine(directoryName, "v1.0.5000.0"));
    					}
    					break;
    				case "2":
    					list.Add(Path.Combine(directoryName, "v2.0.50727"));
    					break;
    				case "4":
    					list.Add(Path.Combine(directoryName, "v4.0.30319"));
    					break;
    				}
    				foreach (string item in list)
    				{
    					if (!File.Exists(Path.Combine(item, text + ".dll")))
    					{
    						if (File.Exists(Path.Combine(item, text + ".exe")))
    						{
    							return Path.Combine(item, text + ".exe");
    						}
    						continue;
    					}
    					return Path.Combine(item, text + ".dll");
    				}
    				List<string> list2 = new List<string>();
    				list2.Add(Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "assembly"));
    				list2.Add(Path.Combine(Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "Microsoft.NET"), "assembly"));
    				string[] array3 = new string[3] { "GAC_MSIL", "GAC_32", "GAC" };
    				string[] array4 = new string[2] { "", "v4.0_" };
    				int num3 = 0;
    				do
    				{
    					int num4 = array3.Length - 1;
    					for (int i = 0; i <= num4; i++)
    					{
    						string path = Path.Combine(list2[num3], array3[i]);
    						string text5 = Path.Combine(Path.Combine(Path.Combine(path, text), array4[num3] + text3 + "__" + text2), text + ".dll");
    						if (File.Exists(text5))
    						{
    							return text5;
    						}
    					}
    					num3++;
    				}
    				while (num3 <= 1);
    				foreach (string item2 in list_0)
    				{
    					if (!File.Exists(Path.Combine(item2, text + ".dll")))
    					{
    						if (File.Exists(Path.Combine(item2, text + ".exe")))
    						{
    							return Path.Combine(item2, text + ".exe");
    						}
    						continue;
    					}
    					return Path.Combine(item2, text + ".dll");
    				}
    				throw new Exception("Not found.");
    			}
    			throw new Exception("Invalid assembly name");
    		}
    	}
    }

}
