FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ImaginariumBackend.csproj", "./"]
RUN dotnet restore "ImaginariumBackend.csproj"
COPY . .
RUN dotnet build "ImaginariumBackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ImaginariumBackend.csproj" -c Release -o /app/publish

FROM base AS final
RUN apt-get update && apt-get install -y ffmpeg
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImaginariumBackend.dll"]
