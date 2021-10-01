FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /source

COPY src/NadekoBot/*.csproj src/NadekoBot/
COPY src/NadekoBot.Coordinator/*.csproj src/NadekoBot.Coordinator/
COPY src/NadekoBot.Generators/*.csproj src/NadekoBot.Generators/
COPY src/ayu/Ayu.Discord.Voice/*.csproj src/ayu/Ayu.Discord.Voice/
RUN dotnet restore src/NadekoBot/

COPY . .
WORKDIR /source/src/NadekoBot
RUN set -xe; \
    dotnet --version; \
    dotnet publish -c Release -o /app --no-restore; \
    mv /app/data /app/data_init; \
    rm -Rf libopus* libsodium* opus.* runtimes/win* runtimes/osx* runtimes/linux-arm* runtimes/linux-mips*; \
    find /app -type f -exec chmod -x {} \; ;\
    chmod +x /app/NadekoBot

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim
WORKDIR /app

RUN set -xe; \
    useradd -m nadeko; \
    apt-get update; \
    apt-get install -y libopus0 libsodium23 libsqlite3-0 curl ffmpeg python3 python3-pip sudo; \
    update-alternatives --install /usr/bin/python python /usr/bin/python3.7 1; \
    echo 'Defaults>nadeko env_keep+="ASPNETCORE_* DOTNET_* NadekoBot_* shard_id total_shards TZ"' > /etc/sudoers.d/nadeko; \
    pip3 install --upgrade youtube-dl; \
    apt-get remove -y python3-pip; \
    chmod +x /usr/local/bin/youtube-dl

COPY --from=build /app ./
COPY docker-entrypoint.sh /usr/local/sbin

ENV shard_id=0
ENV total_shards=1

VOLUME [ "app/data" ]
ENTRYPOINT [ "/usr/local/sbin/docker-entrypoint.sh" ]
CMD dotnet NadekoBot.dll "$shard_id" "$total_shards"