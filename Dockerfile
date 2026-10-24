# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Eerst alleen projectbestanden, zodat 'restore' gecachet blijft zolang de afhankelijkheden niet veranderen.
COPY Directory.Build.props ./
COPY src/Woordenspel.Domain/Woordenspel.Domain.csproj src/Woordenspel.Domain/
COPY src/Woordenspel.Application/Woordenspel.Application.csproj src/Woordenspel.Application/
COPY src/Woordenspel.Contracts/Woordenspel.Contracts.csproj src/Woordenspel.Contracts/
COPY src/Woordenspel.Client/Woordenspel.Client.csproj src/Woordenspel.Client/
COPY src/Woordenspel.Infrastructure/Woordenspel.Infrastructure.csproj src/Woordenspel.Infrastructure/
COPY src/Woordenspel.Api/Woordenspel.Api.csproj src/Woordenspel.Api/
RUN dotnet restore src/Woordenspel.Api/Woordenspel.Api.csproj

COPY data/ data/
COPY src/ src/
RUN dotnet publish src/Woordenspel.Api/Woordenspel.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# De database en de token-sleutel staan in /data; die map moet van de (niet-root) app-gebruiker zijn.
RUN mkdir /data && chown "$APP_UID" /data
COPY --from=build /app .

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Storage__DataDirectory=/data
EXPOSE 8080
VOLUME /data

ENTRYPOINT ["dotnet", "Woordenspel.Api.dll"]
