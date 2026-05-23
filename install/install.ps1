param (
    [string]$Version,
    [string]$VectraCtlRootPath = "$Env:SystemDrive\vectractl"
)

$ErrorActionPreference = 'stop'

Write-Output ""

# Constants
$VectraCtlFileName = "vectractl.exe"
$VectraCtlFilePath = Join-Path $VectraCtlRootPath $VectraCtlFileName

# GitHub Org and repo hosting VectraCtl
$GitHubOrg = "cortexiumlabs"
$GitHubRepo = "vectractl"

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

# Check if VectraCtl is installed.
if (Test-Path $VectraCtlFilePath -PathType Leaf) {
    Write-Warning "VectraCtl is detected - $VectraCtlFilePath"
    & $VectraCtlFilePath version
    Write-Output "Reinstalling VectraCtl..."
}
else {
    Write-Output "Installing VectraCtl..."
}

# Create VectraCtl Directory
Write-Output "Creating $VectraCtlRootPath directory"
New-Item -ErrorAction Ignore -Path $VectraCtlRootPath -ItemType "directory"
if (!(Test-Path $VectraCtlRootPath -PathType Container)) {
    Write-Warning "Please visit https://github.com/cortexiumlabs/vectractl for instructions on how to install without admin rights."
    throw "Cannot create $VectraCtlRootPath"
}

# Get the list of release from GitHub
$releaseJsonUrl = "https://api.github.com/repos/${GitHubOrg}/${GitHubRepo}/releases"

$releases = Invoke-RestMethod -Headers $githubHeader -Uri $releaseJsonUrl -Method Get
if ($releases.Count -eq 0) {
    throw "No releases from github.com/cortexiumlabs/vectractl repo"
}

# Get latest or specified version info from releases
function GetVersionInfo {
    param (
        [string]$Version,
        $Releases
    )
    # Filter windows binary and download archive
    if (!$Version) {
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
    $windowsAsset = $Release | Select-Object -ExpandProperty assets | Where-Object { $_.name -Like "*windows-x64.zip" }
    if (!$windowsAsset) {
        throw "Cannot find the windows VectraCtl binary"
    }
    [hashtable]$return = @{}
    $return.url = $windowsAsset.url
    $return.name = $windowsAsset.name
    return $return

}

$release = GetVersionInfo -Version $Version -Releases $releases
if (!$release) {
    throw "Cannot find the specified VectraCtl binary version"
}
$asset = GetWindowsAsset -Release $release
$zipFileUrl = $asset.url
$assetName = $asset.name

$zipFilePath = $VectraCtlRootPath + "\" + $assetName
Write-Output "Downloading $zipFileUrl ..."

$githubHeader.Accept = "application/octet-stream"
$oldProgressPreference = $global:ProgressPreference
$global:ProgressPreference = 'SilentlyContinue'
Invoke-WebRequest -Headers $githubHeader -Uri $zipFileUrl -OutFile $zipFilePath
$global:ProgressPreference = $oldProgressPreference
if (!(Test-Path $zipFilePath -PathType Leaf)) {
    throw "Failed to download VectraCtl binary - $zipFilePath"
}

# Extract VectraCtl to $VectraCtlRootPath
Write-Output "Extracting $zipFilePath..."
Microsoft.Powershell.Archive\Expand-Archive -Force -Path $zipFilePath -DestinationPath $VectraCtlRootPath
if (!(Test-Path $VectraCtlFilePath -PathType Leaf)) {
    throw "Failed to extract VectraCtl archive - $zipFilePath"
}

# Check the VectraCtl version
# Invoke-Expression "$VectraCtlFilePath version"

# Clean up zipfile
Write-Output "Clean up $zipFilePath..."
Remove-Item $zipFilePath -Force

# Add VectraCtlRootPath directory to User Path environment variable
Write-Output "Try to add $VectraCtlRootPath to User Path Environment variable..."
$UserPathEnvironmentVar = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($UserPathEnvironmentVar -like '*vectractl*') {
    Write-Output "Skipping to add $VectraCtlRootPath to User Path - $UserPathEnvironmentVar"
}
else {
    [System.Environment]::SetEnvironmentVariable("PATH", $UserPathEnvironmentVar + ";$VectraCtlRootPath", "User")
    $UserPathEnvironmentVar = [Environment]::GetEnvironmentVariable("PATH", "User")
    Write-Output "Added $VectraCtlRootPath to User Path - $UserPathEnvironmentVar"
}

Write-Output ""
Write-Output "VectraCtl is installed successfully."
Write-Output "To get started with VectraCtl, please visit https://github.com/cortexiumlabs/vectractl."