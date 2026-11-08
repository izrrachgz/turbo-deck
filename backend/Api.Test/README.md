# Api.Test

Isolated API, middleware, local storage, and full-host tests. The factory uses a unique temporary SQLite database and a
test control token. Tests cover the machine-credential boundary, health, file lifecycle, storage traversal protection,
and removed-feature architecture constraints.

Run with:

```powershell
dotnet test .\Api.Test\Api.Test.csproj
```
