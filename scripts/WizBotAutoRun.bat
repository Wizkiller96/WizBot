@ECHO off
@TITLE WizBot
:auto
CD /D %~dp0WizBot\src\WizBot
dotnet run --configuration Release
goto auto
