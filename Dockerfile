# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CodeHub.slnx ./
COPY CodeHub.Domain/CodeHub.Domain.csproj CodeHub.Domain/
COPY CodeHub.Application/CodeHub.Application.csproj CodeHub.Application/
COPY CodeHub.Infrastructure/CodeHub.Infrastructure.csproj CodeHub.Infrastructure/
COPY CodeHub.Api/CodeHub.Api.csproj CodeHub.Api/
RUN dotnet restore CodeHub.slnx

COPY . .
RUN dotnet publish CodeHub.Api/CodeHub.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Npgsql needs the GSSAPI library; without it the process segfaults at startup.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CodeHub.Api.dll"]
