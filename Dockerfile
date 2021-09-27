FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /source

COPY src/WizBot/*.csproj src/WizBot/
COPY src/WizBot.Coordinator/*.csproj src/WizBot.Coordinator/
COPY src/WizBot.Generators/*.csproj src/WizBot.Generators/
COPY src/ayu/Ayu.Discord.Voice/*.csproj src/ayu/Ayu.Discord.Voice/
RUN dotnet restore src/WizBot/

COPY . .
WORKDIR /source/src/WizBot
RUN set -xe; \
    dotnet --version; \
    dotnet publish -c Release -o /app --no-restore; \
    mv /app/data /app/data_init; \
    rm -Rf libopus* libsodium* opus.* runtimes/win* runtimes/osx* runtimes/linux-arm* runtimes/linux-mips*; \
    find /app -type f -exec chmod -x {} \; ;\
    chmod +x /app/WizBot

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim
WORKDIR /app

RUN set -xe; \
    useradd -m wizbot; \
    apt-get update; \
    apt-get install -y libopus0 libsodium23 libsqlite3-0 curl ffmpeg python3 sudo; \
    update-alternatives --install /usr/bin/python python /usr/bin/python3.7 1; \
    echo 'Defaults>wizbot env_keep+="ASPNETCORE_* DOTNET_* WizBot_* shard_id total_shards TZ"' > /etc/sudoers.d/wizbot; \
    curl -L https://yt-dl.org/downloads/latest/youtube-dl -o /usr/local/bin/youtube-dl; \
    chmod +x /usr/local/bin/youtube-dl

COPY --from=build /app ./
COPY docker-entrypoint.sh /usr/local/sbin

ENV shard_id=0
ENV total_shards=1

VOLUME [ "app/data" ]
ENTRYPOINT [ "/usr/local/sbin/docker-entrypoint.sh" ]
CMD dotnet WizBot.dll "$shard_id" "$total_shards"