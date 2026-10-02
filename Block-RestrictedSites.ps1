# Block-RestrictedSites.ps1
# Portable Restricted Site Control
# Requires PowerShell running as Administrator.

param(
    [ValidateSet("Install","Block","Unblock")]
    [string]$Action = "Install"
)

$ErrorActionPreference = "Stop"

$installDir = Join-Path $env:ProgramFiles "RestrictedSiteControl"
$installedScript = Join-Path $installDir "Block-RestrictedSites.ps1"
$hostsFile = "$env:SystemRoot\System32\drivers\etc\hosts"
$backup = "$hostsFile.restricted-backup"
$startMarker = "# BEGIN RESTRICTED SITES"
$endMarker = "# END RESTRICTED SITES"

$domains = @(
    # Video / social
    "youtube.com","www.youtube.com","m.youtube.com","youtu.be","www.youtu.be",
    "youtube-nocookie.com","www.youtube-nocookie.com","googlevideo.com",
    "www.googlevideo.com","ytimg.com","www.ytimg.com",
    "tiktok.com","www.tiktok.com","m.tiktok.com","vm.tiktok.com",
    "tiktokcdn.com","www.tiktokcdn.com",

    # Gaming
    "roblox.com","www.roblox.com","minecraft.net","www.minecraft.net",
    "epicgames.com","www.epicgames.com","fortnite.com","www.fortnite.com",
    "steampowered.com","store.steampowered.com","steamcommunity.com",
    "www.steamcommunity.com","ea.com","www.ea.com","riotgames.com",
    "www.riotgames.com","leagueoflegends.com","www.leagueoflegends.com",
    "battle.net","www.battle.net","blizzard.com","www.blizzard.com",
    "ubisoft.com","www.ubisoft.com","xbox.com","www.xbox.com",
    "playstation.com","www.playstation.com","nintendo.com","www.nintendo.com",
    "twitch.tv","www.twitch.tv","poki.com","www.poki.com",
    "miniclip.com","www.miniclip.com","crazygames.com","www.crazygames.com",
    "friv.com","www.friv.com",

    # Forex / trading
    "tradingview.com","www.tradingview.com",
    "investing.com","www.investing.com",
    "forexfactory.com","www.forexfactory.com",
    "fxstreet.com","www.fxstreet.com",
    "dailyfx.com","www.dailyfx.com",
    "oanda.com","www.oanda.com",
    "xm.com","www.xm.com",
    "exness.com","www.exness.com",
    "icmarkets.com","www.icmarkets.com",
    "pepperstone.com","www.pepperstone.com",
    "deriv.com","www.deriv.com",
    "fbs.com","www.fbs.com",
    "octa.com","www.octafx.com",
    "avatrade.com","www.avatrade.com",
    "plus500.com","www.plus500.com",
    "etoro.com","www.etoro.com",
    "metatrader.com","www.metatrader.com",
    "mql5.com","www.mql5.com"
)

# Verify Administrator privileges.
$principal = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent()
)

if (-not $principal.IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)) {
    Write-Host ""
    Write-Host "ERROR: Administrator privileges are required." -ForegroundColor Red
    Write-Host "Close PowerShell and select 'Run as administrator'."
    exit 1
}

# INSTALL
if ($Action -eq "Install") {

    $sourceScript = $MyInvocation.MyCommand.Path

    if (-not $sourceScript) {
        Write-Host "ERROR: The script must be run from a saved .ps1 file." -ForegroundColor Red
        exit 1
    }

    New-Item -ItemType Directory -Path $installDir -Force | Out-Null

    Copy-Item $sourceScript $installedScript -Force

    # Hide the installation directory.
    attrib +h $installDir

    Write-Host ""
    Write-Host "Restricted Site Control installed successfully."
    Write-Host "Installation folder: $installDir"
    Write-Host ""
    Write-Host "Next action: run the installed script with -Action Block"
    exit 0
}

# BLOCK
if ($Action -eq "Block") {

    if (-not (Test-Path $backup)) {
        Copy-Item $hostsFile $backup -Force
        Write-Host "Original HOSTS file backed up."
    }

    $current = Get-Content $hostsFile -ErrorAction Stop

    $filtered = @()
    $insideBlock = $false

    foreach ($line in $current) {

        if ($line -eq $startMarker) {
            $insideBlock = $true
            continue
        }

        if ($line -eq $endMarker) {
            $insideBlock = $false
            continue
        }

        if (-not $insideBlock) {
            $filtered += $line
        }
    }

    $filtered += ""
    $filtered += $startMarker

    foreach ($domain in $domains) {
        $filtered += "0.0.0.0 $domain"
    }

    $filtered += $endMarker

    Set-Content -Path $hostsFile -Value $filtered -Encoding ASCII -Force

    Clear-DnsClientCache
    # Prevent Microsoft Edge from bypassing the HOSTS file using DNS-over-HTTPS.
    $edgePolicyPath = "HKLM:\SOFTWARE\Policies\Microsoft\Edge"
    New-Item -Path $edgePolicyPath -Force | Out-Null
    New-ItemProperty -Path $edgePolicyPath -Name "DnsOverHttpsMode" -PropertyType String -Value "off" -Force | Out-Null

    # Close Microsoft Edge so existing connections cannot bypass the restriction.
    Stop-Process -Name "msedge" -Force -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "Restricted sites have been BLOCKED successfully."
    Write-Host "HOSTS backup: $backup"
    exit 0
}

# UNBLOCK
if ($Action -eq "Unblock") {

    if (Test-Path $backup) {
        Copy-Item $backup $hostsFile -Force
        Clear-DnsClientCache

        Write-Host ""
        Write-Host "Restricted sites have been UNBLOCKED successfully."
        Write-Host "Original HOSTS file restored."
    }
    else {
        Write-Host ""
        Write-Host "ERROR: No HOSTS backup was found." -ForegroundColor Red
        Write-Host "Nothing was changed."
        exit 1
    }

    exit 0
}


