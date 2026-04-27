; Inno Setup Script for Open Mods Tracker
; Download Inno Setup from https://jrsoftware.org/isinfo.php

#define MyAppName "Open Mods Tracker"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Open Mods Tracker"
#define MyAppURL "https://github.com/obewan/OpenModsTracker"
#define MyAppExeName "Open Mods Tracker.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output settings
OutputDir=installer
OutputBaseFilename=OpenModsTracker-Setup-{#MyAppVersion}
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
; Modern appearance
WizardStyle=modern
; Require admin rights for installation
PrivilegesRequired=admin
; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Uninstaller
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english";               MessagesFile: "compiler:Default.isl"
Name: "arabic";                MessagesFile: "compiler:Languages\Arabic.isl"
Name: "armenian";              MessagesFile: "compiler:Languages\Armenian.isl"
Name: "brazilianportuguese";   MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "bulgarian";             MessagesFile: "compiler:Languages\Bulgarian.isl"
Name: "catalan";               MessagesFile: "compiler:Languages\Catalan.isl"
Name: "czech";                 MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish";                MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch";                 MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish";               MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french";                MessagesFile: "compiler:Languages\French.isl"
Name: "german";                MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew";                MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "hungarian";             MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "italian";               MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese";              MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean";                MessagesFile: "compiler:Languages\Korean.isl"
Name: "norwegian";             MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish";                MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese";            MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian";               MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovak";                MessagesFile: "compiler:Languages\Slovak.isl"
Name: "slovenian";             MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish";               MessagesFile: "compiler:Languages\Spanish.isl"
Name: "swedish";               MessagesFile: "compiler:Languages\Swedish.isl"
Name: "tamil";                 MessagesFile: "compiler:Languages\Tamil.isl"
Name: "turkish";               MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian";             MessagesFile: "compiler:Languages\Ukrainian.isl"


[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application files
Source: "bin\x64\Release\net10.0-windows10.0.26100.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\{#MyAppName}"
Type: filesandordirs; Name: "{userappdata}\{#MyAppName}"