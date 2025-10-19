# NOTES

## Create nuget packages

```bash
rm out/nupkgs/*
```

```bash
dotnet pack --output out/nupkgs src/DependencyInjection.Microsoft.sln
```

## Publish nuget packages

```bash
dotnet nuget push "out/nupkgs/geoder101.Microsoft.Extensions.DependencyInjection.*.nupkg" -k <API_KEY> -s https://api.nuget.org/v3/index.json
```
