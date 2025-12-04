# Dockerfile for .NET 8.0 Application
# This Dockerfile fixes the "dotnet: command not found" error on Render

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj file and restore dependencies
# If Dockerfile is at SelfOrderingSystemKiosk/Dockerfile, the build context is repo root
# So we need to copy from SelfOrderingSystemKiosk/ folder
COPY SelfOrderingSystemKiosk/*.csproj ./SelfOrderingSystemKiosk/
WORKDIR /src/SelfOrderingSystemKiosk
RUN dotnet restore

# Copy everything else and build
COPY SelfOrderingSystemKiosk/ ./
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Expose port (Render will set PORT env var, but default to 8080)
EXPOSE 8080

# Set environment - Render will override PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "SelfOrderingSystemKiosk.dll"]

