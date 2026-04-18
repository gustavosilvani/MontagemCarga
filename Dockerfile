FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/MontagemCarga.Api/MontagemCarga.Api.csproj src/MontagemCarga.Api/
COPY src/MontagemCarga.Application/MontagemCarga.Application.csproj src/MontagemCarga.Application/
COPY src/MontagemCarga.Domain/MontagemCarga.Domain.csproj src/MontagemCarga.Domain/
COPY src/MontagemCarga.Infrastructure/MontagemCarga.Infrastructure.csproj src/MontagemCarga.Infrastructure/

RUN dotnet restore src/MontagemCarga.Api/MontagemCarga.Api.csproj

COPY . .
RUN dotnet publish src/MontagemCarga.Api/MontagemCarga.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MontagemCarga.Api.dll"]
