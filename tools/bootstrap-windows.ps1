<#
.SYNOPSIS
  Set up a Windows PC for making art for game1 (artists, not programmers). Safe to re-run.

.DESCRIPTION
  Starting from a blank machine, this script:
    1. Installs, via winget, anything that is missing: Git (includes Git LFS and Git Credential
       Manager), GitHub Desktop, GitHub CLI, Unity Hub, Krita and, if winget has it, PureRef
    2. Sets your git name and email (only if they aren't set yet)
    3. Clones the game repo to -TargetDir (or fast-forwards it if it's already there)
    4. Sets this clone's git config the way tools/bootstrap-macos.sh does: Git LFS, tracked hooks
       and the UnityYAMLMerge driver, then pulls the LFS files
    5. Installs the Unity editor version in ProjectSettings/ProjectVersion.txt, with WebGL support
  It never asks for, stores or sends a password or token. Git Credential Manager opens a browser
  window to sign in to GitHub the first time git needs it. It does not install code-only tools
  (Rider, .NET SDK, gitleaks, Claude Code) and does not clone the private dev-history repo.

  At the end it prints what was installed or skipped, and the steps only a person can do
  (Unity sign-in and license, GitHub sign-in, opening the project in Unity Hub).

.PARAMETER GitHubUser
  Your GitHub username. Used for the credential hint (so the sign-in page is pre-filled) and in
  the instructions at the end. Required.

.PARAMETER RepoUrl
  Repository to clone. Default: https://github.com/ChaoticMischief/game1.git

.PARAMETER TargetDir
  Where to clone it. Default: %USERPROFILE%\dev\game1

.PARAMETER GitName
  Name for git commits. Only used if git has no user.name yet; you're asked if it isn't given.

.PARAMETER GitEmail
  Email for git commits. Only used if git has no user.email yet; you're asked if it isn't given.
  Use the email address of your GitHub account (or its noreply address).

.PARAMETER SkipUnity
  Don't install Unity Hub or the Unity editor.

.PARAMETER SkipKrita
  Don't install Krita.

.PARAMETER SkipPureRef
  Don't look for or install PureRef.

.PARAMETER SkipGitHubCli
  Don't install the GitHub CLI (gh). It's optional; GitHub Desktop does the same job for artists.

.PARAMETER DryRun
  Print what would be installed or changed, and change nothing.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\bootstrap-windows.ps1 -GitHubUser octocat

.EXAMPLE
  .\bootstrap-windows.ps1 -GitHubUser octocat -SkipUnity -DryRun
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)] [string] $GitHubUser,
  [string] $RepoUrl = 'https://github.com/ChaoticMischief/game1.git',
  [string] $TargetDir = (Join-Path $env:USERPROFILE 'dev\game1'),
  [string] $GitName,
  [string] $GitEmail,
  [switch] $SkipUnity,
  [switch] $SkipKrita,
  [switch] $SkipPureRef,
  [switch] $SkipGitHubCli,
  [switch] $DryRun
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

# What happened, for the summary at the end.
$script:Installed = New-Object System.Collections.Generic.List[string]
$script:Present = New-Object System.Collections.Generic.List[string]
$script:Skipped = New-Object System.Collections.Generic.List[string]
$script:Manual = New-Object System.Collections.Generic.List[string]

function Write-Step([string] $Message) { Write-Host ""; Write-Host "==> $Message" -ForegroundColor Cyan }
function Write-Note([string] $Message) { Write-Host "    $Message" }
function Write-Warn([string] $Message) { Write-Host "    WARNING: $Message" -ForegroundColor Yellow }

# Make tools installed during this run visible without opening a new window.
function Update-SessionPath {
  $machine = [Environment]::GetEnvironmentVariable('Path', 'Machine')
  $user = [Environment]::GetEnvironmentVariable('Path', 'User')
  $env:Path = (@($machine, $user) | Where-Object { $_ }) -join ';'
}

