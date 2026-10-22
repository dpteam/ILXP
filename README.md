# ILXP

ILXP is a preserved and restored .NET decompiler / IL analysis tool. It started life as a
decompiled copy of the old **CodeReflect** decompiler (DevExtras) and is being rebuilt into a
maintainable, deobfuscated application with additional functionality merged from other open
tools (ILSpy, dnSpy, de4dot and the Unscrambler feature set).

The project is deliberately conservative:

* **.NET Framework 4.0**, x86 and x64 (Windows XP compatible).
* No external runtime dependencies at startup; everything needed is either source in this
  repository or shipped in `native/`.
* All third-party libraries are embedded as source and built from this solution.
* Every failure path is logged through `System.Diagnostics.Trace` and degrades gracefully.

## Building

Requirements: .NET SDK (tested with 9.x) and the .NET Framework 4.0 reference assemblies
(installed with Visual Studio or the Windows SDK).

```powershell
dotnet build ILXP.slnx
```

Output:

* `src/ILXP/bin/Debug/net40/ILXP.exe` – AnyCPU application
* `src/ILXP/bin/x86/Debug/net40/ILXP.exe` – x86 build (`-p:Platform=x86`)
* `src/ILXP/bin/x64/Debug/net40/ILXP.exe` – x64 build (`-p:Platform=x64`)

The `native/` folder (ILDasm, ILAsm, BeaEngine, fusion.dll) is copied next to the executable.

## Running

Start the GUI:

```powershell
src\ILXP\bin\Debug\net40\ILXP.exe
```

Command line mode (all commands are logged to `%APPDATA%\ILXP\logs\ILXP.log`):

```text
ILXP --version
ILXP --list <assembly>
ILXP --decompile <assembly> [--language il|cs|vb] [--output <dir>]
ILXP --resources <assembly> --output <dir>
ILXP --ildasm <assembly> [--output <file.il>] [--bytes]
ILXP --ilasm <file.il> [--output <assembly>] [--dll] [--x86|--x64]
ILXP --disasm-x86 <file> [--offset <n>] [--length <n>] [--x64]
ILXP --clean <assembly> [--output <assembly>]
```

Notes:

* `--decompile` writes one file per type using the selected writer. IL output contains method
  bodies; the C#/VB type view is an outline and method bodies are produced when a method is
  selected in the GUI (or by calling the writer per method).
* `--disasm-x86` requires the **x86** build because BeaEngine is a 32-bit native library.
* `--clean` applies the IL cleanup passes from `src/ILXP/Deobfuscation` and writes a new
  assembly. Strong-name signatures are not preserved.

## Repository layout

```text
ILXP.slnx                     solution
ILXP.ico                      application icon (from refs/239.ico)
src/ILXP/                     application source
  AppInfo.cs, AppSettings.cs, Log.cs, Program.cs
  AssemblyItem.cs, ModuleItem.cs, AssemblyResolver.cs
  LanguageWriter.cs, CSharpWriter.cs, IlWriter.cs, VbWriter.cs
  StatementDecompiler.cs      IL -> C#/VB statement translator
  FileContentInspector.cs     resource classification / hex dump / .resources view
  MainForm.cs, MainForm.Designer.cs, AboutForm.cs
  CodeEditor.cs, BorderPanel.cs, UiThread.cs, CodeLine.cs, CodeToken.cs ...
  Deobfuscation/              IL cleanup passes (ported from Unscrambler)
  Interop/                    BeaEngine, ILAsm/ILDasm integration, Win32 helpers
  Resources/                  embedded images and tree icon payload
src/Cecil/                    Mono.Cecil 0.9.6.4, decompiled and embedded as source
src/Cecil.Pdb/                Mono.Cecil.Pdb, decompiled and embedded as source
native/                       shipped native tools (see REVERSE_ENGINEERING.md)
tools/                        maintenance scripts used during the restoration
refs/                         reference material (git-ignored)
```

## Status

See [PROJECT_STATE.md](PROJECT_STATE.md) for the current state and [ROADMAP.md](ROADMAP.md)
for the plan. The short version: the application builds, starts, loads assemblies, renders
IL/C#/VB, extracts resources, can call ILDasm/ILAsm, and has a working CLI. The remaining
large work items are the full merge of de4dot/dnSpy functionality and a complete rename of
the remaining obfuscated member names inside `StatementDecompiler`.

## Documentation

* [ARCHITECTURE.md](ARCHITECTURE.md) – how the decompiler and UI fit together
* [ROADMAP.md](ROADMAP.md) – planned work
* [REVERSE_ENGINEERING.md](REVERSE_ENGINEERING.md) – findings for the shipped native tools
* [PROJECT_STATE.md](PROJECT_STATE.md) – state, conventions, next steps
* [PROMPT.md](PROMPT.md) – original task statement and rules
