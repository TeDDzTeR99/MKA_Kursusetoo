; Installer for the Elden Ring Wiki app, built with Inno Setup 6 (https://jrsoftware.org/isinfo.php).
; It packs the Release build: the program, its DLLs, the Estonian texts, the database and every item picture.
;
; Build it from the repository root (see the README):
;   1. MSBuild MKA_KursuseToo\MKA_KursuseToo.csproj -p:Configuration=Release
;   2. ISCC installer\EldenRingWiki.iss
; The result is installer\Output\EldenRingWikiSetup.exe.

#define AppName "Elden Ring Wiki"
#define AppVersion "1.0.0"
#define AppExe "MKA_KursuseToo.exe"
#define Build "..\MKA_KursuseToo\bin\Release"

[Setup]
; AppId identifies the app for updates and uninstalling: never change it
AppId={{38478015-A3EB-402D-876B-F13C8BD38014}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=TeDDzTeR99
AppPublisherURL=https://github.com/TeDDzTeR99/MKA_Kursusetoo
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\MKA_KursuseToo\app.ico
OutputDir=Output
OutputBaseFilename=EldenRingWikiSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Lets the user pick "only for me" (no admin rights needed) or "for all users"
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Everything in the Release folder except debug symbols and API docs. The database is opened read-only,
; so it can live in Program Files; the language choice is saved per user in %LOCALAPPDATA%.
Source: "{#Build}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
