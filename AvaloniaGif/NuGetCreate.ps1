dotnet build -c release
dotnet nuget sign .\bin\release\AvaloniaGif-Fork.1.0.1.nupkg --certificate-fingerprint 77912c00dc8891c94c4df9afc19f480ed33e66a7 --timestamper http://timestamp.digicert.com
dotnet nuget verify --all .\bin\release\AvaloniaGif-Fork.1.0.1.nupkg
