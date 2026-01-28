# build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Cashflowpoly.slnx ./
COPY src/Cashflowpoly.Api/Cashflowpoly.Api.csproj src/Cashflowpoly.Api/
RUN dotnet restore src/Cashflowpoly.Api/Cashflowpoly.Api.csproj

COPY . .
RUN dotnet publish src/Cashflowpoly.Api/Cashflowpoly.Api.csproj -c Release -o /app/publish

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cashflowpoly.Api.dll"]
