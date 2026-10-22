# Architecture

## Overview

ILXP is a WinForms front end around a metadata reader (Mono.Cecil) and three text writers.
The same metadata tree drives the navigation view and the generated source view.

```
assembly file
    │  Mono.Cecil (AssemblyDefinition.ReadAssembly)
    ▼
AssemblyItem ──► tree: modules → references / namespaces → types → members
    │                                   │
    │                                   ▼
    │                            selected tag (Cecil object / resource / string)
    │                                   │
    ▼                                   ▼
LanguageWriter (IlWriter | CSharpWriter | VbWriter)
    │  markup: <<b>> keyword, <<r>> string, <<g>> comment, <<#target>> link
    ▼
CodeEditor (parse markup → CodeLine/CodeToken, draw, hit-test, links)
```

Embedded resources bypass the writers: `FileContentInspector` classifies the payload
(binary/text/xml/image/ResX) and either hex-dumps it, decodes it as text, loads it as an image,
or renders the `.resources` table.

## Core types

| Type | Responsibility |
| --- | --- |
| `AssemblyItem` | loaded assembly + source path |
| `ModuleItem` | namespace group + module |
| `AssemblyResolver` | probes the CLR folder, GAC and opened assemblies for references |
| `LanguageWriter` | abstract writer + shared metadata formatting/navigation keys |
| `IlWriter` | ILAsm-style listing, including method bodies |
| `CSharpWriter` | C# outline + method bodies via `StatementDecompiler` |
| `VbWriter` | VB outline + method bodies via `StatementDecompiler` |
| `StatementDecompiler` | IL → C#/VB statement translator (largest component) |
| `FileContentInspector` | resource classification and text rendering |
| `AssemblyLoader` | reads assemblies, carving a PE image from larger blobs if needed |
| `Decompiler` | shared "one file per type" export used by the GUI and CLI |
| `OutputDialect` | target language/toolchain profiles for generated source |
| `CodeEditor` | markup-aware read-only code view with selection, copy and links |
| `MainForm` | tree, toolbar, menus, status bar, navigation history |
| `AppSettings` | INI-style settings in `%APPDATA%\ILXP\settings.ini` |
| `Log` | `TraceSource` with a UTF-8 file listener and optional console listener |

## Markup format

Writers emit lightweight markup that the editor turns into colored tokens:

| Tag | Meaning |
| --- | --- |
| `<<b>>` | keyword/bold (blue) |
| `<<r>>` | string literal (dark red) |
| `<<g>>` | comment (green) |
| `<<#...>>` | link target (navigation key) |
| `<<>>` | reset to default color |

`LanguageWriter.GetReferenceKey` produces the navigation keys, for example
`<<#mscorlib, Version=4.0.0.0...//NSystem//String>>`. `MainForm.GetSelectedNodePath` and
`SelectNodeByPath` use the same format for history and settings, so links, history and the
saved selection all share one address syntax.

## Threading and error handling

* `UiThread.Invoke` runs a delegate on the UI thread, shows the animated loading overlay and
  waits for completion.
* `Program` installs `Application.ThreadException` and `AppDomain.UnhandledException` handlers.
* Every file-system, metadata and native operation is wrapped in `try/catch` and logged; the
  GUI shows a message box only for user-visible failures.
* `BorderPanel` catches paint failures so a broken renderer cannot kill the window.

## CLI

`CommandLine` is a thin dispatcher over the same components:

* `--decompile` uses the writers and `StripMarkup` to produce plain text files.
* `--resources` extracts embedded resources.
* `--ildasm` / `--ilasm` use `ManagedIlDisassembler` / `ManagedIlAssembler` by default and the
  native Microsoft tools (`Interop.IlTools`) with `--native`.
* `--disasm-x86` uses the managed x86/x64 decoder (`Interop.ManagedDisassembler`); the native
  BeaEngine wrapper remains available for reference.
* `--clean` runs the deobfuscation passes (`Deobfuscation`).
* `--music` converts between MIDI and the text format and can render WAV.
* `--screenshot` renders a window to PNG (used for visual checks and docs).

## Tree behaviour

* `MainForm.BuildAssemblyTree` builds the root (assembly), a lazy `References` node, the
  modules and the `Resources` node.
* Reference and namespace children are created on first expand (`LazyNode` +
  `TreeView.BeforeExpand`), so a large assembly opens instantly.
* Expanding a reference resolves the assembly on disk (`AssemblyResolver`), loads it through
  `AssemblyLoader` and caches it (`assemblyCache`), so you can navigate into a dependency:
  assembly -> module -> namespace -> type -> members.
* Base types and interfaces are clickable (`TypeLink`) and navigate to the declaring type.
* Folders are scanned on a worker thread (`LoadFolder`); results are added in batches on the UI
  thread, with a generation counter so that a second scan cancels the first.

## Music

```
Music/Tracks/*.txt  (embedded)
        │  MidiTextFormat.Parse
        ▼
   MidiSequence ──► ChiptuneSynth.RenderWav ──► MemoryStream ──► SoundPlayer
        ▲
        │  MidiReader / MidiWriter  (.mid files, import/export)
```

`MusicPlayer` renders on a thread-pool thread, keeps one `SoundPlayer` alive and exposes
`IsMuted`, `Volume` and a `StateChanged` event that the About box uses to refresh its button.
`AboutForm` stops playback on close and remembers the last tune, the mute state and the volume.

## Water logo

`UI/WaterLogo.cs` is a height-field simulation:

* `current`/`previous` float arrays hold the surface, updated with the classic
  `new = (sum of neighbours)/2 - previous` recurrence and a damping factor.
* Clicks and drags add gaussian splashes; a timer steps the simulation only while there is
  energy left, so an idle About box costs nothing.
* `RenderFrame` displaces every pixel along the surface gradient and adds a slope-based
  highlight, then blits the result through `LockBits`.
* `Advance(frames)` allows deterministic rendering for screenshots and tests.

## Deobfuscation passes

`DeobfuscationPipeline` currently runs:

1. `SizeOfReplacePass` – folds `sizeof(T)` for primitives/blittable value types.
2. `EmptyTypesPass` – folds `Type.EmptyTypes.Length` to `0`.
3. `NopRemoverPass` – removes unreferenced NOPs (branch/handler targets are preserved).

The passes are conservative by design: unknown layouts and unreferenced targets are left
alone. The next passes to port from `refs/Unscrambler` are `MathReplace`,
`ConvertToIntReplace`, `DoubleParseReplace`, `CalliReplace`, `HideCallsRemover`,
`LocalsToFieldRemover` and `InterfaceLoopRemover`.

## Compatibility notes

* Target framework is `net40`; the code avoids APIs newer than .NET 4.0.
* `DebugType=full` keeps the emitted PDB readable by Mono.Cecil 0.9.6 (portable PDBs are not
  supported by that Cecil version).
* Mono.Cecil and Mono.Cecil.Pdb are decompiled into `src/Cecil` and `src/Cecil.Pdb`. The
  `InternalsVisibleTo` attributes were changed to unsigned variants because the decompiled
  assemblies are not strong-named.
* `StatementDecompiler` still contains decompiler-generated member names (`smethod_N`); the
  public entry points are renamed, the internals are pending.
