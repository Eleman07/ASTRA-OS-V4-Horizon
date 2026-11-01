; ASTRA OS V4 Release Candidate - per-user installer (Inno Setup 6)
; FIRST run scripts/build.ps1 to produce dist/AstraOS.
#define AppName "ASTRA OS"
#define AppVersion "4.0.0-rc1"
#define AppPublisher "ASTRA OS Contributors"
#define PublishFolder "..\dist\AstraOS"

[Setup]
AppId={{2B0D0D61-A9F4-4FAD-8A70-5E0FD74D39E3}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\ASTRA OS
DefaultGroupName=ASTRA OS
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=ASTRA-OS-V4-RC1-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Astra.Desktop.exe

[Files]
Source: "{#PublishFolder}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\ASTRA OS"; Filename: "{app}\Astra.Desktop.exe"
Name: "{userdesktop}\ASTRA OS"; Filename: "{app}\Astra.Desktop.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Crea collegamento desktop"; GroupDescription: "Collegamenti:"; Flags: unchecked

[Run]
Filename: "{app}\Astra.Desktop.exe"; Description: "Avvia ASTRA OS"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Deliberately do NOT delete preferences under LocalAppData\ASTRA\OS.
; User can opt to delete saved settings separately; document location.
