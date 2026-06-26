; Window Alert App — Inno Setup Script
; 빌드: .\Packaging\build-innosetup.ps1

#define AppName "Window Alert App"
#define AppVersion "1.0.0"
#define AppExe "Window_Alert_App.exe"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=mjk0323
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputBaseFilename=WindowAlertApp_Setup
OutputDir=..
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Resources\tray-icon.ico
UninstallDisplayIcon={app}\{#AppExe}
MinVersion=10.0.19041
ArchitecturesInstallIn64BitMode=x64os
ArchitecturesAllowed=x64os

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕화면 바로가기 만들기"; GroupDescription: "추가 옵션:"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{#AppName} 제거"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{#AppName} 실행"; Flags: nowait postinstall skipifsilent
