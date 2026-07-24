# Multi-stage build for ASP.NET Core 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["WorldCupPredictor.csproj", "./"]
RUN dotnet restore "WorldCupPredictor.csproj"

COPY . .
RUN dotnet publish "WorldCupPredictor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
EXPOSE 8080

ENTRYPOINT ["dotnet", "WorldCupPredictor.dll"]
