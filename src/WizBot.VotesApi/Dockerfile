FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WizBot.VotesApi.csproj", "WizBot.VotesApi/"]
RUN dotnet restore "WizBot.VotesApi/WizBot.VotesApi.csproj"
COPY . "WizBot.VotesApi/"
WORKDIR "/src/WizBot.VotesApi"
RUN dotnet build "WizBot.VotesApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WizBot.VotesApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WizBot.VotesApi.dll"]
