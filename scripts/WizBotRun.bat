@ECHO off
@TITLE WizBot
CD /D "%~dp0WizBot\src\WizBot"
dotnet run --configuration Release
ECHO WizBot has been succesfully stopped, press any key to close this window.
TITLE WizBot - Stopped
CD /D "%~dp0"
PAUSE >nul 2>&1
del WizBotRunNormal.bat
