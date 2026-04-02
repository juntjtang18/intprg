FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Frontend/Frontend.Ui/Frontend.Ui.csproj Frontend/Frontend.Ui/
RUN dotnet restore Frontend/Frontend.Ui/Frontend.Ui.csproj

COPY . .
RUN dotnet publish Frontend/Frontend.Ui/Frontend.Ui.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Frontend.Ui.dll"]
