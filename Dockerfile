FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["Altra.csproj", "./"]
RUN dotnet restore "Altra.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Altra.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Altra.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Altra.dll"]
