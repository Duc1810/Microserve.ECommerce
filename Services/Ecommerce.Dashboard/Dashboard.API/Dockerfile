# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Ecommerce.Dashboard/Dashboard.API/Dashboard.API.csproj", "Services/Ecommerce.Dashboard/Dashboard.API/"]
COPY ["Services/Ecommerce.Dashboard/Dashboard.Infrastructure/Dashboard.Infrastructure.csproj", "Services/Ecommerce.Dashboard/Dashboard.Infrastructure/"]
COPY ["Services/Ecommerce.Dashboard/Dashboard.Application/Dashboard.Application.csproj", "Services/Ecommerce.Dashboard/Dashboard.Application/"]
COPY ["Services/Ecommerce.Dashboard/Dashboard.Domain/Dashboard.Domain.csproj", "Services/Ecommerce.Dashboard/Dashboard.Domain/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]
COPY ["BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "BuildingBlocks/BuildingBlocks.Messaging/"]
RUN dotnet restore "./Services/Ecommerce.Dashboard/Dashboard.API/Dashboard.API.csproj"
COPY . .
WORKDIR "/src/Services/Ecommerce.Dashboard/Dashboard.API"
RUN dotnet build "./Dashboard.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Dashboard.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dashboard.API.dll"]