function Test-Command([string] $Name) {
  return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

# Run a native command and return its output lines. Never throws on a non-zero exit code;
# check $LASTEXITCODE afterwards.
function Invoke-Native([string] $Exe, [string[]] $Arguments) {
  $previous = $ErrorActionPreference
  $ErrorActionPreference = 'Continue'
  try { return @(& $Exe @Arguments 2>&1 | ForEach-Object { "$_" }) }
  finally { $ErrorActionPreference = $previous }
}

function Test-WingetInstalled([string] $Id) {
  $null = Invoke-Native 'winget' @('list', '--id', $Id, '--exact', '--accept-source-agreements')
  return ($LASTEXITCODE -eq 0)
}

function Install-WingetPackage([string] $Id, [string] $Label) {
  if (Test-WingetInstalled $Id) {
    Write-Note "$Label is already installed, leaving it alone."
    $script:Present.Add($Label)
    return
  }
  if ($DryRun) {
    Write-Note "[dry run] would install $Label (winget id $Id)"
    $script:Skipped.Add("$Label (dry run)")
    return
  }
  Write-Note "Installing $Label (this can take a few minutes)..."
  $null = Invoke-Native 'winget' @('install', '--id', $Id, '--exact', '--silent',
    '--accept-package-agreements', '--accept-source-agreements')
  if ($LASTEXITCODE -eq 0) {
    $script:Installed.Add($Label)
  } else {
    Write-Warn "winget could not install $Label (exit code $LASTEXITCODE)."
    $script:Manual.Add("Install $Label by hand: run 'winget install --id $Id --exact' in a normal PowerShell window, or search for it in the Microsoft Store.")
  }
  Update-SessionPath
}

function Get-GitConfig([string] $Key, [string] $WorkingDir) {
  if ($WorkingDir) {
    $out = Invoke-Native 'git' @('-C', $WorkingDir, 'config', '--get', $Key)
  } else {
    $out = Invoke-Native 'git' @('config', '--global', '--get', $Key)
  }
  if ($LASTEXITCODE -ne 0 -or $out.Count -eq 0) { return '' }
  return ($out | Select-Object -First 1)
}

function Set-GitConfig([string] $Key, [string] $Value, [string] $WorkingDir) {
  if ($DryRun) { Write-Note "[dry run] git config $Key = $Value"; return }
  if ($WorkingDir) {
    $null = Invoke-Native 'git' @('-C', $WorkingDir, 'config', $Key, $Value)
  } else {
    $null = Invoke-Native 'git' @('config', '--global', $Key, $Value)
  }
  if ($LASTEXITCODE -ne 0) { throw "git config $Key failed" }
}

# ---------------------------------------------------------------------------------------------
Write-Host "game1 art machine setup for GitHub user '$GitHubUser'"
if ($DryRun) { Write-Host "DRY RUN: nothing will be installed or changed." -ForegroundColor Yellow }

if ($env:OS -ne 'Windows_NT') {
  throw 'This script is for Windows. On a Mac use tools/bootstrap-macos.sh.'
}
if ($GitHubUser -notmatch '^[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})$') {
  throw "'$GitHubUser' doesn't look like a GitHub username (letters, digits and hyphens only)."
}

# --- winget ----------------------------------------------------------------------------------
Write-Step 'Checking winget (Windows Package Manager)'
if (-not (Test-Command 'winget')) {
  Write-Host @'

winget was not found. It comes with "App Installer", which is built into current Windows 10
and 11. To get it:
  1. Open the Microsoft Store, search for "App Installer" and press Update (or Get).
  2. Close this window, open a new PowerShell window, and run this script again.
If you can't use the Store, install the tools by hand and then re-run this script:
  Git for Windows  https://git-scm.com/download/win
  GitHub Desktop   https://desktop.github.com
  Unity Hub        https://unity.com/download
  Krita            https://krita.org/en/download
'@ -ForegroundColor Yellow
  exit 1
}
Write-Note 'winget is available.'

# --- Packages --------------------------------------------------------------------------------
Write-Step 'Installing tools'
# Git for Windows ships Git LFS and Git Credential Manager (browser sign-in for GitHub).
Install-WingetPackage 'Git.Git' 'Git for Windows (with Git LFS and Git Credential Manager)'
Install-WingetPackage 'GitHub.GitHubDesktop' 'GitHub Desktop'
if ($SkipGitHubCli) { $script:Skipped.Add('GitHub CLI (-SkipGitHubCli)') }
else { Install-WingetPackage 'GitHub.cli' 'GitHub CLI' }
if ($SkipUnity) { $script:Skipped.Add('Unity Hub (-SkipUnity)') }
else { Install-WingetPackage 'Unity.UnityHub' 'Unity Hub' }
if ($SkipKrita) { $script:Skipped.Add('Krita (-SkipKrita)') }
else { Install-WingetPackage 'KDE.Krita' 'Krita' }

if ($SkipPureRef) {
  $script:Skipped.Add('PureRef (-SkipPureRef)')
} else {
  # PureRef is not reliably in winget, so look for it instead of hard-coding an id.
  $found = Invoke-Native 'winget' @('search', 'PureRef', '--source', 'winget', '--accept-source-agreements')
  $pureRefId = $null
  foreach ($line in $found) {
    if ($line -match '^PureRef\s+(\S+\.PureRef\S*)\s') { $pureRefId = $Matches[1]; break }
  }
  if ($pureRefId) { Install-WingetPackage $pureRefId 'PureRef' }
  else {
    Write-Note 'PureRef is not in winget; skipping it. It is optional (a reference-image board).'
    $script:Manual.Add('Optional: download PureRef (pay what you like, including nothing) from https://www.pureref.com')
    $script:Skipped.Add('PureRef (not in winget)')
  }
}

Update-SessionPath
if (-not (Test-Command 'git')) {
  if ($DryRun) {
    Write-Note '[dry run] git is not installed yet, so the git steps below are only described.'
  } else {
    throw 'git is still not available after installing it. Close this window, open a new PowerShell window, and run the script again.'
  }
}
$haveGit = Test-Command 'git'

# --- git identity and sign-in ----------------------------------------------------------------
Write-Step 'Git settings (this user)'
if ($haveGit) {
  $name = Get-GitConfig 'user.name' ''
  if ($name) { Write-Note "user.name is already set ($name)." }
  else {
    if (-not $GitName -and -not $DryRun) { $GitName = Read-Host 'Your name for git commits (e.g. Sarah Smith)' }
    if ($GitName) { Set-GitConfig 'user.name' $GitName '' } else { Write-Warn 'No git name set; run the script again with -GitName.' }
  }
  $mail = Get-GitConfig 'user.email' ''
  if ($mail) { Write-Note "user.email is already set ($mail)." }
  else {
    if (-not $GitEmail -and -not $DryRun) { $GitEmail = Read-Host 'Your email for git commits (the one on your GitHub account)' }
    if ($GitEmail) { Set-GitConfig 'user.email' $GitEmail '' } else { Write-Warn 'No git email set; run the script again with -GitEmail.' }
  }

  # Git for Windows already uses Git Credential Manager; only set it if nothing is configured.
  if (-not (Get-GitConfig 'credential.helper' '')) { Set-GitConfig 'credential.helper' 'manager' '' }
  else { Write-Note 'A git credential helper is already set.' }
  # Pre-fill the GitHub username on the sign-in page. No secret is involved.
  if (-not (Get-GitConfig 'credential.https://github.com.username' '')) {
    Set-GitConfig 'credential.https://github.com.username' $GitHubUser ''
  }
  if (-not $DryRun) { $null = Invoke-Native 'git' @('lfs', 'install', '--skip-repo') }
} else {
  Write-Note '[dry run] would set user.name, user.email and the credential helper.'
}

# --- Clone -----------------------------------------------------------------------------------
Write-Step "Repository: $TargetDir"
$repoReady = $false
if (Test-Path -LiteralPath (Join-Path $TargetDir '.git')) {
  Write-Note 'Already cloned; fetching and fast-forwarding if the current branch allows it.'
  if ($haveGit -and -not $DryRun) {
    $status = Invoke-Native 'git' @('-C', $TargetDir, 'status', '--porcelain')
    if ($status.Count -gt 0) {
      Write-Warn 'You have uncommitted changes; not updating the clone. Commit or discard them first.'
    } else {
      $out = Invoke-Native 'git' @('-C', $TargetDir, 'pull', '--ff-only')
      if ($LASTEXITCODE -eq 0) { Write-Note ($out | Select-Object -Last 1) }
      else { Write-Warn "git pull --ff-only failed:`n    $($out -join "`n    ")" }
    }
  }
  $repoReady = $true
  $script:Present.Add('Repository clone')
} elseif ($DryRun) {
  Write-Note "[dry run] would clone $RepoUrl to $TargetDir"
  $script:Skipped.Add('Repository clone (dry run)')
} else {
  if (Test-Path -LiteralPath $TargetDir) {
    $existing = @(Get-ChildItem -LiteralPath $TargetDir -Force)
    if ($existing.Count -gt 0) { throw "$TargetDir exists and isn't a git clone. Move it away or pass a different -TargetDir." }
  }
  $parent = Split-Path -Parent $TargetDir
  if ($parent -and -not (Test-Path -LiteralPath $parent)) { $null = New-Item -ItemType Directory -Path $parent -Force }
  Write-Note 'Cloning (the game files are large; this can take a while)...'
  $out = Invoke-Native 'git' @('clone', $RepoUrl, $TargetDir)
  if ($LASTEXITCODE -ne 0) { throw "git clone failed:`n$($out -join "`n")" }
  $repoReady = $true
  $script:Installed.Add('Repository clone')
}

# --- Per-clone git config --------------------------------------------------------------------
$unityVersion = $null
$unityChangeset = $null
if ($repoReady) {
  Write-Step 'Git settings (this clone)'
  $versionFile = Join-Path $TargetDir 'ProjectSettings\ProjectVersion.txt'
  if (Test-Path -LiteralPath $versionFile) {
    $text = Get-Content -LiteralPath $versionFile -Raw
    if ($text -match '(?m)^m_EditorVersion:\s*(\S+)') { $unityVersion = $Matches[1] }
    if ($text -match '(?m)^m_EditorVersionWithRevision:\s*\S+\s*\(([0-9a-f]+)\)') { $unityChangeset = $Matches[1] }
  }
  if (-not $unityVersion -or -not $unityChangeset) {
    Write-Warn "Couldn't read the Unity version from $versionFile."
  }

  if ($haveGit -and -not $DryRun) {
    $null = Invoke-Native 'git' @('-C', $TargetDir, 'lfs', 'install', '--local')
    Set-GitConfig 'core.hooksPath' '.githooks' $TargetDir
    if ($unityVersion) {
      # Same driver as tools/bootstrap-macos.sh; the Windows editor keeps it in Editor\Data\Tools.
      # Git runs the driver through its own sh, so use forward slashes.
      $hubEditors = 'C:/Program Files/Unity/Hub/Editor'
      $merge = "$hubEditors/$unityVersion/Editor/Data/Tools/UnityYAMLMerge.exe"
      Set-GitConfig 'merge.unityyamlmerge.name' 'Unity SmartMerge' $TargetDir
      Set-GitConfig 'merge.unityyamlmerge.driver' "'$merge' merge -h -p --force %O %B %A %A" $TargetDir
      Set-GitConfig 'merge.unityyamlmerge.recursive' 'binary' $TargetDir
    }
    Write-Note 'Downloading the large files (Git LFS)...'
    $out = Invoke-Native 'git' @('-C', $TargetDir, 'lfs', 'pull')
    if ($LASTEXITCODE -ne 0) {
      Write-Warn "git lfs pull failed:`n    $($out -join "`n    ")"
      $script:Manual.Add("Download the large files: open a terminal in $TargetDir and run 'git lfs pull' (or use Repository > Pull in GitHub Desktop).")
    }
    $script:Installed.Add('Clone git settings (LFS, hooks, UnityYAMLMerge driver)')
  } else {
    Write-Note '[dry run] would run git lfs install --local, set core.hooksPath and the UnityYAMLMerge driver, and git lfs pull.'
  }
}

# --- Unity editor ----------------------------------------------------------------------------
if ($SkipUnity) {
  Write-Step 'Unity editor'
  Write-Note 'Skipped (-SkipUnity).'
  $script:Skipped.Add('Unity editor (-SkipUnity)')
} elseif (-not $unityVersion -or -not $unityChangeset) {
  Write-Step 'Unity editor'
  Write-Note 'Skipped: the version is read from the cloned repository, which is not available yet.'
  $script:Skipped.Add('Unity editor (no cloned repository to read the version from)')
} else {
  Write-Step "Unity editor $unityVersion"
  $editorExe = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
  $hubExe = $null
  foreach ($candidate in @(
      (Join-Path $env:ProgramFiles 'Unity Hub\Unity Hub.exe'),
      (Join-Path ${env:ProgramFiles(x86)} 'Unity Hub\Unity Hub.exe'),
      (Join-Path $env:LOCALAPPDATA 'Programs\Unity Hub\Unity Hub.exe'))) {
    if ($candidate -and (Test-Path -LiteralPath $candidate)) { $hubExe = $candidate; break }
  }
  $manualEditor = "Install Unity $unityVersion by hand: in Unity Hub choose Installs > Install Editor > Archive > 'download archive', pick $unityVersion, tick 'WebGL Build Support', and install. Or open https://unity.com/releases/editor/whats-new/$unityVersion"

  if (Test-Path -LiteralPath $editorExe) {
    Write-Note "Already installed: $editorExe"
    $script:Present.Add("Unity editor $unityVersion")
  } elseif ($DryRun) {
    Write-Note "[dry run] would install Unity $unityVersion ($unityChangeset) with WebGL support through Unity Hub"
    $script:Skipped.Add("Unity editor $unityVersion (dry run)")
  } elseif (-not $hubExe) {
    Write-Warn "Couldn't find Unity Hub.exe. If it was just installed, open Unity Hub once, then re-run this script."
    $script:Manual.Add($manualEditor)
  } else {
    Write-Note 'Installing through the Unity Hub command line (a large download; be patient).'
    Write-Note 'The Hub CLI is marked deprecated by Unity but still works; if it fails you get manual steps.'
    # Windows editor builds include Windows (Mono) support; the WebGL module is the extra we need.
    # Unity Hub is a GUI program, so PowerShell would not wait for it; Start-Process -Wait does.
    $hubArgs = @('--', '--headless', 'install', '--version', $unityVersion,
      '--changeset', $unityChangeset, '--module', 'webgl')
    $hubCode = 'unknown'
    try {
      $proc = Start-Process -FilePath $hubExe -ArgumentList $hubArgs -Wait -PassThru -NoNewWindow
      $hubCode = $proc.ExitCode
    } catch {
      Write-Warn "Couldn't run Unity Hub: $($_.Exception.Message)"
    }
    if ((Test-Path -LiteralPath $editorExe)) {
      $script:Installed.Add("Unity editor $unityVersion (with WebGL support)")
    } else {
      Write-Warn "Unity Hub's command line did not install the editor (exit code $hubCode)."
      $script:Manual.Add($manualEditor)
    }
  }
}

# --- Summary ---------------------------------------------------------------------------------
Write-Host ""
Write-Host '==> Summary' -ForegroundColor Green
if ($script:Installed.Count) { Write-Host '  Installed / set up:'; $script:Installed | ForEach-Object { Write-Host "    + $_" } }
if ($script:Present.Count) { Write-Host '  Already there (left alone):'; $script:Present | ForEach-Object { Write-Host "    = $_" } }
if ($script:Skipped.Count) { Write-Host '  Skipped:'; $script:Skipped | ForEach-Object { Write-Host "    - $_" } }

Write-Host ""
Write-Host '==> What you still need to do by hand' -ForegroundColor Green
$step = 1
foreach ($item in $script:Manual) { Write-Host "  $step. $item"; $step++ }
Write-Host "  $step. GitHub sign-in: open GitHub Desktop, choose File > Options > Accounts > Sign in, and sign in as '$GitHubUser'"
Write-Host '     in the browser window that opens. The first git push or pull from a terminal opens the same page.'
$step++
Write-Host "  $step. GitHub Desktop: File > Add local repository... > $TargetDir"
Write-Host '     (Ask the project owner for write access first if Push is greyed out.)'
$step++
Write-Host "  $step. Unity Hub: open it and sign in with your Unity ID, then Manage licenses > Add > Get a free personal license."
Write-Host '     Decline any offer to connect to Unity Cloud or Unity Version Control (this project uses Git only).'
$step++
Write-Host "  $step. Unity Hub: Projects > Add > Add project from disk > $TargetDir"
Write-Host "     then open it. The first open imports everything and can take several minutes."
$step++
Write-Host "  $step. Read docs/design/art-direction.md and docs/design/art-spec.md in the project before you start drawing."
if ($DryRun) { Write-Host ''; Write-Host 'This was a dry run; nothing was changed.' -ForegroundColor Yellow }
