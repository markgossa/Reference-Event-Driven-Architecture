#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/LSE.Stocks.Api/LSE.Stocks.Api.csproj", "src/LSE.Stocks.Api/"]
RUN dotnet restore "src/LSE.Stocks.Api/LSE.Stocks.Api.csproj"
COPY . .
WORKDIR "/src/src/LSE.Stocks.Api"
RUN dotnet build "LSE.Stocks.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LSE.Stocks.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LSE.Stocks.Api.dll"]