# syntax=docker/dockerfile:1

# ---------- Build stage: compiles the MVC web app ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first, so the (slow) package restore is cached
# and only repeats when the dependencies change
COPY RaceDay.Web/RaceDay.Web.csproj RaceDay.Web/
RUN dotnet restore RaceDay.Web/RaceDay.Web.csproj

# Now copy the source and publish a Release build
COPY RaceDay.Web/ RaceDay.Web/
WORKDIR /src/RaceDay.Web
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime stage: small image that only runs the published web app ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root "app" user that ships with the .NET 8 images
USER app

# .NET 8 images listen on port 8080 by default
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]