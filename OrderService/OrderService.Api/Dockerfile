# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Contracts/Contracts.csproj Contracts/
COPY OrderService/OrderService.Api/OrderService.Api.csproj OrderService/OrderService.Api/

RUN dotnet restore OrderService/OrderService.Api/OrderService.Api.csproj

COPY . .
RUN dotnet publish OrderService/OrderService.Api/OrderService.Api.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.Api.dll"]