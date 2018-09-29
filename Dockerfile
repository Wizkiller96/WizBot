FROM microsoft/dotnet:2.1-sdk-alpine AS build

COPY . /WizBot

WORKDIR /WizBot/src/WizBot
RUN set -ex; \
    dotnet restore; \
    dotnet build -c Release; \
    dotnet publish -c Release -o /app

WORKDIR /app
RUN set -ex; \
    rm libopus.so libsodium.dll libsodium.so opus.dll; \
    find . -type f -exec chmod -x {} \;; \
    rm -R runtimes/win* runtimes/osx* runtimes/linux-*

FROM microsoft/dotnet:2.1-runtime-alpine AS runtime
WORKDIR /app
COPY --from=build /app /app
RUN set -ex; \
	echo '@edge http://dl-cdn.alpinelinux.org/alpine/edge/main' >> /etc/apk/repositories; \
    echo '@edge http://dl-cdn.alpinelinux.org/alpine/edge/community' >> /etc/apk/repositories; \
    apk add --no-cache \
        ffmpeg \
        youtube-dl@edge \
        libsodium \
        opus; \
    adduser -D wizbot; \
    chown wizbot /app; \
    chmod u+w /app; \
    install -d -o wizbot -g wizbot -m 755 /app/data

# workaround for the runtime to find the native libs loaded through DllImport
RUN set -ex; \
    ln -s /usr/lib/libopus.so.0 /app/libopus.so; \
    ln -s /usr/lib/libsodium.so.23 /app/libsodium.so

VOLUME [ "/app/data" ]
USER wizbot
CMD ["dotnet", "/app/WizBot.dll"]
