# This script is meant to build binaries for the project and package them into a zip file.

if (-Not (Test-Path -Path ".\SteamElevatorLite") -Or -Not (Test-Path -Path ".\SELCommander")) {
    Write-Output "This script must be run from the root of the project."
    Exit
}

# Build SEL binaries in release mode
Write-Output "Building SEL binaries..."
Set-Location .\SteamElevatorLite
dotnet.exe build -c Release

Set-Location ..\SELCommander
dotnet.exe build -c Release

# Return to root of project
Set-Location ..

# Make sure the binaries folder exists
$folderPath = ".\SELBinaries"
if (-Not (Test-Path -Path $folderPath)) {
    New-Item -ItemType Directory -Path $folderPath
}

Wrire-Output "Copying binaries to $folderPath..."
# Copy the binaries to the binaries folder
Copy-Item -Recurse -Path ".\SteamElevatorLite\bin\Release\net8.0-windows\*" -Destination $folderPath
Copy-Item -Recurse -Path ".\SELCommander\bin\Release\net9.0\*" -Destination $folderPath

Write-Output "Creating zip file..."
# Create a zip file
$zipPath = ".\SteamElevatorLite.zip"
if (Test-Path -Path $zipPath) {
    Remove-Item -Path $zipPath
}

Compress-Archive -Path ".\SELBinaries" -DestinationPath $zipPath -CompressionLevel NoCompression