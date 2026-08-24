# Publishes PatasePasseios.Desktop as a single self-contained win-x64 .exe.
# Run from anywhere; the script cd's into its own directory first.
#
#   dotnet clean ..\..\PatasePasseios.sln -c Release   # if a previous publish is stale

Set-Location $PSScriptRoot

dotnet publish `
    PatasePasseios.Desktop.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=embedded `
    -p:DebugSymbols=true
