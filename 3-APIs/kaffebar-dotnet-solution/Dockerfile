# Kaffebar API — .NET-sporet. Fasit fra samling 3, datakilde for samling 4.
#
# Multi-stage, samme prinsipp som 1-DevOps/2-Docker/Dockerfile: bygg i et image
# med hele SDK-et, kjør i et som bare har runtime.

# --- Stage 1: bygg ----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Prosjektfila først, i sitt eget lag — da caches NuGet-restore mellom bygg.
COPY Kaffebar/Kaffebar.csproj Kaffebar/
RUN dotnet restore Kaffebar/Kaffebar.csproj

COPY Kaffebar/ Kaffebar/
RUN dotnet publish Kaffebar/Kaffebar.csproj -c Release -o /app --no-restore

# --- Stage 2: kjøring -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runner
WORKDIR /app

# curl trengs av healthchecken i docker-compose.yml.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# aspnet-imaget har hatt en innebygd ikke-root-bruker «app» siden .NET 8.
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Kaffebar.dll"]
