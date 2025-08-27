FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base

ARG GITHUB_PAT
ARG DEVEXPRESS_NUGETFEED
RUN [ -z "$DEVEXPRESS_NUGETFEED" ] && echo "DEVEXPRESS_NUGETFEED needs to be defined" && exit 1 || true

WORKDIR /app
EXPOSE 5000
EXPOSE 5001

RUN echo "deb https://httpredir.debian.org/debian bookworm main contrib" > /etc/apt/sources.list \
   && echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections \
   && apt-get update \
   && apt install -y apt-transport-https gnupg ca-certificates \
   && apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
   && echo "deb https://download.mono-project.com/repo/debian stable-buster main" >> /etc/apt/sources.list | tee /etc/apt/sources.list.d/mono-official-stable.list \
   && apt-get update \
   && apt-get install -y \
   libc6 \
   libicu-dev \
   libfontconfig1 \
   ttf-mscorefonts-installer \
   libjpeg62-turbo \
   && rm -rf /var/lib/apt/lists/* 

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

ARG GITHUB_PAT
ARG DEVEXPRESS_NUGETFEED

RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
   && apt-get update \
   && apt-get install -y \
   nodejs \
   && rm -rf /var/lib/apt/lists/* 

RUN node -v \
    && npm -v

WORKDIR /src

COPY ./Ballware.Document.Api ./Ballware.Document.Api
COPY ./Ballware.Document.Data ./Ballware.Document.Data
COPY ./Ballware.Document.Data.Ef ./Ballware.Document.Data.Ef
COPY ./Ballware.Document.Data.Ef.Postgres ./Ballware.Document.Data.Ef.Postgres
COPY ./Ballware.Document.Data.Ef.SqlServer ./Ballware.Document.Data.Ef.SqlServer
COPY ./Ballware.Document.Engine.Dx ./Ballware.Document.Engine.Dx
COPY ./Ballware.Document.Jobs ./Ballware.Document.Jobs
COPY ./Ballware.Document.Metadata ./Ballware.Document.Metadata
COPY ./Ballware.Document.Service ./Ballware.Document.Service
COPY ./Ballware.Document.Session ./Ballware.Document.Session

WORKDIR "/src/Ballware.Document.Service"
RUN dotnet nuget add source "https://nuget.pkg.github.com/ballware/index.json" --name github --username frankball --password "$GITHUB_PAT" --store-password-in-clear-text
RUN dotnet nuget add source "$DEVEXPRESS_NUGETFEED" --name devexpress
RUN dotnet restore "Ballware.Document.Service.csproj"
RUN dotnet build "Ballware.Document.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ballware.Document.Service.csproj" -c Release -o /app/publish

FROM base AS final
USER app
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Ballware.Document.Service.dll"]