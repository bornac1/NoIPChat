dotnet build Server --runtime win-x64 -o ".\Release\Server\win-x64" --verbosity minimal
dotnet build Server --runtime win-x86 -o ".\Release\Server\win-x86" --verbosity minimal
dotnet build Server --runtime win-arm64 -o ".\Release\Server\win-arm64" --verbosity minimal
Compress-Archive -Path ".\Release\Server\win-x64", ".\Release\Server\win-x86", ".\Release\Server\win-arm64" -DestinationPath ".\Release\Windows server.zip" -Force
dotnet build Server --runtime linux-x64 -o ".\Release\Server\linux-x64" --verbosity minimal
dotnet build Server --runtime linux-musl-x64 -o ".\Release\Server\linux-musl-x64" --verbosity minimal
dotnet build Server --runtime linux-musl-arm64 -o ".\Release\Server\linux-musl-arm64" --verbosity minimal
dotnet build Server --runtime linux-arm -o ".\Release\Server\linux-arm" --verbosity minimal
dotnet build Server --runtime linux-arm64 -o ".\Release\Server\linux-arm64" --verbosity minimal
Compress-Archive -Path ".\Release\Server\linux-x64", ".\Release\Server\linux-musl-x64", ".\Release\Server\linux-musl-arm64", ".\Release\Server\linux-arm", ".\Release\Server\linux-arm64" -DestinationPath ".\Release\Linux server.zip" -Force
dotnet build Client --runtime win-x64 -o ".\Release\Client\win-x64" --verbosity minimal
dotnet build Client --runtime win-x86 -o ".\Release\Client\win-x86" --verbosity minimal
dotnet build Client --runtime win-arm64 -o ".\Release\Client\win-arm64" --verbosity minimal
Compress-Archive -Path ".\Release\Client\win-x64", ".\Release\Client\win-x86", ".\Release\Client\win-arm64" -DestinationPath ".\Release\Windows client.zip" -Force