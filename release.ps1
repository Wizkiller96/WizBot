function Get-Changelog()
{
    $lastTag = git describe --tags --abbrev=0
    $tag = "$lastTag..HEAD"

    $clArr = (& 'git' 'log', $tag, '--oneline')
    [array]::Reverse($clArr)
    $changelog = $clArr | where { "$_" -notlike "*(POEditor.com)*" -and "$_" -notlike "*Merge branch*" -and "$_" -notlike "*Merge pull request*" -and "$_" -notlike "^-*" -and "$_" -notlike "*Merge remote tracking*" }
    $changelog = [string]::join([Environment]::NewLine, $changelog)

    $cl2 = $clArr | where { "$_" -like "*Merge pull request*" }
    $changelog = "## Changes$nl$changelog"
    if ($null -ne $cl2) {
        $cl2 = [string]::join([Environment]::NewLine, $cl2)
        $changelog = $changelog + "$nl ## Pull Requests Merged$nl$cl2"
    }
}

function Build-Installer($versionNumber)
{
    $env:WIZBOT_INSTALL_VERSION = $versionNumber

    dotnet publish -c Release --runtime win7-x64
    .\rcedit-x64.exe "src\WizBot\bin\Release\netcoreapp2.1\win7-x64\wizbot.exe" --set-icon "src\WizBot\bin\Release\netcoreapp2.1\win7-x64\wizbot_icon.ico"

    & "iscc.exe" "/O+" ".\WizBot.iss"

    $path = [Environment]::GetFolderPath('MyDocuments') + "\_projekti\WizBotInstallerOutput\WizBot-setup-$versionNumber.exe";
    $dest = [Environment]::GetFolderPath('MyDocuments') + "\_projekti\WizBotInstallerOutput\wizbot-setup.exe";
    Copy-Item -Path $path -Destination $dest -Force
}

function GitHub-Release($versionNumber) {
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    $ErrorActionPreference = "Stop"

    git pull
    git push #making sure commit id exists on remote

    $nl = [Environment]::NewLine
    $env:WIZBOT_INSTALL_VERSION = $versionNumber
    $gitHubApiKey = $env:GITHUB_API_KEY
    
    $commitId = git rev-parse HEAD

    $changelog = Get-Changelog

    Write-Host $changelog 


    # set-alias sz "$env:ProgramFiles\7-Zip\7z.exe" 
    # $source = "src\WizBot\bin\Release\PublishOutput\win7-x64" 
    # $target = "src\WizBot\bin\Release\PublishOutput\WizBot.7z"

    # sz 'a' '-mx3' $target $source

    Build-Installer
    $artifact = "wizbot-setup.exe";
    $auth = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($gitHubApiKey + ":x-oauth-basic"));
    Write-Host $changelog
    $result = GitHubMake-Release $versionNumber $commitId $TRUE $gitHubApiKey $auth "" "$changelog"
    $releaseId = $result | Select-Object -ExpandProperty id
    $uploadUri = $result | Select-Object -ExpandProperty upload_url
    $uploadUri = $uploadUri -creplace '\{\?name,label\}', "?name=$artifact"
    Write-Host $releaseId $uploadUri
    $uploadFile = [Environment]::GetFolderPath('MyDocuments') + "\projekti\WizBotInstallerOutput\$artifact"

    $uploadParams = @{
        Uri         = $uploadUri;
        Method      = 'POST';
        Headers     = @{
            Authorization = $auth;
        }
        ContentType = 'application/x-msdownload';
        InFile      = $uploadFile
    }

    Write-Host 'Uploading artifact'
    $result = Invoke-RestMethod @uploadParams
    Write-Host 'Artifact upload finished.'
    $result = GitHubMake-Release $versionNumber $commitId $FALSE $gitHubApiKey $auth "$releaseId"
    git pull
    Write-Host 'Done 🎉'
}

function GitHubMake-Release($versionNumber, $commitId, $draft, $gitHubApiKey, $auth, $releaseId, $body) {
    $releaseId = If ($releaseId -eq "") {""} Else {"/" + $releaseId};

    Write-Host $versionNumber
    Write-Host $commitId
    Write-Host $draft
    Write-Host $releaseId
    Write-Host $body

    $releaseData = @{
        tag_name         = $versionNumber;
        target_commitish = $commitId;
        name             = [string]::Format("WizBot v{0}", $versionNumber);
        body             = $body;
        draft            = $draft;
        prerelease       = $releaseId -ne "";
    }

    $releaseParams = @{
        Uri         = "https://api.github.com/repos/Wizkiller96/WizBot/releases" + $releaseId;
        Method      = 'POST';
        Headers     = @{
            Authorization = $auth;
        }
        ContentType = 'application/json';
        Body        = (ConvertTo-Json $releaseData -Compress)
    }
    return Invoke-RestMethod @releaseParams
}