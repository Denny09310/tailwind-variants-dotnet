## [0.0.2] - 2025-10-03

### 🐛 Bug Fixes

- Class variants were overridden by base variants
- 
## [0.0.1] - 2025-10-02

### 🚀 Features

- Added variant targets, need to review the infrastructure
- Precomputed options without overrides
- Added slot class accessor
- Added slot class overriding, added more examples
- Added slots accessors source generator
- Added #nullable directive
- Added check for existing SlotsMap member
- Removed set properties, added IReadonlyDictionary on SlotMap, added Diagnostic messages, split in file
- Added nuget metadata for both projects
- Linked class property to base slot
- Added textbox
- Initial header, initial styling
- Removed cssbuilder as it can be replaced with stringbuilder
- Made TvFunctions into an injectable service called TwVariants, moved preloading inside the TvOptions constructor to avoid calling it multiple times
- Removed cssbuilder as it can be replaced with stringbuilder
- Added slots override generation to IncrementalGenerator
- Removed cssbuilder as it can be replaced with stringbuilder
- Packaging source generator with the main nuget
- Avoid ISymbol caching pitfall, getting only needed data
- Added EquatableArray to enable array caching
- Added simple sidebar, wip for sticking
- Added initial source generator snapshot tests
- Added initial bUnit tests
- Added landing page, created blank layout, added missing error section in layout
- Added home page, added docs routes and layout, added card and button components, updated README.md
- Add possibility to initialize a slot collection as string array
- Renamed TvOptions to TvDescriptor

### 🐛 Bug Fixes

- Follow tailwind-variants docs naming
- Wrong slotMember generation
- Getting the name of the conaining type instead of symbol, removing any traling "Slots" from name
- Wrong analyzers file
- Wrong example in README.md
- Migrations issues
- Changed SlotMap name to SlotsMap
- Merge conflicts
- Merge conflicts
- Merge conflicts
- Merge conflicts
- Avoid the consumer instantiating TvOptions through the initializer
- Adding type parameter constraints
- Merge conflicts
- Wrong documentation examples
- Merge conflicts
- Merge conflicts
- Merge conflicts
- Sidebar was not sticky
- Wrong EnforceExtendedAnalyzerRules value
- TvOptions retained in example
- Test projects were removed from solution
- CHANGELOG name was spelled wrong

### 💼 Other

- New infrastructure
- Refactor structure to match tv-variants

### 🚜 Refactor

- Converted wasm standalone into the new server project
- Renamed projects TailwindVariants.NET.*

### 📚 Documentation

- Added missing to public members
- Added missing to public members
- Added main layout

### ⚙️ Miscellaneous Tasks

- Updating README.md
- Updated nuget metadata
- Flattened _variant name
- Changed radii name
- Cleanup old infrastructure
- Removed unused property
- Key/value destructuring
- Split code in files/folders
- Added card (multi-slot) example
- Added complex slotted component example
- Initial sample docs website
- Removed old project folder
- Updated README.md
- Better BodyContent rendering
- Better generator implementation
- Move diagnostic in its own file
- Finished demo navbar
- Renamed package, moved Directory.Build.props
- Updated README.md
- Updated version
- Updated README.md
- Keep it simple for now
- Renamed class to follow every other "Slots"
- Versioning
- Suppressing warning for external compat
- Versioning
- Added active link classes on sidebar
- Updated README.md
- Cleanup docs site
- Added/updated XML documentation
- Split generator in methods for better readability
- Moved property check in its own method
- Updated XML documentation
- Removed unusued method
- Improved source code output
- Updated documentation
- Updated README.md
- Small syntax change
- Code formatting
- Update version
- Updating version
- Enhanced link component
- Code formatting
- Updated version, updated CHANGELOG.md
