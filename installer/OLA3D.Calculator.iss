#define AppName "OLA3D Calculator"
#define AppVersion "1.0.0"
#define AppExeName "OLA3D.Calculator.exe"

[Setup]
AppId={{7667E563-BA67-449B-BF0B-64ACEE3727A1}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} 1.0
AppPublisher=OLA3D
AppCopyright=Copyright (C) 2026 OLA3D
VersionInfoVersion={#AppVersion}
VersionInfoCompany=OLA3D
DefaultDirName={localappdata}\Programs\OLA3D Calculator
DefaultGroupName=OLA3D Calculator
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\artifacts\release
OutputBaseFilename=OLA3D-Calculator-1.0.0-Setup-win-x64
SetupIconFile=..\src\OLA3D.Calculator\app.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=yes
CloseApplications=yes

[Languages]
Name: "hu"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
hu.DesktopShortcut=Asztali parancsikon létrehozása
en.DesktopShortcut=Create a desktop shortcut
hu.LaunchApp=Az OLA3D Calculator indítása
en.LaunchApp=Launch OLA3D Calculator
hu.CurrencyTitle=Pénznem kiválasztása
en.CurrencyTitle=Choose a currency
hu.CurrencyDescription=Melyik pénznemben szeretnél számolni?
en.CurrencyDescription=Which currency would you like to use?
hu.CurrencyInfo=Válassz alapértelmezett díjszabást. Az óradíjak és anyagárak később módosíthatók a programban. A pénznem a telepítő újbóli futtatásával változtatható meg.
en.CurrencyInfo=Choose a default price list. You can edit hourly rates and material prices in the app. To change currency, run the installer again.
hu.CurrencyHUF=Magyar forint (HUF) — alap CAD-óradíj: 5000 Ft
en.CurrencyHUF=Hungarian forint (HUF) — default CAD hourly rate: 5000 HUF
hu.CurrencyEUR=Euró (EUR) — alap CAD-óradíj: 13 EUR
en.CurrencyEUR=Euro (EUR) — default CAD hourly rate: 13 EUR
hu.CurrencyUSD=USA-dollár (USD) — alap CAD-óradíj: 15 USD
en.CurrencyUSD=US dollar (USD) — default CAD hourly rate: 15 USD

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.hu.md"; DestDir: "{app}"; Flags: ignoreversion

[INI]
Filename: "{app}\language.ini"; Section: "Application"; Key: "Language"; String: "{language}"; Flags: uninsdeleteentry uninsdeletesectionifempty
Filename: "{app}\language.ini"; Section: "Application"; Key: "Currency"; String: "{code:SelectedCurrency}"; Flags: uninsdeleteentry uninsdeletesectionifempty

[UninstallDelete]
Type: files; Name: "{app}\language.ini"

[Icons]
Name: "{autoprograms}\OLA3D Calculator"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\OLA3D Calculator"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[Code]
var
  CurrencyPage: TInputOptionWizardPage;

function SelectedCurrency(Param: String): String;
begin
  case CurrencyPage.SelectedValueIndex of
    1: Result := 'EUR';
    2: Result := 'USD';
  else
    Result := 'HUF';
  end;
end;

procedure InitializeWizard;
var
  PreviousCurrency: String;
begin
  CurrencyPage := CreateInputOptionPage(wpSelectDir,
    CustomMessage('CurrencyTitle'), CustomMessage('CurrencyDescription'),
    CustomMessage('CurrencyInfo'), True, False);
  CurrencyPage.Add(CustomMessage('CurrencyHUF'));
  CurrencyPage.Add(CustomMessage('CurrencyEUR'));
  CurrencyPage.Add(CustomMessage('CurrencyUSD'));
  PreviousCurrency := ExpandConstant('{param:CURRENCY|' + GetPreviousData('Currency', 'HUF') + '}');
  if PreviousCurrency = 'EUR' then CurrencyPage.SelectedValueIndex := 1
  else if PreviousCurrency = 'USD' then CurrencyPage.SelectedValueIndex := 2
  else CurrencyPage.SelectedValueIndex := 0;
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'Currency', SelectedCurrency(''));
end;
