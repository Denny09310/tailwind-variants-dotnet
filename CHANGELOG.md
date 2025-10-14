## [0.2.0] - 2025-10-14

### 🚀 Features

- Added slot inheritance, making ISlots methods virtual/override based on the class modifiers

### 🐛 Bug Fixes

- Adding GetName with string as key to use with *SlotsNames class constants
- Merge conflicts
- Docs not importing App.razor
- Naming and qualification was wrong when generating
- Changed extensions class name
- Added space after nested components

### ⚙️ Miscellaneous Tasks

- Code formatting
- Split tests in multiple files
- Revert changes "SlotAttribute"
- Updated generator pipeline to filter out empty classes and not partial classes, removed diagnostics
- Code formatting, added .editorconfig
- Modified editorconfig
- Removed IsPartial from SlotsAccessorToGenerate
- Revert to old naming convention
- Fixed README.md to account for latest changes
## [0.1.0] - 2025-10-12

### 🚀 Features

- Added source generated SlotAttribute
- Added slot names const helpers
- Centralized slot name retrieval
- Added utility extension method to get name of slot

### 🐛 Bug Fixes

- Wrong source generator namespace

### ⚙️ Miscellaneous Tasks

- Merged source generator test project with main
- Added slots attribute to test it
- Bump version, update CHANGELOG.md
## [0.0.6] - 2025-10-11

### 🐛 Bug Fixes

- Apply compound variants slots even if class is set

### ⚙️ Miscellaneous Tasks

- Bump version, update CHANGELOG.md
## [0.0.5] - 2025-10-11

### 🐛 Bug Fixes

- Wrong typing of compound variants causing classes to be applied only to Base slot
- Gracefully handle nullable ClassValue, SlotCollection and Variant as either empty or empty string

### ⚙️ Miscellaneous Tasks

- Added "#pragma" warnings disable for common roslyn warnings
- Removed implicit conversion to string in favor of "ToString" override
- Avoid writing namespace in top-level contexts
- Added some tests, code organization
- Bump version, update CHANGELOG.md
## [0.0.4] - 2025-10-10

### 🐛 Bug Fixes

- Now slots can be unset, accessing them returns a null string

### ⚙️ Miscellaneous Tasks

- Bump version, update CHANGELOG.md
## [0.0.3] - 2025-10-10

### 🚀 Features

- Initial landing page
- Created catch-all route
- Created docs router, added markdown pipeline, add prismjs for syntax highlighting
- Switched to complete SSR, using BlazorJSComponents for lifetime handling
- Added anchorjs
- Added dynamic blur to header
- Improved hero section
- Mobile sidebar view, simplified base tw component slots access
- Added documentation links, added work in progress markdown
- Added sidebar header, fixed styles
- Added initial docs drafts
- Added simple stargazers count
- Added hacky way to force re-render content
- Disabled data-enhance-nav

### 🐛 Bug Fixes

- Mobile docs layout scrolling horizontally
- Button scrolling with component, wrong z-index
- Wrong function declaration
- Javascript function was not bound
- Removing code-blocks on dispose
- Enabled stream rendering to fix wrapping deletion
- Using 'attached' instead of 'setParameters'
- Strange diffing behavior causes duplicate blazor state controls
- Background color not working in light mode
- General docs enhancements
- Null slots were ignored and empty class values throws error

### ⚙️ Miscellaneous Tasks

- Updated README.md
- Updated README.md
- Renamed docs
- Removed polling lookup as now it's imported
- Removed line-numbers
- Tailwind classes sorting
- Updated InvokeAsync(StateHasChanged)
- Fixed button styling home page
- Disabled prismjs DOM update until fix is found
- Bump version, update CHANGELOG.md
## [fix-append-ordering] - 2025-10-03

### 🐛 Bug Fixes

- Class variants were overridden by base variants

### ⚙️ Miscellaneous Tasks

- Updated version, updated CHANGELOG.md
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
