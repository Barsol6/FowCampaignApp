FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN dotnet workload install wasm-tools

COPY ["FowCampaign.Api/FowCampaign.Api.csproj", "FowCampaign.Api/"]
COPY ["FowCampaign.App/FowCampaign.App.csproj", "FowCampaign.App/"]

RUN dotnet restore "FowCampaign.Api/FowCampaign.Api.csproj"

COPY . .
WORKDIR "/src/FowCampaign.Api"

RUN dotnet build "FowCampaign.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FowCampaign.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FowCampaign.Api.dll"]