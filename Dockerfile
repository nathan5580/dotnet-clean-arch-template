# syntax=docker/dockerfile:1

# =====================================================
# Stage 1: Build frontend assets
# =====================================================
FROM node:22-alpine AS frontend

WORKDIR /src

COPY Applications/Web/package.json Applications/Web/package-lock.json ./Applications/Web/
RUN cd Applications/Web && npm ci

COPY Applications/Web/ ./Applications/Web/
RUN cd Applications/Web && npx tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css --minify

# =====================================================
# Stage 2: Build .NET backend
# =====================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY {{ProjectName}}.slnx Directory.Build.props Directory.Packages.props ./
COPY Applications/Api/Api.csproj Applications/Api/
COPY Applications/Web/Web.csproj Applications/Web/
COPY Databases/*/*.csproj Databases/
COPY Shared/*/*.csproj Shared/

RUN dotnet restore {{ProjectName}}.slnx

COPY . .
COPY --from=frontend /src/Applications/Web/wwwroot/css/app.css ./Applications/Web/wwwroot/css/app.css
COPY --from=frontend /src/Applications/Web/node_modules ./Applications/Web/node_modules

RUN dotnet publish Applications/Api/Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app \
    -p:BlazorEnableCompression=true \
    -p:PublishSingleFile=false

# =====================================================
# Stage 3: Runtime
# =====================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

USER $APP_UID

COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
