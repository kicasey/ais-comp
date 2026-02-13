FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY API_For_Server.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "API_For_Server.dll"]
ENV ASPNETCORE_URLS=http://+:8080
