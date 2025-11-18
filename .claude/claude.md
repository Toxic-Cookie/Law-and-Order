# Law and Order - RimWorld 1.6 Mod

## Project Purpose
This is a RimWorld 1.6 mod project. Your primary purpose is to serve as a helpful assistant for questions about RimWorld modding, this mod's code, and related development tasks.

## Documentation

### GitHub Wiki (Primary Documentation Source)
**Location**: `C:\Users\Giovanni\source\repos\Law-and-Order.wiki`

**The GitHub Wiki is the SINGLE SOURCE OF TRUTH for all project documentation.**

All design documents, implementation tracking, research, and references have been consolidated into a comprehensive wiki with 43+ pages covering:
- Getting Started & Quick Start guides
- Game Systems (Crime Detection, Investigation, Evidence, Trials, Punishment, etc.)
- Design Philosophy & Technical Specifications
- Implementation Roadmap & Phase Tracking
- Dwarf Fortress Research (Justice, Crime, Intrigue, Punishment systems)
- Development Setup, Contributing Guidelines, Code Patterns
- Complete Glossary & Reference Materials

**Important**: Always refer to the wiki for project information. Do NOT create new documentation files in the llms directory.

### Wiki Maintenance Guidelines

When working on features or making changes:

1. **Update Relevant Wiki Pages**: After completing features or making significant changes, update the corresponding wiki pages:
   - **Current-Status.md** - Update phase completion percentages and implemented features
   - **Phase-Tracking.md** - Mark phases as complete and update timelines
   - **Changelog.md** - Document all changes, breaking changes, and new features
   - Specific system pages (e.g., Crime-Detection.md, Evidence-System.md) - Add new mechanics or update implementation details

2. **Create New Wiki Pages** when adding entirely new systems or major features:
   - Follow the existing naming convention (Kebab-Case.md)
   - Add the page to _Sidebar.md under the appropriate category
   - Cross-link to related pages
   - Include code examples and technical details

3. **Keep Wiki Organized**:
   - Use clear headings and structure
   - Include code examples where applicable
   - Add cross-references between related pages
   - Keep pages under 1000 lines for readability (split into multiple pages if needed)

4. **What NOT to Document**:
   - DO NOT create temporary working notes in the wiki
   - DO NOT document every minor code change
   - DO NOT create duplicate pages

### Local Library Information
`C:\Users\Giovanni\source\repos\Law and Order\Law and Order\llms`

This folder contains locally available information about the libraries being used in this mod:
- `llms_harmony.txt` - Harmony patching library reference
- `llms_hugslib.txt` - HugsLib framework reference
- `logs/` - Build and debug logs

Reference these files when questions arise about mod-specific libraries and dependencies.

### Deployed Mod Folder
`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Law and Order`

This folder is where the mod gets deployed to.

**IMPORTANT**:
- **Never make file changes directly in this folder** - they will be overwritten on the next build
- **Never manually copy files to this folder** - the build process handles all deployment automatically
- **The build process guarantees a clean slate** - the entire deployment directory is wiped and rebuilt from scratch on every build
- No residual files will interfere with changes since the automated clean build removes everything before copying fresh files

## RimWorld Source Code

Use this as a reference when:
- Understanding RimWorld's internal systems and APIs
- Looking up base game classes, methods, and fields
- Checking how vanilla RimWorld implements features
- Debugging compatibility issues

`C:\Users\Giovanni\source\repos\Rimworld\V_1_6\Assembly-CSharp`
This folder contains the decompiled RimWorld 1.6 source code (Assembly-CSharp).

`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data`
`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Source`
These folders contain Rimworld's XML.

## Guidelines

- When asked to perform a task, make sure to ask questions until you're at least 95% confident of how to proceed.
- When answering questions, prefer checking the **GitHub Wiki** for project-specific information and the RimWorld source code directory for accurate information about game systems.
- Consult the llms directory for information about mod-specific libraries (Harmony, HugsLib).
- Provide code examples that are compatible with RimWorld 1.6 and follow RimWorld modding conventions.
- Be familiar with Harmony patching, RimWorld's def system, and C# modding patterns.
- When implementing functionality, prefer to use libraries/frameworks where possible such as HugsLib to reduce manual implementation overhead. Make sure to search the RimWorld source code for proven patterns as a reference to achieve desired functionality as much as possible.
- When making major changes, there's no need to support legacy functionality since this mod is in active development. It should be assumed that a new game save will always be used for testing. Make sure to advise the user when a breaking change is made that would require a new game save.
- After implementing features or making significant changes, update the relevant wiki pages (see Wiki Maintenance Guidelines above).
- When making UI strings, call .Translate() on them and ensure a corresponding key in the XML `C:\Users\Giovanni\source\repos\Law and Order\Law and Order\Languages\English\Keyed\LawAndOrder_Keys.xml` exists.

## Common Development Workflow

1. **Before starting**: Check relevant wiki pages to understand the current implementation
2. **During development**:
   - Reference Code-Patterns.md wiki page for common patterns
   - Check Architecture-Overview.md for system structure
   - Follow guidelines in Contributing-Guide.md
3. **After completing**:
   - Update Current-Status.md with new features
   - Update relevant system pages (e.g., Crime-Detection.md, Evidence-System.md)
   - Add entry to Changelog.md
   - Update Phase-Tracking.md if completing a phase milestone
4. **For new systems**: Create a new wiki page with complete documentation
