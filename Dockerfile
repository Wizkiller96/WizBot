# Use the .NET 8.0 SDK as the base image for the build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy the .csproj files for each project
COPY src/Nadeko.Medusa/*.csproj src/Nadeko.Medusa/
COPY src/NadekoBot/*.csproj src/NadekoBot/
COPY src/NadekoBot/creds_example.yml src/NadekoBot/
COPY src/NadekoBot.Coordinator/*.csproj src/NadekoBot.Coordinator/
COPY src/NadekoBot.Generators/*.csproj src/NadekoBot.Generators/
COPY src/NadekoBot.Voice/*.csproj src/NadekoBot.Voice/
COPY NuGet.Config ./

# Restore the dependencies for the NadekoBot project
RUN dotnet restore src/NadekoBot/

# Copy the rest of the source code
COPY . .

# Set the working directory to the NadekoBot project
WORKDIR /source/src/NadekoBot

# Build and publish the NadekoBot project, then clean up unnecessary files
RUN set -xe; \
    dotnet --version; \
    dotnet publish -c Release -o /app --no-restore; \
    mv /app/data /app/data_init; \
    rm -Rf libopus* libsodium* opus.* runtimes/win* runtimes/osx* runtimes/linux-arm* runtimes/linux-mips*; \
    find /app -type f -exec chmod -x {} \; ;\
    chmod +x /app/NadekoBot

# Use the .NET 8.0 runtime as the base image for the final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Create a new user, install dependencies, and set up sudoers file
RUN set -xe; \
    useradd -m nadeko; \
    apt-get update; \
    apt-get install -y --no-install-recommends libsqlite3-0 curl ffmpeg sudo python3; \
    echo 'Defaults>nadeko env_keep+="ASPNETCORE_* DOTNET_* NadekoBot_* shard_id total_shards TZ"' > /etc/sudoers.d/nadeko; \
    curl -Lo /usr/local/bin/yt-dlp https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp; \
    chmod a+rx /usr/local/bin/yt-dlp; \
    apt-get autoremove -y; \
    apt-get autoclean -y

# Copy creds_example.yml from the build stage
COPY --from=build /source/src/NadekoBot/creds_example.yml /source/src/NadekoBot/

# Check if creds.yml exists, if not, copy and rename creds_example.yml
RUN mkdir -p /app/data && if [ ! -f /app/data/creds.yml ]; then cp /source/src/NadekoBot/creds_example.yml /app/data/creds.yml; fi

# Copy the built application and the entrypoint script from the build stage
COPY --from=build /app ./
COPY docker-entrypoint.sh /usr/local/sbin

# Set environment variables
ENV shard_id=0
ENV total_shards=1
ENV NadekoBot__creds=/app/data/creds.yml

# Define the data directory as a volume
VOLUME [ "/app/data" ]

# Set the entrypoint and default command
ENTRYPOINT [ "/usr/local/sbin/docker-entrypoint.sh" ]
CMD dotnet NadekoBot.dll "$shard_id" "$total_shards"
