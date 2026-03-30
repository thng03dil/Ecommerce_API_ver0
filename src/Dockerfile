# BƯỚC 1: Sử dụng SDK để Build code
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy các file .csproj để Restore trước (tối ưu tốc độ build)
COPY ["src/Ecommerce.API/Ecommerce.API.csproj", "src/Ecommerce.API/"]
COPY ["src/Ecommerce.Application/Ecommerce.Application.csproj", "src/Ecommerce.Application/"]
COPY ["src/Ecommerce.Domain/Ecommerce.Domain.csproj", "src/Ecommerce.Domain/"]
COPY ["src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj", "src/Ecommerce.Infrastructure/"]
#COPY ["Ecommerce.UnitTests/Ecommerce.UnitTests.csproj", "Ecommerce.UnitTests/"]

RUN dotnet restore "src/Ecommerce.API/Ecommerce.API.csproj"

# Copy toàn bộ mã nguồn còn lại
COPY . .
WORKDIR "/src/src/Ecommerce.API"

# Build dự án ra thư mục /app/build
RUN dotnet build "Ecommerce.API.csproj" -c Release -o /app/build

# BƯỚC 2: Publish ứng dụng
FROM build AS publish
RUN dotnet publish "Ecommerce.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# BƯỚC 3: Chạy ứng dụng (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.API.dll"]