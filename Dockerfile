# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file
COPY sp.rbac.api/SP.RBAC.API.csproj sp.rbac.api/

# Restore NuGet packages
RUN dotnet restore sp.rbac.api/SP.RBAC.API.csproj

# Copy the entire source code
COPY . .

# Build the application
WORKDIR /src/sp.rbac.api
RUN dotnet build SP.RBAC.API.csproj -c Release -o /app/build

# Publish the application
RUN dotnet publish SP.RBAC.API.csproj -c Release -o /app/publish

# Use the official .NET 9 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the port the app runs on
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Run the application
ENTRYPOINT ["dotnet", "SP.RBAC.API.dll"]
