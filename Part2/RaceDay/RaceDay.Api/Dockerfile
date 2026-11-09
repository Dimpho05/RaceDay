# syntax=docker/dockerfile:1

# ---------- Build stage: compiles the API ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first, so the (slow) package restore is cached
# and only repeats when the dependencies change
COPY RaceDay.Api/RaceDay.Api.csproj RaceDay.Api/
RUN dotnet restore RaceDay.Api/RaceDay.Api.csproj

# Now copy the source and publish a Release build
COPY RaceDay.Api/ RaceDay.Api/
WORKDIR /src/RaceDay.Api
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime stage: small image that only runs the published API ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root "app" user that ships with the .NET 8 images
USER app

# .NET 8 images listen on port 8080 by default
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]