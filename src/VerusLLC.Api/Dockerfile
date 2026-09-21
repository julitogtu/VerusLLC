FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

COPY Directory.Packages.props ./
COPY src/VerusLLC.Domain/VerusLLC.Domain.csproj src/VerusLLC.Domain/
COPY src/VerusLLC.Application/VerusLLC.Application.csproj src/VerusLLC.Application/
COPY src/VerusLLC.Persistence/VerusLLC.Persistence.csproj src/VerusLLC.Persistence/
COPY src/VerusLLC.Api/VerusLLC.Api.csproj src/VerusLLC.Api/
RUN dotnet restore src/VerusLLC.Api/VerusLLC.Api.csproj

COPY src/ src/
RUN dotnet publish src/VerusLLC.Api/VerusLLC.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app ./

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "VerusLLC.Api.dll"]
