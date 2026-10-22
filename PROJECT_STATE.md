# Project state (for continuing work)

This file is the hand-off note for future sessions and for context compaction. Keep it up to
date whenever a milestone is reached.

## Ground rules (from PROMPT.md)

1. Everything targets **.NET Framework 4.0**, x86 and x64, Windows XP compatible.
2. Prefer dynamic fault tolerance and defensive handling over hard failures.
3. Logging goes through `Trace` (file + console when a console is attached) without duplicate
   listeners.
4. Support any MSIL input, including damaged assemblies and memory dumps.
5. Commit regularly; the user pushes manually.
6. Documentation files (this one, `ROADMAP.md`, `ARCHITECTURE.md`, `REVERSE_ENGINEERING.md`,
   `README.md`, `PROMPT.md`) must be updated as the project evolves.

## Current build status

```
dotnet build ILXP.slnx                 # 0 errors
dotnet build src/ILXP/ILXP.csproj -p:Platform=x86
dotnet build src/ILXP/ILXP.csproj -p:Platform=x64
```

Verified manually:

* GUI starts and stays alive; log file is written to `%APPDATA%\ILXP\logs\ILXP.log`.
* `--list`, `--decompile` (il/cs/vb), `--resources` work on `ILXP.exe` and `Mono.Cecil.dll`.
* `--ildasm` produces IL for `ILXP.exe`; `--ilasm` reassembles ILDasm output into a working
  DLL (round-trip verified with Mono.Cecil).
* `--disasm-x86` decodes bytes with BeaEngine in the **x86** build.
* `--clean` removes NOPs and folds `sizeof`/`Type.EmptyTypes.Length`.

## Known issues / next fixes

1. `StatementDecompiler` still has `smethod_N` names for internal helpers. Public entry points
   are `WriteVisualBasicBody` / `WriteCSharpBody`.
2. The C#/VB writers still have rough edges: attributes on properties/events, generic
   constraints, string escaping, and some `checked`/`Conversions` noise from the VB origin.
3. `refs/_MayBeNeedReverse/ildasm.exe` (4.6 x64) needs companion resources; `native/ildasm.exe`
   is the working SDK 4.8 copy.
4. `--clean` does not preserve strong-name signatures and writes without symbols.
5. `native/fusion.dll` is shipped for preservation only; ILXP does not call it.
6. `AssemblyResolver` probes the GAC by directory layout; it does not use the Fusion API.
7. `GetTypeName` in `VbWriter` prints .NET type names (`Int32`, `String`), which is valid VB but
   not idiomatic; the C# writer maps to keywords.
8. The x86 build reports many warnings (mostly obsolete/CLS from the embedded Cecil sources).

## Useful commands

```powershell
# build
dotnet build ILXP.slnx

# run GUI
src\ILXP\bin\Debug\net40\ILXP.exe

# decompile an assembly to IL files
src\ILXP\bin\Debug\net40\ILXP.exe --decompile path\to\a.dll --language il --output out

# round-trip through ILDasm/ILAsm
src\ILXP\bin\Debug\net40\ILXP.exe --ildasm a.dll --output a.il
src\ILXP\bin\Debug\net40\ILXP.exe --ilasm a.il --output a.rt.dll --dll

# clean NOPs/sizeof
src\ILXP\bin\Debug\net40\ILXP.exe --clean a.dll --output a.clean.dll

# x86 disassembly (must use the x86 build)
src\ILXP\bin\x86\Debug\net40\ILXP.exe --disasm-x86 native\BeaEngine.dll --offset 0 --length 32 --output bea.txt
```

## Maintenance scripts (`tools/`)

* `Decrypt-Strings.ps1` – original string-table decryption (already applied; kept for history).
* `Extract-Resx.ps1`, `Extract-IcoFrames.ps1`, `New-CompanyLogo.ps1` – resource recovery.
* `Add-Namespaces.ps1`, `Rename-Types.ps1`, `Rename-Writers.ps1`,
  `Rename-StatementDecompiler.ps1` – the rename passes that were applied.
* `Cleanup-Sources.ps1`, `Fix-*.ps1` – repairs for decompiler artifacts.
* `Inspect-NativeArtifacts.ps1` – PE header/export/version/string inspection for `native/`.
* `List-Usage.ps1`, `List-EventWiring.ps1` – helper reports used while renaming.

The scripts are not part of the build; they document how the restoration was done and can be
re-run on a fresh decompiled snapshot.

## Suggested next steps (in order)

1. Finish renaming `StatementDecompiler` internals (read each helper, give it a semantic name,
   update the call sites inside the file only).
2. Port the remaining Unscrambler passes (`MathReplace` first) into `Deobfuscation/`.
3. Add dnlib as `src/dnlib` (decompiled), then port `de4dot.blocks` and `de4dot.code.renamer`.
4. Add a GUI "Clean assembly" dialog that runs `DeobfuscationPipeline` and saves the result.
5. Add a smoke test (load every assembly in a folder, render every node, assert no exception).
6. Improve the C# writer (attributes, generics, events) and re-check the round-trip.

## Commit style

Short imperative subjects, e.g.:

* `refactor: decrypt and inline obfuscated string table`
* `build: restructure into solution, embed Mono.Cecil sources, restore resources and settings`
* `refactor: rewrite UI layer with named members, add CLI mode, improve C#/VB writers`
* `feat: integrate ILDasm/ILAsm and BeaEngine, add IL cleanup passes`
