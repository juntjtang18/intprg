# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Contracts/Contracts.csproj Contracts/
COPY ProductService/ProductService.Api/ProductService.Api.csproj ProductService/ProductService.Api/

RUN dotnet restore ProductService/ProductService.Api/ProductService.Api.csproj

COPY . .
RUN dotnet publish ProductService/ProductService.Api/ProductService.Api.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductService.Api.dll"]