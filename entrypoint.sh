#!/bin/bash
set -e

# Wait for Postgres to be ready 
until pg_isready -h "$DB_HOST" -p "$DB_PORT"; do
  echo "Waiting for Postgres at $DB_HOST:$DB_PORT..."
  sleep 2
done

# Apply EF migrations
echo "Applying database migrations..."
dotnet tool install --global dotnet-ef --version 8.0.0 || true
export PATH="$PATH:/root/.dotnet/tools"
dotnet ef database update --project AuthService.csproj

# Start the app
exec dotnet AuthService.dll
