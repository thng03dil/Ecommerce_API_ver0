# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Layer cache: restore
COPY ["src/Ecommerce.Domain/Ecommerce.Domain.csproj", "Ecommerce.Domain/"]
COPY ["src/Ecommerce.Application/Ecommerce.Application.csproj", "Ecommerce.Application/"]
COPY ["src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj", "Ecommerce.Infrastructure/"]
COPY ["src/Ecommerce.API/Ecommerce.API.csproj", "Ecommerce.API/"]
RUN dotnet restore "Ecommerce.API/Ecommerce.API.csproj"

COPY src/ ./
RUN dotnet publish "Ecommerce.API/Ecommerce.API.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.API.dll"]