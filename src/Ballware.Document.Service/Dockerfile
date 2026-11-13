FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

ARG GITHUB_USERNAME
ARG GITHUB_PAT
ARG DEVEXPRESS_NUGETFEED
RUN [ -z "$DEVEXPRESS_NUGETFEED" ] && echo "DEVEXPRESS_NUGETFEED needs to be defined" && exit 1 || true
ARG DEVEXPRESS_LICENSE
RUN [ -z "$DEVEXPRESS_LICENSE" ] && echo "DEVEXPRESS_LICENSE needs to be defined" && exit 1 || true

WORKDIR /app
EXPOSE 5000
EXPOSE 5001

RUN echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections \
   && apt-get update \
   && apt-get install -y --no-install-recommends \
   libc6 \
   libicu-dev \
   libfontconfig1 \
   ttf-mscorefonts-installer \
   libjpeg-turbo8 \
   libjpeg62 \
   && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG GITHUB_USERNAME
ARG GITHUB_PAT
ARG DEVEXPRESS_NUGETFEED
ARG DEVEXPRESS_LICENSE

WORKDIR /src

COPY ./Ballware.Document.Api ./Ballware.Document.Api
COPY ./Ballware.Document.Data ./Ballware.Document.Data
COPY ./Ballware.Document.Data.Ef ./Ballware.Document.Data.Ef
COPY ./Ballware.Document.Data.Ef.Postgres ./Ballware.Document.Data.Ef.Postgres
COPY ./Ballware.Document.Data.Ef.SqlServer ./Ballware.Document.Data.Ef.SqlServer
COPY ./Ballware.Document.Engine.Dx ./Ballware.Document.Engine.Dx
COPY ./Ballware.Document.Hosting ./Ballware.Document.Hosting
COPY ./Ballware.Document.Jobs ./Ballware.Document.Jobs
COPY ./Ballware.Document.Metadata ./Ballware.Document.Metadata
COPY ./Ballware.Document.Service ./Ballware.Document.Service
COPY ./Ballware.Document.Session ./Ballware.Document.Session

WORKDIR "/src/Ballware.Document.Service"
RUN dotnet nuget add source "https://nuget.pkg.github.com/ballware/index.json" --name github --username "$GITHUB_USERNAME" --password "$GITHUB_PAT" --store-password-in-clear-text
RUN dotnet nuget add source "$DEVEXPRESS_NUGETFEED" --name devexpress
RUN mkdir -p "$HOME/.config/DevExpress" && printf '%s' "$DEVEXPRESS_LICENSE" > "$HOME/.config/DevExpress/DevExpress_License.txt"
RUN dotnet restore "Ballware.Document.Service.csproj"
RUN dotnet build "Ballware.Document.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ballware.Document.Service.csproj" -c Release -o /app/publish

FROM base AS final
USER app
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Ballware.Document.Service.dll"]
