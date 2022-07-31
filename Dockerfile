FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

COPY src/WizBot.Medusa/*.csproj src/WizBot.Medusa/
COPY src/Wiz.Econ/*.csproj src/Wiz.Econ/
COPY src/Wiz.Common/*.csproj src/Wiz.Common/
COPY src/WizBot/*.csproj src/WizBot/
COPY src/WizBot.Coordinator/*.csproj src/WizBot.Coordinator/
COPY src/WizBot.Generators/*.csproj src/WizBot.Generators/
COPY src/ayu/Ayu.Discord.Voice/*.csproj src/ayu/Ayu.Discord.Voice/
COPY NuGet.Config ./
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
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app

RUN set -xe; \
    useradd -m wizbot; \
    apt-get update; \
    apt-get install -y --no-install-recommends libopus0 libsodium23 libsqlite3-0 curl ffmpeg python3 python3-pip sudo; \
    update-alternatives --install /usr/bin/python python /usr/bin/python3.9 1; \
    echo 'Defaults>wizbot env_keep+="ASPNETCORE_* DOTNET_* WizBot_* shard_id total_shards TZ"' > /etc/sudoers.d/wizbot; \
    pip3 install --no-cache-dir --upgrade youtube-dl; \
    apt-get purge -y python3-pip; \
    chmod +x /usr/local/bin/youtube-dl; \
    apt-get autoremove -y; \
    apt-get autoclean -y

COPY --from=build /app ./
COPY docker-entrypoint.sh /usr/local/sbin

ENV shard_id=0
ENV total_shards=1
ENV WizBot__creds=/app/data/creds.yml

VOLUME [ "/app/data" ]
ENTRYPOINT [ "/usr/local/sbin/docker-entrypoint.sh" ]
CMD dotnet WizBot.dll "$shard_id" "$total_shards"