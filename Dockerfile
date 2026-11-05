FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN find . -name "*.csproj"
RUN dotnet restore ./ConsoleApp.csproj
RUN dotnet publish ./ConsoleApp.csproj -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "ConsoleApp.dll"]