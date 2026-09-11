# OLA3D Calculator 1.0

CAD design and multi-material 3D printing price calculator for Windows, by **OLA3D**.

[Magyar leírás](README.hu.md)

## Download and installation

Download `OLA3D-Calculator-1.0.0-Setup-win-x64.exe` from this repository's **Releases** page. Run the installer, choose **English** or **Magyar**, select **HUF**, **EUR** or **USD**, and optionally select **Create a desktop shortcut**. The application uses the language chosen during installation, including when opened from its EXE. Re-run the installer to change language or currency.

Windows 10/11, 64-bit. No separate .NET installation or administrator account is required. The installer includes the .NET runtime. This release is not digitally signed.

## Features

- CAD estimates with complexity, precision, scanning, revisions, deadlines and travel.
- Multi-material print quotes with material weights, machine time, waste factors, minimum fees and rounding.
- English and Hungarian interface, settings, messages and installer.
- HUF / EUR / USD selection during installation, with independently saved pricing profiles.
- Suggested CAD hourly rates: **5000 HUF**, **13 EUR**, **15 USD**. These are editable sample rates, not live exchange rates. Other monetary defaults are also provided for each currency.
- Currency stays fixed while the application runs. Re-run the installer to select a different price list.
- Factory reset affects only the selected currency's pricing profile.
- Author: **OLA3D**. Version: **1.0.0**.

## Settings

Each Windows user has separate settings under `%APPDATA%\OLA3D Calculator`:

- `settings-HUF.json`, `settings-EUR.json`, `settings-USD.json`: independent rates and material catalogs.

Language and currency are recorded in `language.ini` in the installation folder.

No personal settings are bundled or imported from other applications. Removing the application retains your pricing profiles for later reinstallation.

Print charge: sum of `(grams / 1000) × price per kg × material factor × waste factor`, plus `print hours × machine hourly rate`. The minimum fee and rounding step are applied last. Changing currency selects another price list; it does not perform live currency conversion.

## Build

Install the .NET 8 SDK (or a newer compatible SDK) and [Inno Setup 6](https://jrsoftware.org/isdl.php).

```powershell
dotnet run --project tests/OLA3D.Calculator.Tests.csproj -c Release
.\scripts\build.ps1 -IsccPath 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

The self-contained installer and its SHA-256 checksum are written to `artifacts/release`. Build outputs and local settings are excluded from source control. To run from source in a specific language, pass `--language=en` or `--language=hu` after `dotnet run --project src/OLA3D.Calculator --`.

Copyright © 2026 OLA3D. No open-source license is granted with this release.
