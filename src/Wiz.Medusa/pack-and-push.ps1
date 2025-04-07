dotnet pack -o bin/Release/packed
dotnet nuget push bin/Release/packed/ --api-key $env:wizbot_myget_api_key --source https://www.myget.org/F/wizbot/api/v2/package