#!/bin/bash

# SQL Server Docker Setup Script for macOS
# This script sets up SQL Server in a Docker container for local development

echo "Setting up SQL Server in Docker..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker Desktop and try again."
    exit 1
fi

# Check if container already exists
if docker ps -a --format '{{.Names}}' | grep -q "^sqlserver$"; then
    echo "SQL Server container already exists."
    read -p "Do you want to remove the existing container and create a new one? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "Stopping and removing existing container..."
        docker stop sqlserver > /dev/null 2>&1
        docker rm sqlserver > /dev/null 2>&1
    else
        echo "Starting existing container..."
        docker start sqlserver
        echo "SQL Server is running. Connection string is already configured in appsettings.json"
        exit 0
    fi
fi

# Create and run SQL Server container
echo "Creating SQL Server container..."
docker run -e "ACCEPT_EULA=Y" \
    -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
    -p 1433:1433 \
    --name sqlserver \
    -d mcr.microsoft.com/mssql/server:2022-latest

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ SQL Server container created successfully!"
    echo ""
    echo "Container details:"
    echo "  - Name: sqlserver"
    echo "  - Port: 1433"
    echo "  - SA Password: YourStrong@Passw0rd"
    echo ""
    echo "The connection string in appsettings.json is already configured for this setup."
    echo ""
    echo "To start the container in the future: docker start sqlserver"
    echo "To stop the container: docker stop sqlserver"
    echo ""
    echo "Waiting for SQL Server to be ready..."
    sleep 10
    echo "You can now run: dotnet ef database update"
else
    echo "Error: Failed to create SQL Server container"
    exit 1
fi

