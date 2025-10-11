## [0.0.6] - 2025-10-11

### 🐛 Bug Fixes

- Apply compound variants slots even if class is set
## [0.0.5] - 2025-10-11

### 🐛 Bug Fixes

- Wrong typing of compound variants causing classes to be applied only to Base slot
- Gracefully handle nullable ClassValue, SlotCollection and Variant as either empty or empty string

### ⚙️ Miscellaneous Tasks

- Removed implicit conversion to string in favor of "ToString" override
- Added some tests, code organization
- Bump version, update CHANGELOG.md
## [0.0.4] - 2025-10-10

### 🐛 Bug Fixes

- Now slots can be unset, accessing them returns a null string

### ⚙️ Miscellaneous Tasks

- Bump version, update CHANGELOG.md
## [0.0.3] - 2025-10-10

### 🐛 Bug Fixes

- Null slots were ignored and empty class values throws error

### ⚙️ Miscellaneous Tasks

- Bump version, update CHANGELOG.md
## [fix-append-ordering] - 2025-10-03

### 🚀 Features

- Linked class property to base slot
- Removed cssbuilder as it can be replaced with stringbuilder
- Made TvFunctions into an injectable service called TwVariants, moved preloading inside the TvOptions constructor to avoid calling it multiple times
- Removed cssbuilder as it can be replaced with stringbuilder
- Added slots override generation to IncrementalGenerator
- Removed cssbuilder as it can be replaced with stringbuilder
- Packaging source generator with the main nuget
- Add possibility to initialize a slot collection as string array
- Renamed TvOptions to TvDescriptor

### 🐛 Bug Fixes

- Migrations issues
- Changed SlotMap name to SlotsMap
- Merge conflicts
- Merge conflicts
- Merge conflicts
- Merge conflicts
- Avoid the consumer instantiating TvOptions through the initializer
- Adding type parameter constraints
- Merge conflicts
- Merge conflicts
- Class variants were overridden by base variants

### 🚜 Refactor

- Renamed projects TailwindVariants.NET.*

### 📚 Documentation

- Added missing to public members
- Added missing to public members

### ⚙️ Miscellaneous Tasks

- Renamed class to follow every other "Slots"
- Added/updated XML documentation
- Updated XML documentation
- Updated documentation
- Update version
- Updating version
- Code formatting
- Updated version, updated CHANGELOG.md
- Updated version, updated CHANGELOG.md
