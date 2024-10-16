#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Agenda.API/Agenda.API.csproj", "src/Agenda.API/"]
COPY ["src/Agenda.DataStores.Postgres/Agenda.DataStores.Postgres.csproj", "src/Agenda.DataStores.Postgres/"]
COPY ["src/Agenda.DataStores/Agenda.DataStores.csproj", "src/Agenda.DataStores/"]
COPY ["src/Agenda.Objects/Agenda.Objects.csproj", "src/Agenda.Objects/"]
COPY ["src/Agenda.Ids/Agenda.Ids.csproj", "src/Agenda.Ids/"]
COPY ["src/Agenda.DataStores.Sqlite/Agenda.DataStores.Sqlite.csproj", "src/Agenda.DataStores.Sqlite/"]
RUN dotnet restore "src/Agenda.API/Agenda.API.csproj"
COPY . .
WORKDIR "/src/src/Agenda.API"
RUN dotnet build "Agenda.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Agenda.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Agenda.API.dll"]