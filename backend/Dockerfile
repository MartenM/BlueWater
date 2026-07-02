# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Bluewater.sln ./
COPY src/Bluewater.Api/Bluewater.Api.csproj src/Bluewater.Api/
COPY src/Bluewater.Core/Bluewater.Core.csproj src/Bluewater.Core/
COPY src/Bluewater.Domain/Bluewater.Domain.csproj src/Bluewater.Domain/
COPY src/Bluewater.Infra/Bluewater.Infra.csproj src/Bluewater.Infra/
COPY src/Bluewater.Tests/Bluewater.Tests.csproj src/Bluewater.Tests/
RUN dotnet restore Bluewater.sln

COPY . .
RUN dotnet publish src/Bluewater.Api/Bluewater.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bluewater.Api.dll"]
