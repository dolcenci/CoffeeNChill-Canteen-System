FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
WORKDIR /home/site/wwwroot
COPY --from=build /app/out .

# Set environment variables
ENV AzureWebJobsStorage=UseDevelopmentStorage=true
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

EXPOSE 80