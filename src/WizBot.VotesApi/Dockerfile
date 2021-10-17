FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/NadekoBot.VotesApi/WizBot.VotesApi.csproj", "WizBot.VotesApi/"]
RUN dotnet restore "src/WizBot.VotesApi/WizBot.VotesApi.csproj"
COPY . .
WORKDIR "/src/WizBot.VotesApi"
RUN dotnet build "WizBot.VotesApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WizBot.VotesApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WizBot.VotesApi.dll"]
