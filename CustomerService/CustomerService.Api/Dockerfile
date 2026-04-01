# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Contracts/Contracts.csproj Contracts/
COPY CustomerService/CustomerService.Api/CustomerService.Api.csproj CustomerService/CustomerService.Api/

RUN dotnet restore CustomerService/CustomerService.Api/CustomerService.Api.csproj

COPY . .
RUN dotnet publish CustomerService/CustomerService.Api/CustomerService.Api.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CustomerService.Api.dll"]