# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0.405 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0.15
WORKDIR /app
COPY --from=build /app .

# Install curl for healthcheck
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Create non-root user for security
RUN useradd -m myappuser && chown -R myappuser:myappuser /app
USER myappuser

EXPOSE 80
ENTRYPOINT ["dotnet", "DomainStatusChecker.dll"]