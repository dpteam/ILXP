# Roadmap

Status legend: `[x]` done, `[~]` in progress / partially done, `[ ]` planned.

## Phase 1 вЂ“ restore the original application (done)

- [x] Initialise git and a Visual Studio `.gitignore`.
- [x] Decrypt and inline the obfuscated string table (354 strings, `tools/Decrypt-Strings.ps1`).
- [x] Reconstruct the project as an SDK-style solution targeting `net40`.
- [x] Recover the application icon from `refs/239.ico` в†’ `ILXP.ico`.
- [x] Extract the designer resources from the lost `.resx` files into `src/ILXP/Resources`.
- [x] Replace the missing `My.Settings`/`My.Resources` with `AppSettings`/`AppResources`.
- [x] Reconstruct the missing token list type and the VB application framework.
- [x] Rename all types and files (`GClassN` в†’ meaningful names).
- [x] Rewrite the UI layer (`MainForm`, `AboutForm`, `CodeEditor`, `BorderPanel`,
      `UiThread`) with named members, properties and event handlers.
- [x] Logging through `Trace` with a UTF-8 file listener and an optional console listener.
- [x] Command line mode with `--list`, `--decompile`, `--resources`, `--ildasm`, `--ilasm`,
      `--disasm-x86`, `--clean`.
- [x] Embed Mono.Cecil 0.9.6.4 and Mono.Cecil.Pdb as decompiled source projects.
- [x] Reverse engineer and integrate the four binaries from `refs/_MayBeNeedReverse`
      (see `REVERSE_ENGINEERING.md`).
- [x] Baseline support for damaged inputs: `AssemblyLoader` carves a valid PE image out of a
      larger blob (memory dump, appended data) and retries (verified with `tools/Test-Carving.ps1`).

## Phase 2 вЂ“ finish the deobfuscation of the source (in progress)

- [~] Rename the remaining `smethod_N` helpers in `StatementDecompiler` (public entry points
      are done; internal helpers still use decompiler names).
- [~] Replace `Microsoft.VisualBasic` helpers (`Operators.CompareString`, `ProjectData`,
      `Conversions`, `Interaction.IIf`, `StaticLocalInitFlag`) with plain C#.
- [~] Replace `ArrayList` with `List<Instruction>`/`List<string>` and remove `checked` noise
      where it is not required for behaviour.
- [ ] Remove dead code left by the VB compiler (`_003C`/`_0024` method names, unreachable
      branches).
- [ ] Normalise formatting (tabs в†’ spaces, file headers) once the member names are final.

## Phase 3 вЂ“ merge functionality from `refs`

Dependency order matters: de4dot-family sources need **dnlib**; Unscrambler needs
**AsmResolver**; dnSpy needs a modern WPF/.NET stack and must be ported selectively.

### 3.1 Libraries

- [ ] Download dnlib (net40-compatible release), decompile with ILSpyCmd, embed as
      `src/dnlib` and add to the solution.
- [ ] Embed AsmResolver (already present as decompiled source in `refs/AsmResolver*`) as
      `src/AsmResolver` and make it compile on `net40`.
- [x] Mono.Cecil / Mono.Cecil.Pdb embedded as source.
- [ ] Decide whether to merge the resulting assemblies into a single executable
      (ILRepack-style) or keep them side by side.

### 3.2 de4dot

- [ ] Port `de4dot.blocks` (control-flow graph, blocks, emulation helpers).
- [ ] Port `de4dot.code.renamer` (heuristic renaming of types/methods/fields/properties).
- [ ] Port the deobfuscator plug-ins in priority order: Confuser/ConfuserEx, .NET Reactor,
      Eazfuscator, SmartAssembly, Babel, CryptoObfuscator, Dotfuscator, Xenocode, ...
- [ ] Port `AssemblyData` (string decrypter + method rewriter service).
- [ ] Port `de4dot.mdecrypt` (native method decryption support).

### 3.3 Unscrambler

- [x] Port `SizeOfReplace`, `EmptyTypeReplace`, `NopRemover` as Cecil passes.
- [x] Port `MathReplace` (constant folding of `System.Math` calls); verified with
      `tools/Test-MathPass.ps1` (`Math.Abs(-5)` → `5`, `Math.Sqrt(16.0)` → `4.0`).
- [ ] Port `ConvertToIntReplace`, `DoubleParseReplace`, `CalliReplace`, `HideCallsRemover`,
      `LocalsToFieldRemover`, `InterfaceLoopRemover`.
- [ ] Add a GUI "Clean assembly" action with a pass selection dialog.

### 3.4 dnSpy / ILSpy

- [ ] Port the ILSpy-style C# decompiler pipeline (the current C# output is a declaration
      outline plus statement-level bodies).
- [ ] Port dnSpy's hex editor view and metadata tables view.
- [ ] Port dnSpy's assembly explorer features (search, bookmarks, tabs).
- [ ] Port dnSpy's BAML/resources viewers where they fit the `net40`/WinForms architecture.

## Phase 4 вЂ“ robustness and damaged inputs

- [x] Baseline PE carving: `AssemblyLoader` searches a blob for a valid PE header and retries.
- [ ] Read assemblies from raw memory dumps (search for the `MZ`/PE header inside a blob).
- [ ] Read assemblies with a damaged metadata root (partial tables, bad RVAs).
- [ ] Fall back to a raw PE/IL reader when Cecil refuses to open the file.
- [ ] Tolerate missing references (resolve lazily, never crash the tree).
- [ ] Fuzz the tree builder and the writers with truncated/corrupted assemblies.
- [ ] Preserve the original file bytes and expose them in the hex viewer.

## Phase 5 вЂ“ quality

- [ ] Add automated tests (golden IL output, string decryption, PE parsing, passes).
- [ ] Add a smoke test that loads a set of assemblies and renders every node.
- [ ] Improve the C#/VB writers: attributes, generics, events, constructors, string escaping.
- [ ] Localisation-ready UI strings.
- [ ] Verify Windows XP compatibility (no APIs newer than .NET 4.0, no DPI-only APIs).

## Phase 6 вЂ“ preservation extras

- [ ] Batch mode: decompile a directory tree into IL/C#/VB.
- [ ] Diff two assembly versions (metadata + IL).
- [ ] Export a "preservation bundle": original bytes, IL, resources, metadata report.
- [ ] Document the original CodeReflect behaviour and the differences introduced by ILXP.
