using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb;

internal static class GuidMapping
{
	private static readonly Dictionary<Guid, DocumentLanguage> guid_language;

	private static readonly Dictionary<DocumentLanguage, Guid> language_guid;

	private static readonly Guid type_text;

	private static readonly Guid hash_md5;

	private static readonly Guid hash_sha1;

	private static readonly Guid vendor_ms;

	static GuidMapping()
	{
		guid_language = new Dictionary<Guid, DocumentLanguage>();
		language_guid = new Dictionary<DocumentLanguage, Guid>();
		type_text = new Guid(1518771467, 26129, 4563, 189, 42, 0, 0, 248, 8, 73, 189);
		hash_md5 = new Guid(1080993376, 25807, 19586, 182, 240, 66, 212, 129, 114, 167, 153);
		hash_sha1 = new Guid(4279768812u, 43614, 19728, 135, 247, 111, 73, 99, 131, 52, 96);
		vendor_ms = new Guid(2571847108u, 59113, 4562, 144, 63, 0, 192, 79, 163, 2, 161);
		AddMapping(DocumentLanguage.C, new Guid(1671464724u, 64567, 4562, 144, 76, 0, 192, 79, 163, 2, 161));
		AddMapping(DocumentLanguage.Cpp, new Guid(974311607u, 49772, 4560, 180, 66, 0, 160, 36, 74, 29, 210));
		AddMapping(DocumentLanguage.CSharp, new Guid(1062298360, 1990, 4563, 144, 83, 0, 192, 79, 163, 2, 161));
		AddMapping(DocumentLanguage.Basic, new Guid(974311608u, 49772, 4560, 180, 66, 0, 160, 36, 74, 29, 210));
		AddMapping(DocumentLanguage.Java, new Guid(974311604u, 49772, 4560, 180, 66, 0, 160, 36, 74, 29, 210));
		AddMapping(DocumentLanguage.Cobol, new Guid(2936302801u, 53473, 4562, 151, 124, 0, 160, 201, 180, 213, 12));
		AddMapping(DocumentLanguage.Pascal, new Guid(2936302802u, 53473, 4562, 151, 124, 0, 160, 201, 180, 213, 12));
		AddMapping(DocumentLanguage.Cil, new Guid(2936302803u, 53473, 4562, 151, 124, 0, 160, 201, 180, 213, 12));
		AddMapping(DocumentLanguage.JScript, new Guid(974311606u, 49772, 4560, 180, 66, 0, 160, 36, 74, 29, 210));
		AddMapping(DocumentLanguage.Smc, new Guid(228302715, 26129, 4563, 189, 42, 0, 0, 248, 8, 73, 189));
		AddMapping(DocumentLanguage.MCpp, new Guid(1261829608, 1990, 4563, 144, 83, 0, 192, 79, 163, 2, 161));
		AddMapping(DocumentLanguage.FSharp, new Guid(2874095817u, 46822, 17338, 190, 59, 88, 8, 11, 44, 204, 227));
	}

	private static void AddMapping(DocumentLanguage language, Guid guid)
	{
		guid_language.Add(guid, language);
		language_guid.Add(language, guid);
	}

	public static DocumentType ToType(this Guid guid)
	{
		if (guid == type_text)
		{
			return DocumentType.Text;
		}
		return DocumentType.Other;
	}

	public static Guid ToGuid(this DocumentType type)
	{
		if (type == DocumentType.Text)
		{
			return type_text;
		}
		return default(Guid);
	}

	public static DocumentHashAlgorithm ToHashAlgorithm(this Guid guid)
	{
		if (guid == hash_md5)
		{
			return DocumentHashAlgorithm.MD5;
		}
		if (guid == hash_sha1)
		{
			return DocumentHashAlgorithm.SHA1;
		}
		return DocumentHashAlgorithm.None;
	}

	public static Guid ToGuid(this DocumentHashAlgorithm hash_algo)
	{
		return hash_algo switch
		{
			DocumentHashAlgorithm.MD5 => hash_md5, 
			DocumentHashAlgorithm.SHA1 => hash_sha1, 
			_ => default(Guid), 
		};
	}

	public static DocumentLanguage ToLanguage(this Guid guid)
	{
		if (!guid_language.TryGetValue(guid, out var value))
		{
			return DocumentLanguage.Other;
		}
		return value;
	}

	public static Guid ToGuid(this DocumentLanguage language)
	{
		if (!language_guid.TryGetValue(language, out var value))
		{
			return default(Guid);
		}
		return value;
	}

	public static DocumentLanguageVendor ToVendor(this Guid guid)
	{
		if (guid == vendor_ms)
		{
			return DocumentLanguageVendor.Microsoft;
		}
		return DocumentLanguageVendor.Other;
	}

	public static Guid ToGuid(this DocumentLanguageVendor vendor)
	{
		if (vendor == DocumentLanguageVendor.Microsoft)
		{
			return vendor_ms;
		}
		return default(Guid);
	}
}
