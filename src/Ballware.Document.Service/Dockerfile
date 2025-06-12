FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

COPY ./Ballware.Document.Jobs ./Ballware.Document.Jobs
COPY ./Ballware.Document.Service ./Ballware.Document.Service
COPY ./Ballware.Meta.Client ./Ballware.Meta.Client
COPY ./Ballware.Generic.Client ./Ballware.Generic.Client
COPY ./Ballware.Storage.Client ./Ballware.Storage.Client

WORKDIR "/src/Ballware.Document.Service"
RUN dotnet build "Ballware.Document.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ballware.Document.Service.csproj" -c Release -o /app/publish

FROM base AS final
USER app
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Ballware.Document.Service.dll"]