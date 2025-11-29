# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Restore dependencies
RUN dotnet restore

# Publish app
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Render will inject PORT env var
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# Start the app
ENTRYPOINT ["dotnet", "EventManagementSystem.dll"]
