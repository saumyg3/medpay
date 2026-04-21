# Multi-stage build for smaller final image
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY MedPay.slnx ./
COPY MedPay.Core/MedPay.Core.csproj MedPay.Core/
COPY MedPay.Infrastructure/MedPay.Infrastructure.csproj MedPay.Infrastructure/
COPY MedPay.Api/MedPay.Api.csproj MedPay.Api/
COPY MedPay.Tests/MedPay.Tests.csproj MedPay.Tests/

RUN dotnet restore MedPay.Api/MedPay.Api.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish MedPay.Api/MedPay.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage - much smaller image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "MedPay.Api.dll"]
