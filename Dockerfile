FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY "NBCovidBot/*.csproj" "NBCovidBot/"
RUN dotnet restore NBCovidBot

COPY . .
RUN dotnet publish NBCovidBot -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /data
COPY --from=build /app/publish /app
ENTRYPOINT ["dotnet", "/app/NBCovidBot.dll"]