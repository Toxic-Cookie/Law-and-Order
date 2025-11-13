# Law and Order - RimWorld 1.6 Mod

## Project Purpose
This is a RimWorld 1.6 mod project. Your primary purpose is to serve as a helpful assistant for questions about RimWorld modding, this mod's code, and related development tasks.

## Important Directories

### Local Library Information ###
`C:\Users\Giovanni\source\repos\Law and Order\Law and Order\llms`

This folder contains locally available information about the libraries being used in this mod. Reference this when questions arise about mod-specific libraries and dependencies.

### Deployed Mod Folder ###
`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Law and Order`

This folder is where the mod gets deployed to. Any file changes should NOT be made here as they will be overwritten by the files in the project during the build. Although, residual files could be here and potentially interfere with new changes.

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

- When answering questions, prefer checking the RimWorld source code directory for accurate information about game systems
- Consult the llms directory for information about mod-specific libraries
- Provide code examples that are compatible with RimWorld 1.6 and follow RimWorld modding conventions
- Be familiar with Harmony patching, RimWorld's def system, and C# modding patterns
- When modding in functionality, prefer to use libraries / frameworks where possible such as HugsLib to reduce manual implementation overhead.
- When making major changes, there's no need to support legacy functionality since this mod is in active development. It should be assumed that a new game save will always be used for testing. Make sure to advise the user when a breaking change is made that would require a new game save.
- Refer to and document changes in `C:\Users\Giovanni\source\repos\Law and Order\Law and Order\llms\docs\Project_Documentation.md` and `C:\Users\Giovanni\source\repos\Law and Order\Law and Order\llms\docs\Project_Tracker.md` respectively.
- When working on something like a feature use `C:\Users\Giovanni\source\repos\Law and Order\Law and Order\llms\docs\Project_Notepad.md` as a notepad to keep track of what you're doing. This is important to resist context compactions ruining your workflow.
- When making UI strings, call .Translate() on them and ensure a corresponding key in the XML `C:\Users\Giovanni\source\repos\Law and Order\Law and Order\Languages\English\Keyed\LawAndOrder_Keys.xml` exists.