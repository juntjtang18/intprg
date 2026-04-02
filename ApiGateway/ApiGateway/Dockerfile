FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ApiGateway/ApiGateway/ApiGateway.csproj ApiGateway/ApiGateway/
RUN dotnet restore ApiGateway/ApiGateway/ApiGateway.csproj

COPY . .
RUN dotnet publish ApiGateway/ApiGateway/ApiGateway.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
