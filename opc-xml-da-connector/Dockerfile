#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["opc-xml-da-connector/opc-xml-da-connector.csproj", "opc-xml-da-connector/"]
RUN dotnet restore "opc-xml-da-connector/opc-xml-da-connector.csproj"
COPY . .
WORKDIR "/src/opc-xml-da-connector"
RUN dotnet build "opc-xml-da-connector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "opc-xml-da-connector.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "opc-xml-da-connector.dll"]