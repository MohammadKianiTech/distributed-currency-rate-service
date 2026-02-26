FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["DistributedCurrencyRate.Api/DistributedCurrencyRate.Api.csproj", "DistributedCurrencyRate.Api/"]
RUN dotnet restore "DistributedCurrencyRate.Api/DistributedCurrencyRate.Api.csproj"
COPY . .
WORKDIR "/src/DistributedCurrencyRate.Api"
RUN dotnet build "DistributedCurrencyRate.Api.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "DistributedCurrencyRate.Api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DistributedCurrencyRate.Api.dll"]
