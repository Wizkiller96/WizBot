FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY src/Wiz.Medusa/*.csproj src/Wiz.Medusa/
COPY src/WizBot/*.csproj src/WizBot/
COPY src/WizBot.Coordinator/*.csproj src/WizBot.Coordinator/
COPY src/WizBot.Generators/*.csproj src/WizBot.Generators/
COPY src/WizBot.Voice/*.csproj src/WizBot.Voice/
COPY src/WizBot.GrpcApiBase/*.csproj src/WizBot.GrpcApiBase/

RUN DOTNET_RID="linux-musl-$([ "$TARGETARCH" = "arm64" ] && echo "arm64" || echo "x64")" \
    && echo "$DOTNET_RID" > /tmp/rid
RUN dotnet restore src/WizBot/ -r $(cat /tmp/rid)

COPY . .
WORKDIR /source/src/WizBot

RUN dotnet publish -c Release -o /app --self-contained -r $(cat /tmp/rid) --no-restore \
    && mv /app/data /app/data_init \
    && chmod +x /app/WizBot

FROM alpine:3.23
ARG TARGETARCH
WORKDIR /app

RUN YT_DLP_BIN="yt-dlp_musllinux$([ "$TARGETARCH" = "arm64" ] && echo "_aarch64" || echo "")" \
    && wget -O /usr/local/bin/yt-dlp "https://github.com/yt-dlp/yt-dlp/releases/latest/download/${YT_DLP_BIN}" \
    && chmod 755 /usr/local/bin/yt-dlp

RUN apk add --no-cache ffmpeg libsodium opus deno
RUN apk add --no-cache libstdc++ libgcc icu-libs libc6-compat tzdata

COPY --from=build /app ./
COPY docker-entrypoint.sh /usr/local/sbin/

RUN rm -f /app/data_init/lib/libsodium.so /app/data_init/lib/opus.so \
    && ln -sf /usr/lib/libsodium.so.26 /app/data_init/lib/libsodium.so \
    && ln -sf /usr/lib/libopus.so.0 /app/data_init/lib/opus.so

VOLUME [ "/app/data" ]

ENTRYPOINT [ "/usr/local/sbin/docker-entrypoint.sh" ]
CMD [ "./WizBot" ]