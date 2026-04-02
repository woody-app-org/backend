# Build (SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Woody.sln ./
COPY src/Woody.Api/Woody.Api.csproj src/Woody.Api/
COPY src/Woody.Application/Woody.Application.csproj src/Woody.Application/
COPY src/Woody.Domain/Woody.Domain.csproj src/Woody.Domain/
COPY src/Woody.Infrastructure/Woody.Infrastructure.csproj src/Woody.Infrastructure/

RUN dotnet restore src/Woody.Api/Woody.Api.csproj

COPY src/ src/

RUN dotnet publish src/Woody.Api/Woody.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0
ENV PORT=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Woody.Api.dll"]
