# Contributing to mesh-section-loops

Thank you for your interest in contributing!

## Code of Conduct

All contributors and participants are expected to maintain a constructive, respectful, and professional environment.

## Development Workflow

1. **Prerequisites**:
   - .NET 10.0 SDK
   - C# 14.0

2. **Build and Test**:
   ```bash
   dotnet build MeshSectionLoops.sln
   dotnet run --project tests/MeshSectionLoops.Tests/MeshSectionLoops.Tests.csproj
   dotnet run --project src/MeshSectionLoops.Cli/MeshSectionLoops.Cli.csproj -- demo --assert
   ```

3. **Submitting Changes**:
   - Maintain documentation integrity in English (`README.md`) and Portuguese (`README.pt-BR.md`).
   - Keep pull requests focused, verified with offline test execution.
