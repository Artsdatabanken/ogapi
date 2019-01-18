FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY NinMemApi/NinMemApi.csproj NinMemApi/
COPY NinMemApi.Data/NinMemApi.Data.csproj NinMemApi.Data/
COPY NinMemApi.GraphDb/NinMemApi.GraphDb.csproj NinMemApi.GraphDb/
COPY Trees/Trees.csproj Trees/

RUN dotnet restore NinMemApi/NinMemApi.csproj
COPY . .
WORKDIR /src/NinMemApi
RUN dotnet build NinMemApi.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish NinMemApi.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "NinMemApi.dll"]

