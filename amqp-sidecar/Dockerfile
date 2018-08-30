FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["amqp-sidecar/amqp-sidecar.csproj", "amqp-sidecar/"]
RUN dotnet restore "amqp-sidecar/amqp-sidecar.csproj"
COPY . .
WORKDIR "/src/amqp-sidecar"
RUN dotnet build "amqp-sidecar.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "amqp-sidecar.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "amqp-sidecar.dll"]