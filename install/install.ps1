param (
    [string]$Version,
    [string]$SynCtlRootPath = "$Env:SystemDrive\synctl"
)

$ErrorActionPreference = 'stop'

Write-Output ""

# Constants
$SynCtlFileName = "synctl.exe"
$SynCtlFilePath = Join-Path $SynCtlRootPath $SynCtlFileName

# GitHub Org and repo hosting SynCtl
$GitHubOrg = "synentra"
$GitHubRepo = "synctl"

# Set Github request authentication for basic authentication.
if ($Env:GITHUB_USER) {
    $basicAuth = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($Env:GITHUB_USER + ":" + $Env:GITHUB_TOKEN));
    $githubHeader = @{"Authorization" = "Basic $basicAuth" }
}
else {
    $githubHeader = @{}
}

if ((Get-ExecutionPolicy) -gt 'RemoteSigned' -or (Get-ExecutionPolicy) -eq 'ByPass') {
    Write-Output "PowerShell requires an execution policy of 'RemoteSigned'."
    Write-Output "To make this change please run:"
    Write-Output "'Set-ExecutionPolicy RemoteSigned -scope CurrentUser'"
    exit 1
}

# Change security protocol to support TLS 1.2 / 1.1 / 1.0 - old powershell uses TLS 1.0 as a default protocol
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls11 -bor [Net.SecurityProtocolType]::Tls

# Check if SynCtl is installed.
if (Test-Path $SynCtlFilePath -PathType Leaf) {
    Write-Warning "SynCtl is detected - $SynCtlFilePath"
    & $SynCtlFilePath version
    Write-Output "Reinstalling SynCtl..."
}
else {
    Write-Output "Installing SynCtl..."
}

# Create SynCtl Directory
Write-Output "Creating $SynCtlRootPath directory"
New-Item -ErrorAction Ignore -Path $SynCtlRootPath -ItemType "directory"
if (-not (Test-Path $SynCtlRootPath -PathType Container)) {
    Write-Warning "Please visit https://github.com/synentra/synctl for instructions on how to install without admin rights."
    throw "Cannot create $SynCtlRootPath"
}

# Get the list of release from GitHub
$releaseJsonUrl = "https://api.github.com/repos/${GitHubOrg}/${GitHubRepo}/releases"

$releases = Invoke-RestMethod -Headers $githubHeader -Uri $releaseJsonUrl -Method Get
if ($releases.Count -eq 0) {
    throw "No releases from github.com/synentra/synctl repo"
}

# Get latest or specified version info from releases
function GetVersionInfo {
    param (
        [string]$Version,
        $Releases
    )
    # Filter windows binary and download archive
    if (-not $Version) {
        $release = $Releases | Where-Object { $_.tag_name -notlike "*rc*" } | Select-Object -First 1
    }
    else {
        $release = $Releases | Where-Object { $_.tag_name -eq "v$Version" } | Select-Object -First 1
    }

    return $release
}

# Get info about windows asset from release
function GetWindowsAsset {
    param (
        $Release
    )
    $windowsAsset = $Release | Select-Object -ExpandProperty assets | Where-Object { $_.name -like "*windows-x64.zip" }
    if (-not $windowsAsset) {
        throw "Cannot find the windows SynCtl binary"
    }
    [hashtable]$return = @{}
    $return.url = $windowsAsset.url
    $return.name = $windowsAsset.name
    return $return

}

$release = GetVersionInfo -Version $Version -Releases $releases
if (-not $release) {
    throw "Cannot find the specified SynCtl binary version"
}
$asset = GetWindowsAsset -Release $release
$zipFileUrl = $asset.url
$assetName = $asset.name

$zipFilePath = $SynCtlRootPath + "\" + $assetName
Write-Output "Downloading $zipFileUrl ..."

$githubHeader.Accept = "application/octet-stream"
$oldProgressPreference = $global:ProgressPreference
$global:ProgressPreference = 'SilentlyContinue'
Invoke-WebRequest -Headers $githubHeader -Uri $zipFileUrl -OutFile $zipFilePath
$global:ProgressPreference = $oldProgressPreference
if (-not (Test-Path $zipFilePath -PathType Leaf)) {
    throw "Failed to download SynCtl binary - $zipFilePath"
}

# Extract SynCtl to $SynCtlRootPath
Write-Output "Extracting $zipFilePath..."
Microsoft.Powershell.Archive\Expand-Archive -Force -Path $zipFilePath -DestinationPath $SynCtlRootPath
if (-not (Test-Path $SynCtlFilePath -PathType Leaf)) {
    throw "Failed to extract SynCtl archive - $zipFilePath"
}

# Check the SynCtl version
# Invoke-Expression "$SynCtlFilePath version"

# Clean up zipfile
Write-Output "Clean up $zipFilePath..."
Remove-Item $zipFilePath -Force

# Add SynCtlRootPath directory to User Path environment variable
Write-Output "Try to add $SynCtlRootPath to User Path Environment variable..."
$UserPathEnvironmentVar = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($UserPathEnvironmentVar -like '*synctl*') {
    Write-Output "Skipping to add $SynCtlRootPath to User Path - $UserPathEnvironmentVar"
}
else {
    [System.Environment]::SetEnvironmentVariable("PATH", $UserPathEnvironmentVar + ";$SynCtlRootPath", "User")
    $UserPathEnvironmentVar = [Environment]::GetEnvironmentVariable("PATH", "User")
    Write-Output "Added $SynCtlRootPath to User Path - $UserPathEnvironmentVar"
}

Write-Output ""
Write-Output "SynCtl is installed successfully."
Write-Output "To get started with SynCtl, please visit https://github.com/synentra/synctl."