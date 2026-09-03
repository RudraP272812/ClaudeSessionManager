; Inno Setup script for ClaudeSessionManager (by TraceFix).
; Requires Inno Setup 6: https://jrsoftware.org/isdl.php
;
; Build steps:
;   1. dotnet publish ..\ClaudeSessionManager.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
;   2. Compile this script with ISCC.exe (or open it in Inno Setup and press Build).
; Output: installer\Output\ClaudeSessionManagerSetup.exe

#define MyAppName "ClaudeSessionManager"
#define MyAppPublisher "TraceFix"
#define MyAppVersion "1.0.0"
#define MyAppExeName "ClaudeSessionManager.exe"

[Setup]
AppId={{B6E2E6C4-6E7A-4C7E-9A4D-9C7B2D6E6F31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=ClaudeSessionManagerSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
; Optional: drop a tracefix.ico (converted from branding\tracefix-mark.svg) next to this
; script and uncomment the line below for a branded installer/uninstaller icon.
; SetupIconFile=tracefix.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
