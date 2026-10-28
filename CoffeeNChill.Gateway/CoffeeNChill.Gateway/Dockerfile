FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CoffeeNChill.Gateway.csproj", "./"]
RUN dotnet restore "CoffeeNChill.Gateway.csproj"

COPY . .
RUN dotnet publish "CoffeeNChill.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CoffeeNChill.Gateway.dll"]