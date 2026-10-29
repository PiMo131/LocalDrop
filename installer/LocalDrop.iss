; Inno Setup script for LocalDrop (Windows)
; Build: first `dotnet publish` the Windows target (see build.ps1), then compile this script.

#define AppName "LocalDrop"
#define AppVersion "1.0.0"
#define AppPublisher "LocalDrop"
#define AppExe "LocalDrop.exe"
#ifndef PublishDir
  #define PublishDir "..\LocalDrop\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish"
#endif

[Setup]
AppId={{7E1C4C0A-2D7B-4F1E-9B6C-LOCALDROP0001}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=LocalDrop-Setup-{#AppVersion}
SetupIconFile=..\LocalDrop\Platforms\Windows\trayicon.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"

[CustomMessages]
english.AutoStart=Start LocalDrop automatically when Windows starts
dutch.AutoStart=LocalDrop automatisch starten met Windows
english.FirewallNote=LocalDrop listens on your local network (port 8787). Windows may ask you to allow it through the firewall on first start; choose "Private networks".
dutch.FirewallNote=LocalDrop luistert op je lokale netwerk (poort 8787). Windows kan bij de eerste start vragen om toegang door de firewall; kies "Particuliere netwerken".

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutoStart}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Autostart (per user), minimised to tray
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"" --minimized"; Flags: uninsdeletevalue; Tasks: autostart
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "{#AppName}"; Flags: deletevalue uninsdeletevalue; Tasks: not autostart

[Run]
; Open the firewall for the app on private networks (silently; needs admin, skipped otherwise)
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""LocalDrop"" dir=in action=allow program=""{app}\{#AppExe}"" enable=yes profile=private,domain"; Flags: runhidden; Check: IsAdminInstallMode
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""LocalDrop"""; Flags: runhidden; Check: IsAdminInstallMode; RunOnceId: "fw"

[Code]
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
    WizardForm.FinishedLabel.Caption := WizardForm.FinishedLabel.Caption + #13#10#13#10 + CustomMessage('FirewallNote');
end;
