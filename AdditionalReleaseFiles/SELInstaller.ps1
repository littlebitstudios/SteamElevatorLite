if (-Not (Test-Path -Path ".\SteamElevatorLite.exe") -Or -Not (Test-Path -Path ".\SELCommander.exe")) {
    Write-Output "One of the programs was not found. This script is meant to be run from the packaged software directory."
    Exit
}

$folderPath = "$env:USERPROFILE\userapps\SteamElevatorLite"

Write-Output "This script will move the files for the program to $folderPath. Continue? (Y/N)"
$confirm = Read-Host
if ($confirm -ne "Y") {
    Write-Output "Exiting..."
    Exit
}

# Make sure the folder exists
if (-Not (Test-Path -Path $folderPath)) {
    New-Item -ItemType Directory -Path $folderPath
}

# Copy the binaries to the userapps folder
Copy-Item -Recurse -Path ".\*" -Destination $folderPath

Write-Output "Files have been copied to $folderPath."
Write-Output "Do you want SteamElevatorLite to run on startup? (Y/N)"

$confirm = Read-Host
if ($confirm -eq "Y") {
    $startupPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\SteamElevatorLite.lnk"
    $shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($startupPath)
    $shortcut.TargetPath = "$folderPath\SteamElevatorLite.exe"
    $shortcut.Save()
    Write-Output "SteamElevatorLite will now run on startup."
}

Write-Output "Installation complete."
Write-Output "Run the software now? (Y/N)"

$confirm = Read-Host
if ($confirm -eq "Y") {
    Start-Process "$folderPath\SteamElevatorLite.exe"
}