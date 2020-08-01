function Get-Changelog($lastTag)
{
    if(!$lastTag)
    {
        $lastTag = git describe --tags --abbrev=0
    }

    $tag = "$lastTag..HEAD"

    $clArr = (git log $tag --oneline)
    [array]::Reverse($clArr)
    $changelog = $clArr | where { "$_" -notlike "*(POEditor.com)*" -and "$_" -notlike "*Merge branch*" -and "$_" -notlike "*Merge pull request*" -and "$_" -notlike "^-*" -and "$_" -notlike "*Merge remote tracking*" }
    $changelog = [string]::join([Environment]::NewLine, $changelog)

    $cl2 = $clArr | where { "$_" -like "*Merge pull request*" }
    $changelog = "## Changes$nl$changelog"
    if ($null -ne $cl2) {
        $cl2 = [string]::join([Environment]::NewLine, $cl2)
        $changelog = $changelog + "$nl ## Pull Requests Merged$nl$cl2"
    }

    return $changelog
}

function Build-Installer($versionNumber)
{
    $env:WIZBOT_INSTALL_VERSION = $versionNumber

	dotnet clean
    # rm -r -fo "src\WizBot\bin"
    dotnet publish -c Release --runtime win7-x64 /p:Version=$versionNumber
    .\rcedit-x64.exe "src\WizBot\bin\Release\netcoreapp2.1\win7-x64\wizbot.exe" --set-icon "src\WizBot\bin\Release\netcoreapp2.1\win7-x64\wizbot_icon.ico"

    & "iscc.exe" "/O+" ".\exe_builder.iss"

    Write-ReleaseFile($versionNumber)
    # $path = [Environment]::GetFolderPath('MyDocuments') + "\_projekti\new_installer\$versionNumber\";
    # $binPath = $path + "wizbot-setup-$versionNumber.exe";
    # Copy-Item -Path $path -Destination $dest -Force -ErrorAction Stop

	# return $path
}

function Write-ReleaseFile($versionNumber) {	
    $changelog = ""
	# pull the changes if they exist
	# git pull
	# attempt to build teh installer
	# $path = Build-Installer $versionNumber

	# get changelog before tagging
    $changelog = Get-Changelog
	# tag the release
	# & (git tag, $tag)

	# print out the changelog to the console
    # Write-Host $changelog

	$jsonReleaseFile = "[{""VersionName"": ""$versionNumber"", ""DownloadLink"": ""https://wizbot.cc/releases/wizbot-setup-$versionNumber.exe"", ""Changelog"": """"}]"

	$releaseJsonOutPath = [Environment]::GetFolderPath('MyDocuments') + "\_projekti\wizbot-installers\$versionNumber\"
	New-Item -Path $releaseJsonOutPath -Value $jsonReleaseFile -Name "releases.json" -Force
}
