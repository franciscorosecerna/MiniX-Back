FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["MiniX.Backend.csproj", "./"]
RUN dotnet restore "./MiniX.Backend.csproj"

COPY . .
RUN dotnet publish "MiniX.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final: runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "MiniX.Backend.dll"]