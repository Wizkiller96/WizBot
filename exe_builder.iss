#define sysfolder "system"
#define version GetEnv("WIZBOT_INSTALL_VERSION")
#define target "win7-x64"
#define platform "net5.0"

[Setup]
AppName = {param:botname|WizBot}
AppVersion={#version}
AppPublisher=WizNet
DefaultDirName={param:installpath|{commonpf}\WizBot}
DefaultGroupName=WizBot
UninstallDisplayIcon={app}\{#sysfolder}\wizbot_icon.ico
Compression=lzma2
SolidCompression=yes
UsePreviousLanguage=no
UsePreviousSetupType=no
UsePreviousAppDir=no
OutputDir=wizbot-installers/{#version}/
OutputBaseFilename=wizbot-setup-{#version}
AppReadmeFile=https://commands.wizbot.cc/
ArchitecturesInstallIn64BitMode=x64
DisableWelcomePage=yes
DisableDirPage=yes
DisableFinishedPage=yes
DisableReadyMemo=yes
DisableProgramGroupPage=yes
WizardStyle=modern
UpdateUninstallLogAppName=no
CreateUninstallRegKey=no
Uninstallable=no

[Files]
;install 
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\*"; DestDir: "{app}\{#sysfolder}"; Permissions: users-full; Flags: recursesubdirs onlyifdoesntexist ignoreversion createallsubdirs; Excludes: "*.pdb, *.db"

;reinstall - i want to copy all files, but i don't want to overwrite any data files because users will lose their customization if they don't have a backup, 
;            and i don't want them to have to backup and then copy-merge into data folder themselves, or lose their currency images due to overwrite.
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\*"; DestDir: "{app}\{#sysfolder}"; Permissions: users-full; Flags: recursesubdirs ignoreversion onlyifdestfileexists createallsubdirs; Excludes: "*.pdb, *.db, data\*, credentials.json, creds.yml";
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\data\*"; DestDir: "{app}\{#sysfolder}\data"; Permissions: users-full; Flags: recursesubdirs onlyifdoesntexist createallsubdirs;
; overwrite strings and aliases
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\data\aliases.yml"; DestDir: "{app}\{#sysfolder}\data\"; Permissions: users-full; Flags: recursesubdirs ignoreversion onlyifdestfileexists createallsubdirs;
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\data\strings\*"; DestDir: "{app}\{#sysfolder}\data\strings"; Permissions: users-full; Flags: recursesubdirs ignoreversion onlyifdestfileexists createallsubdirs;

[Dirs]
Name:"{app}\{#sysfolder}\data"; Permissions: everyone-modify
Name:"{app}\{#sysfolder}\config"; Permissions: everyone-modify
Name:"{app}\{#sysfolder}"; Permissions: everyone-modify

; [Run]
; Filename: "http://wizbot.readthedocs.io/en/latest/jsons-explained/"; Flags: postinstall shellexec runasoriginaluser; Description: "Open setup guide"
; Filename: "{app}\{#sysfolder}\creds.yml"; Flags: postinstall shellexec runasoriginaluser; Description: "Open creds file"

[Icons]
; for pretty install directory
Name: "{app}\WizBot"; Filename: "{app}\{#sysfolder}\WizBot.exe"; IconFilename: "{app}\{#sysfolder}\wizbot_icon.ico"
Name: "{app}\creds"; Filename: "{app}\{#sysfolder}\creds.yml" 
Name: "{app}\data"; Filename: "{app}\{#sysfolder}\data" 

; desktop shortcut 
Name: "{commondesktop}\{#SetupSetting("AppName")}"; Filename: "{app}\WizBot";

[Code]
function GetFileName(const AFileName: string): string;
begin
  Result := ExpandConstant('{app}\{#sysfolder}\' + AFileName);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) then
  begin
      FileCopy(GetFileName('creds_example.yml'), GetFileName('creds.yml'), True);
  end;
end;