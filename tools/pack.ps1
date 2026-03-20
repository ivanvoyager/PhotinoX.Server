$csproj = "$PSScriptRoot\..\Photino.NET.Server\PhotinoX.Server.csproj"
$Configuration = "Release"
$outDir = $PSScriptRoot

dotnet clean $csproj -c $Configuration
dotnet build $csproj -c $Configuration
dotnet pack $csproj -c $Configuration -o $outDir