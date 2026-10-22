# Reverse engineering notes: `refs/_MayBeNeedReverse`

All four artifacts were analysed with REA (managed-artifact inspection first, then native
Ghidra analysis for the DLL). They are **native** PE files, not managed assemblies, so there is
no CIL to decompile. This document records what they are, the evidence, and how each one is
used by ILXP.

## Summary

| File | Architecture | Version | Role | Integrated as |
| --- | --- | --- | --- | --- |
| `BeaEngine.dll` | x86 PE32 | BeaEngine 4.x (export `_Disasm@4`) | x86 disassembler engine | `src/ILXP/Interop/BeaEngine.cs` |
| `ilasm.exe` | x86 PE32 | 4.7.2558.0 (NET471REL1) | Microsoft IL assembler | `src/ILXP/Interop/IlTools.cs`, `native/ilasm.exe` |
| `ildasm.exe` | x86-64 PE32+ | 4.6.1055.0 (NETFXREL2) | Microsoft IL disassembler | `src/ILXP/Interop/IlTools.cs`, `native/ildasm.exe` |
| `fusion.dll` | x86 PE32 | 2.0.50727.8745 | .NET Fusion / assembly manager | used as reference for GAC probing in `AssemblyResolver` |

## Evidence

REA managed inspection (`inspect_managed_artifact`) reported `not-managed` for every file:
the PE has no CLI data directory, so metadata and CIL are unavailable. That is why the native
provider was used for `BeaEngine.dll`.

### BeaEngine.dll

* SHA-256 `2fbe38190fec83b2414ea50c137b1cd12acfa0b8e090abb7afdf56e20357968d`
* 250 880 bytes, PE32, machine `0x014C`, image base `0x10000000`
* Sections: `.text` (212 992 bytes), `.rdata`, `.data`, `.reloc`, plus a `tdb` segment
* Ghidra overview: 696 procedures, 586 defined strings
* Export table: exactly one export, `_Disasm@4` (stdcall `int Disasm(PDISASM)`)
* Imports are the usual C runtime/heap set (`HeapCreate`, `HeapAlloc`, `VirtualAlloc`,
  `VirtualQuery`, ...), consistent with a statically linked C/C++ DLL
* No version resource; the export name and `DISASM` interface identify the BeaEngine 4 ABI

Integration details:

* The `DISASM` structure is passed as a 4 KiB zeroed buffer. On x86 the relevant offsets are
  `EIP = 0`, `VirtualAddr = 8`, `SecurityBlock = 16`, `CompleteInstr = 20` (64 bytes),
  `Archi = 84`, `Options = 88`.
* **`EIP` is a pointer to the instruction bytes, not an offset.** The managed wrapper pins the
  byte array and passes `AddrOfPinnedObject + offset`; passing an integer offset crashes the
  engine (verified: access violation `0xC0000005`).
* `Archi` is `0` for 32-bit and `64` for 64-bit decoding; `Options` is `0`.
* The textual output ends with the encoded bytes in parentheses (`mov eax, ebx (8B C3)`), which
  is used to advance to the next instruction.
* The library is optional and 32-bit only. On a 64-bit process `BeaEngine.IsAvailable` returns
  false and `--disasm-x86` reports that the x86 build is required.

### ilasm.exe

* SHA-256 `232be776d49c1a21e28d3081177f0e3434f612484630427efd4ab0678d6835e0`
* 296 544 bytes, PE32 x86
* Version resource: `Microsoft (R) .NET Framework IL Assembler version 4.7.2558.0`,
  file version `4.7.2558.0 built by: NET471REL1`
* Embedded usage strings confirm the standard option set (`/SSVER`, `/MDV`, `/MSV`, ...)
* Imports `mscoree.dll` and `fusion.dll`; it is the classic native IL assembler

Integration: `IlTools.Assemble` runs it with `/quiet /output=... (/dll|/exe) (/x86|/x64)`.
A full round-trip was verified: ILDasm output of `Mono.Cecil.dll` (3.8 MB IL) was reassembled
into a working 357 KB DLL.

### ildasm.exe

* SHA-256 `18e8735fddbb1439dc5c7142e97f73e55ce274c973f5b9348e1c09b3d5e7e74d`
* 572 056 bytes, PE32+ **x64**
* Version resource: `Microsoft (R) .NET Framework IL Disassembler`, file version
  `4.6.1055.0 built by: NETFXREL2`
* Strings show the expected disassembler output (`// Metadata version: %s`,
  `Major runtime version`, `SuppressIldasmAttribute`, ...)
* This particular copy expects its companion resources (`ildasmfnt.bin`) in the tool folder.
  Without them it exits with code 1 and writes only the file header. The working SDK copy
  (`NETFX 4.8 Tools\ildasm.exe`, version 4.8.3928.0) was placed in `native/ildasm.exe`
  instead; the original 4.6 binary remains in `refs/_MayBeNeedReverse` for preservation.

Integration: `IlTools.Disassemble` runs it with `/text /out=...` (optionally `/bytes`).

### fusion.dll

* SHA-256 `3d8f55fa20c8f1607095b734f528cd2d73c899e81330020c24a9ed5a9fa3738a`
* 16 528 bytes, PE32 x86
* Version resource: `Microsoft .NET Framework`, `2.0.50727.8745`, description
  "Assembly manager"
* Exports: `CreateAssemblyCache`, `CreateAssemblyEnum`, `CreateAssemblyNameObject`,
  `GetCachePath`, `PreBindAssembly`, `PreBindAssemblyEx`, `InstallCustomAssembly`,
  `InstallCustomModule`, `LookupHistoryAssembly`, `NukeDownloadedCache`, `ClearDownloadCache`,
  `CopyPDBs`, `CreateApplicationContext`, `CreateHistoryReader`, `CreateInstallReferenceEnum`,
  `GetHistoryFileDirectory`, `InitializeFusion`
* Imports `mscoree.dll`; the manifest references the VC80 runtime

This is the native Fusion shim that forwards to the CLR's assembly binder. ILXP does not
P/Invoke it (that would bind the process to a specific CLR version); instead the GAC layout it
implies is implemented directly in `AssemblyResolver` (`GAC_MSIL`, `GAC_32`, `GAC_64`, `GAC`,
`v4.0_` folder prefix, `WINDIR\assembly` and `WINDIR\Microsoft.NET\assembly` roots).

## Reproducing the analysis

* `tools/Inspect-NativeArtifacts.ps1` parses PE headers, exports and interesting strings for
  all four files without external tools.
* `tools/Extract-IcoFrames.ps1` extracts icon frames (used for `ILXP.ico`).
* REA evidence for the managed checks and the BeaEngine overview is retained in the session
  log (`inspect_managed_artifact`, `binary_overview`, `list_names`).

## Limitations / unknowns

* The exact BeaEngine build number is not present in the binary; only the BeaEngine 4 ABI is
  established by the `_Disasm@4` export and `DISASM` layout.
* `ildasm.exe` from `refs` could not be made to run standalone without its companion resource
  files; the SDK 4.8 copy is used instead.
* `fusion.dll` was not executed or called; its export list and the CLR GAC layout are the only
  evidence used.
