#define sysfolder "system"
#define version GetEnv("WIZBOT_INSTALL_VERSION")
#define target "win7-x64"
#define platform "netcoreapp2.1"

[Setup]
AppName=WizBot
AppVersion={#version}
AppPublisher=WizNet
AppCopyright=© 2017-2018 WizNet - All Rights Reserved
DefaultDirName={commonpf}\WizBot
DefaultGroupName=WizBot
UninstallDisplayIcon={app}\{#sysfolder}\wizbot_icon.ico
WizardImageFile=wizbot_installer.bmp
Compression=lzma2
SolidCompression=yes
OutputDir=userdocs:_projekti/WizBotInstallerOutput/{#version}/
OutputBaseFilename=wizbot-setup-{#version}
AppReadmeFile=http://wizbot.readthedocs.io/en/latest/
ArchitecturesInstallIn64BitMode=x64
UsePreviousSetupType=no
DisableWelcomePage=no
WizardStyle=modern

[Files]
;install 
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\*"; DestDir: "{app}\{#sysfolder}"; Permissions: users-full; Flags: recursesubdirs onlyifdoesntexist ignoreversion createallsubdirs; Excludes: "*.pdb, *.db"
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\data\command_strings.json"; DestDir: "{app}\{#sysfolder}\data"; DestName: "command_strings.json"; Permissions: users-full; Flags: skipifsourcedoesntexist ignoreversion createallsubdirs recursesubdirs;
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\data\hangman.json"; DestDir: "{app}\{#sysfolder}\data"; DestName: "hangman.json"; Permissions: users-full; Flags: skipifsourcedoesntexist ignoreversion createallsubdirs recursesubdirs;
;rename credentials example to credentials, but don't overwrite if it exists
;Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\credentials_example.json"; DestName: "credentials.json"; DestDir: "{app}\{#sysfolder}"; Permissions: users-full; Flags: skipifsourcedoesntexist onlyifdoesntexist;

;reinstall - i want to copy all files, but i don't want to overwrite any data files because users will lose their customization if they don't have a backup, 
;            and i don't want them to have to backup and then copy-merge into data folder themselves, or lose their currency images due to overwrite.
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\*"; DestDir: "{app}\{#sysfolder}"; Permissions: users-full; Flags: recursesubdirs ignoreversion onlyifdestfileexists createallsubdirs; Excludes: "*.pdb, *.db, data\*, credentials.json";
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\data\*"; DestDir: "{app}\{#sysfolder}\data"; Permissions: users-full; Flags: recursesubdirs onlyifdoesntexist createallsubdirs;
;overwrite pokemon folder always
Source: "src\WizBot\bin\Release\{#platform}\{#target}\publish\data\pokemon"; DestDir: "{app}\{#sysfolder}\data\pokemon"; Permissions: users-full; Flags: skipifsourcedoesntexist ignoreversion createallsubdirs recursesubdirs;
;readme   
;Source: "readme"; DestDir: "{app}"; Flags: isreadme

[Dirs]
Name:"{app}\{#sysfolder}\data"; Permissions: everyone-modify
Name:"{app}\{#sysfolder}"; Permissions: everyone-modify

[Run]
Filename: "http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/"; Flags: postinstall shellexec runasoriginaluser; Description: "Open setup guide"
Filename: "{app}\{#sysfolder}\credentials.json"; Flags: postinstall shellexec runasoriginaluser; Description: "Open credentials file"
Filename: "http://wizbot.cf/"; Flags: postinstall shellexec runasoriginaluser; Description: "Visit WizBot's Website"

[Icons]
; for pretty install directory
Name: "{app}\WizBot"; Filename: "{app}\{#sysfolder}\WizBot.exe"; IconFilename: "{app}\{#sysfolder}\wizbot_icon.ico"
Name: "{app}\credentials"; Filename: "{app}\{#sysfolder}\credentials.json" 
Name: "{app}\data"; Filename: "{app}\{#sysfolder}\data" 

; desktop shortcut 
Name: "{commondesktop}\WizBot"; Filename: "{app}\WizBot"; Tasks: desktopicon
; desktop icon checkbox
[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut";

[Registry]
;make the app run as administrator
Root: "HKLM"; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"; \
    ValueType: String; ValueName: "{app}\{#sysfolder}\WizBot.exe"; ValueData: "RUNASADMIN"; \
    Flags: uninsdeletekeyifempty uninsdeletevalue;
Root: "HKCU"; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"; \
    ValueType: String; ValueName: "{app}\{#sysfolder}\WizBot.exe"; ValueData: "RUNASADMIN"; \
    Flags: uninsdeletekeyifempty uninsdeletevalue;
Root: "HKLM"; Subkey: "SOFTWARE\WizBot"; \
    ValueType: String; ValueName: "InstallPath"; ValueData: "{app}\{#sysfolder}"; \
    Flags: deletevalue uninsdeletekeyifempty uninsdeletevalue;
Root: "HKLM"; Subkey: "SOFTWARE\WizBot"; \
    ValueType: String; ValueName: "Version"; ValueData: "{#version}"; \
    Flags: deletevalue uninsdeletekeyifempty uninsdeletevalue;

[Messages]
WelcomeLabel2=Hello, if you have any issues, join https://wizbot.cf/discord and ask for help in #help channel.%n%nIt is recommended that you CLOSE any ANTI VIRUS before continuing.

;ask the user if they want to delete all settings
[Code]
var
X: string;
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    X := ExpandConstant('{app}');
    if FileExists(X + '\{#sysfolder}\data\WizBot.db') then begin
      if MsgBox('Do you want to delete all settings associated with this bot?', mbConfirmation, MB_YESNO) = IDYES then begin
        DelTree(X + '\{#sysfolder}', True, True, True);
      end
    end else begin
      MsgBox(X + '\{#sysfolder}\data\WizBot.db doesn''t exist', mbConfirmation, MB_YESNO)
    end
  end;
end;

function GetFileName(const AFileName: string): string;
begin
  Result := ExpandConstant('{app}\{#sysfolder}\' + AFileName);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) then
  begin
    if FileExists(GetFileName('credentials_example.json')) and not FileExists(GetFileName('credentials.json')) then
      RenameFile(GetFileName('credentials_example.json'), GetFileName('credentials.json'));
  end;
end;