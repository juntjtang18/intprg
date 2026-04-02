FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Contracts/Contracts.csproj Contracts/
COPY PaymentService/PaymentService.Api/PaymentService.Api.csproj PaymentService/PaymentService.Api/

RUN dotnet restore PaymentService/PaymentService.Api/PaymentService.Api.csproj

COPY . .
RUN dotnet publish PaymentService/PaymentService.Api/PaymentService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.Api.dll"]
