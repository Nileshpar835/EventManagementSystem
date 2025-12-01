# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the project file
COPY EventManagementSystem/*.csproj EventManagementSystem/
RUN dotnet restore EventManagementSystem/EventManagementSystem.csproj

# Copy everything
COPY . .

# Publish the project
RUN dotnet publish EventManagementSystem/EventManagementSystem.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks (Render requirement)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render uses dynamic PORT env
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
EXPOSE 8080

ENTRYPOINT ["dotnet", "EventManagementSystem.dll"]
