# Restricted Site Control for Windows

A Windows utility that restricts access to selected websites using the Windows HOSTS file and disables Microsoft Edge DNS-over-HTTPS to help prevent DNS-based bypasses.

## Download

The main installer is:

**InstallerBuild\RestrictedSiteControl-Setup.exe**

Download the latest release from the GitHub **Releases** section.

## Installation

1. Download `RestrictedSiteControl-Setup.exe`.
2. Double-click the EXE.
3. Accept the Windows Administrator/UAC prompt.
4. The controller installs automatically.

If the installer is run again, it detects the existing installation and provides options to update or uninstall.

## Features

- Blocks configured websites through the Windows HOSTS file.
- Disables Microsoft Edge DNS-over-HTTPS while restrictions are active.
- Restarts Microsoft Edge after blocking is applied.
- Clears the Windows DNS cache.
- Automatically backs up the original HOSTS file.
- Restores the original HOSTS file when uninstalled.
- Runs with Windows Administrator privileges when required.
- Uses a hidden installation structure under Program Files.

## Source

The PowerShell controller and C# installer source are included in this repository.

## Documentation

See `Instructions.txt` for additional instructions.

