# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Ecommerce.Cart/Cart.API/Cart.API.csproj", "Services/Ecommerce.Cart/Cart.API/"]
COPY ["BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "BuildingBlocks/BuildingBlocks.Messaging/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]
COPY ["Services/Ecommerce.Production/Production.API/Production.API.csproj", "Services/Ecommerce.Production/Production.API/"]
COPY ["Services/Ecommerce.Production/Production.Application/Production.Application.csproj", "Services/Ecommerce.Production/Production.Application/"]
COPY ["Services/Ecommerce.Production/Production.Domain/Production.Domain.csproj", "Services/Ecommerce.Production/Production.Domain/"]
COPY ["Services/Ecommerce.Production/Production.Infrastructure/Production.Infrastructure.csproj", "Services/Ecommerce.Production/Production.Infrastructure/"]
COPY ["Services/Ecommerce.Cart/Cart.Application/Cart.Application.csproj", "Services/Ecommerce.Cart/Cart.Application/"]
COPY ["Services/Ecommerce.Order/Order.Application/Order.Application.csproj", "Services/Ecommerce.Order/Order.Application/"]
COPY ["Services/Ecommerce.Order/Order.Domain/Order.Domain.csproj", "Services/Ecommerce.Order/Order.Domain/"]
COPY ["Services/Ecommerce.Cart/Cart.Domain/Cart.Domain.csproj", "Services/Ecommerce.Cart/Cart.Domain/"]
COPY ["Services/Ecommerce.Cart/Cart.Infrastructure/Cart.Infrastructure.csproj", "Services/Ecommerce.Cart/Cart.Infrastructure/"]
RUN dotnet restore "./Services/Ecommerce.Cart/Cart.API/Cart.API.csproj"
COPY . .
WORKDIR "/src/Services/Ecommerce.Cart/Cart.API"
RUN dotnet build "./Cart.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cart.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cart.API.dll"]