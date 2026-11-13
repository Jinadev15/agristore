# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/AgriStore.Web/AgriStore.Web.csproj src/AgriStore.Web/
RUN dotnet restore src/AgriStore.Web/AgriStore.Web.csproj
COPY src/ src/
RUN dotnet publish src/AgriStore.Web/AgriStore.Web.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Render sends traffic to $PORT (default 10000) over its own HTTPS proxy.
# The image runs as a non-root user, so SQLite lives in /tmp (reset on each deploy; demo data is reseeded).
ENV ASPNETCORE_HTTP_PORTS=10000 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ConnectionStrings__DefaultConnection="DataSource=/tmp/app.db;Cache=Shared"
EXPOSE 10000
ENTRYPOINT ["dotnet", "AgriStore.Web.dll"]
