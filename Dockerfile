# Etapa 1: Compilación y Publicación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar proyectos y restaurar dependencias
COPY ["Firmeza.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/Firmeza.Domain/Firmeza.Domain.csproj", "src/Firmeza.Domain/"]
COPY ["src/Firmeza.Application/Firmeza.Application.csproj", "src/Firmeza.Application/"]
COPY ["src/Firmeza.Infrastructure/Firmeza.Infrastructure.csproj", "src/Firmeza.Infrastructure/"]
COPY ["src/Firmeza.Api/Firmeza.Api.csproj", "src/Firmeza.Api/"]
COPY ["tests/Firmeza.Tests/Firmeza.Tests.csproj", "tests/Firmeza.Tests/"]

RUN dotnet restore "src/Firmeza.Api/Firmeza.Api.csproj"

# Copiar el código fuente completo y compilar en Release
COPY . .
WORKDIR "/src/src/Firmeza.Api"
RUN dotnet publish "Firmeza.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen de Producción (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Crear directorio para almacenamiento de recibos PDF
RUN mkdir -p /app/Storage/recibos

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Firmeza.Api.dll"]
