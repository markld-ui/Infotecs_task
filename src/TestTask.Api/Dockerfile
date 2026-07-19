FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/TestTask.Api/TestTask.Api.csproj", "src/TestTask.Api/"]
COPY ["src/TestTask.Core/TestTask.Core.csproj", "src/TestTask.Core/"]
COPY ["src/TestTask.Infrastructure/TestTask.Infrastructure.csproj", "src/TestTask.Infrastructure/"]
RUN dotnet restore "src/TestTask.Api/TestTask.Api.csproj"

COPY . .
WORKDIR /src/src/TestTask.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TestTask.Api.dll"]
