# KODING.md

## Build/Compile Commands
```bash
# To build the project:
unity-builder -projectPath /path/to/project -buildTarget StandaloneWindows64

# If you have a specific build script, use it like this:
./build-unity-project.sh
```

## Linting
For C# code in Unity, consider using tools like CodeMAid or ReSharper.
```bash
# Format and lint C# code (if using Roslyn Analyzers):
dotnet format
```

## Testing
Run all tests (assuming you're using the Unity Test Framework).
```bash
# To run all tests:
unity-test -projectPath /path/to/project

# To run a specific test suite or test method, specify it in your unity-test command.
```

## Code Style Guidelines
1. **Imports**: Use `using` directives at the top of files, organized alphabetically.
2. **Formatting**: Follow C# conventions (4 spaces for indentation).
3. **Naming Conventions**: 
   - Classes: PascalCase (`MyClass`)
   - Methods/Variables: camelCase (`myVariable`)
4. **Error Handling**: Use try-catch blocks where necessary and log errors appropriately.

## Dependencies
The project uses:
- Anon Kode (CLI for coding)
  ```json
  "anon-kode": "^0.0.53"
  ```
