# Use the .NET 8.0 SDK as the base image for the build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy the .csproj files for each project
COPY src/WizBot.Medusa/*.csproj src/WizBot.Medusa/
COPY src/WizBot/*.csproj src/WizBot/
COPY src/WizBot.Coordinator/*.csproj src/WizBot.Coordinator/
COPY src/WizBot.Generators/*.csproj src/WizBot.Generators/
COPY src/WizBot.Voice/*.csproj src/WizBot.Voice/
COPY NuGet.Config ./

# Restore the dependencies for the WizBot project
RUN dotnet restore src/WizBot/

# Copy the rest of the source code
COPY . .

# Set the working directory to the WizBot project
WORKDIR /source/src/WizBot

# Build and publish the WizBot project, then clean up unnecessary files
RUN set -xe; \
    dotnet --version; \
    dotnet publish -c Release -o /app --no-restore; \
    mv /app/data /app/data_init; \
    rm -Rf libopus* libsodium* opus.* runtimes/win* runtimes/osx* runtimes/linux-arm* runtimes/linux-mips*; \
    find /app -type f -exec chmod -x {} \; ;\
    chmod +x /app/WizBot

# Use the .NET 8.0 runtime as the base image for the final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Create a new user, install dependencies, and set up sudoers file
RUN set -xe; \
    useradd -m wizbot; \
    apt-get update; \
    apt-get install -y --no-install-recommends libsqlite3-0 curl ffmpeg sudo python3; \
    echo 'Defaults>wizbot env_keep+="ASPNETCORE_* DOTNET_* WizBot_* shard_id total_shards TZ"' > /etc/sudoers.d/wizbot; \
    curl -Lo /usr/local/bin/yt-dlp https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp; \
    chmod a+rx /usr/local/bin/yt-dlp; \
    apt-get autoremove -y; \
    apt-get autoclean -y

# Copy the built application and the entrypoint script from the build stage
COPY --from=build /app ./
COPY docker-entrypoint.sh /usr/local/sbin

# Set environment variables
ENV shard_id=0
ENV total_shards=1
ENV WizBot__creds=/app/data/creds.yml

# Define the data directory as a volume
VOLUME [ "/app/data" ]

# Set the entrypoint and default command
ENTRYPOINT [ "/usr/local/sbin/docker-entrypoint.sh" ]
CMD dotnet WizBot.dll "$shard_id" "$total_shards"