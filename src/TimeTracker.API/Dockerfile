# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Central Package Management manifest must be present before restore, or NuGet
# silently falls back to unpinned floor versions of every package.
COPY Directory.Packages.props ./

# Copy csproj files first to leverage Docker layer caching for restore
COPY ["src/TimeTracker.Domain/TimeTracker.Domain.csproj", "src/TimeTracker.Domain/"]
COPY ["src/TimeTracker.Application/TimeTracker.Application.csproj", "src/TimeTracker.Application/"]
COPY ["src/TimeTracker.Infrastructure/TimeTracker.Infrastructure.csproj", "src/TimeTracker.Infrastructure/"]
COPY ["src/TimeTracker.API/TimeTracker.API.csproj", "src/TimeTracker.API/"]
RUN dotnet restore "src/TimeTracker.API/TimeTracker.API.csproj"

# Copy the rest of the source and publish
COPY src/ src/
RUN dotnet publish "src/TimeTracker.API/TimeTracker.API.csproj" -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as a non-root user for defence in depth
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TimeTracker.API.dll"]
