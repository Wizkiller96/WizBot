@ECHO off
TITLE Downloading WizBot, please wait
::Setting convenient to read variables which don't delete the windows temp folder
SET root=%~dp0
CD /D %root%
SET rootdir=%cd%
SET build1=%root%WizBotInstall_Temp\WizBot\Discord.Net\src\Discord.Net.Core\
SET build2=%root%WizBotInstall_Temp\WizBot\Discord.Net\src\Discord.Net.Rest\
SET build3=%root%WizBotInstall_Temp\WizBot\Discord.Net\src\Discord.Net.WebSocket\
SET build4=%root%WizBotInstall_Temp\WizBot\Discord.Net\src\Discord.Net.Commands\
SET build5=%root%WizBotInstall_Temp\WizBot\src\WizBot\
SET installtemp=%root%WizBotInstall_Temp\
::Deleting traces of last setup for the sake of clean folders, if by some miracle it still exists
IF EXIST %installtemp% ( RMDIR %installtemp% /S /Q >nul 2>&1)
::Checks that both git and dotnet are installed
dotnet --version >nul 2>&1 || GOTO :dotnet
git --version >nul 2>&1 || GOTO :git
::Creates the install directory to work in and get the current directory because spaces ruins everything otherwise
:start
MKDIR WizBotInstall_Temp
CD /D %installtemp%
::Downloads the latest version of WizBot
ECHO Downloading WizBot...
ECHO.
git clone -b dev --recursive --depth 1 --progress https://github.com/Kwoth/WizBot.git >nul
IF %ERRORLEVEL% EQU 128 (GOTO :giterror)
TITLE Installing WizBot, please wait
ECHO.
ECHO Installing...
::Building WizBot
CD /D %build1%
dotnet restore >nul 2>&1
CD /D %build2%
dotnet restore >nul 2>&1
CD /D %build3%
dotnet restore >nul 2>&1
CD /D %build4%
dotnet restore >nul 2>&1
CD /D %build5%
dotnet restore >nul 2>&1
dotnet build --configuration Release >nul 2>&1
::Attempts to backup old files if they currently exist in the same folder as the batch file
IF EXIST "%root%WizBot\" (GOTO :backupinstall)
:freshinstall
	::Moves the WizBot folder to keep things tidy
	ROBOCOPY "%root%WizBotInstall_Temp" "%rootdir%" /E /MOVE >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	GOTO :end
:backupinstall
	TITLE Backing up old files
	::Recursively copies all files and folders from WizBot to WizBot_Old
	ROBOCOPY "%root%WizBot" "%root%WizBot_Old" /MIR >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	ECHO.
	ECHO Old files backed up to WizBot_Old
	::Copies the credentials and database from the backed up data to the new folder
	COPY "%root%WizBot_Old\src\WizBot\credentials.json" "%installtemp%WizBot\src\WizBot\credentials.json" >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	ECHO.
	ECHO credentials.json copied to new folder
	ROBOCOPY "%root%WizBot_Old\src\WizBot\bin" "%installtemp%WizBot\src\WizBot\bin" /E >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	ECHO.
	ECHO Old bin folder copied to new folder
	ROBOCOPY "%root%WizBot_Old\src\WizBot\data" "%installtemp%WizBot\src\WizBot\data" /E >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	ECHO.
	ECHO Old data folder copied to new folder
	::Moves the setup WizBot folder
	RMDIR "%root%WizBot\" /S /Q >nul 2>&1
	ROBOCOPY "%root%WizBotInstall_Temp" "%rootdir%" /E /MOVE >nul 2>&1
	IF %ERRORLEVEL% GEQ 8 (GOTO :copyerror)
	GOTO :end
:dotnet
	::Terminates the batch script if it can't run dotnet --version
	TITLE Error!
	ECHO dotnet not found, make sure it's been installed as per the guides instructions!
	ECHO Press any key to exit.
	PAUSE >nul 2>&1
	CD /D "%root%"
	GOTO :EOF
:git
	::Terminates the batch script if it can't run git --version
	TITLE Error!
	ECHO git not found, make sure it's been installed as per the guides instructions!
	ECHO Press any key to exit.
	PAUSE >nul 2>&1
	CD /D "%root%"
	GOTO :EOF
:giterror
	ECHO.
	ECHO Git clone failed, trying again
	RMDIR %installtemp% /S /Q >nul 2>&1
	GOTO :start
:copyerror
	::If at any point a copy error is encountered 
	TITLE Error!
	ECHO.
	ECHO An error in copying data has been encountered, returning an exit code of %ERRORLEVEL%
	ECHO.
	ECHO Make sure to close any files, such as `WizBot.db` before continuing or try running the installer as an Administrator
	PAUSE >nul 2>&1
	CD /D "%root%"
	GOTO :EOF
:end
	::Normal execution of end of script
	TITLE Installation complete!
	CD /D "%root%"
	RMDIR /S /Q "%installtemp%" >nul 2>&1
	ECHO.
	ECHO Installation complete, press any key to close this window!
	PAUSE >nul 2>&1
	del Latest.bat
