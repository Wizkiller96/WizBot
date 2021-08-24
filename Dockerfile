FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

COPY src/NadekoBot/*.csproj src/NadekoBot/
COPY src/NadekoBot.Coordinator/*.csproj src/NadekoBot.Coordinator/
COPY src/NadekoBot.Generators/*.csproj src/NadekoBot.Generators/
COPY src/ayu/Ayu.Discord.Voice/*.csproj src/ayu/Ayu.Discord.Voice/
RUN dotnet restore src/NadekoBot/

COPY . .
WORKDIR /source/src/NadekoBot
RUN dotnet --version
RUN dotnet publish -c Release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:5.0
ENV shard_id=0
ENV total_shards=1
WORKDIR /app
COPY --from=build /app ./
VOLUME [ "app/data", "app/creds.yml", "app/creds_example.yml" ]
ENTRYPOINT dotnet NadekoBot.dll "$shard_id" "$total_shards"