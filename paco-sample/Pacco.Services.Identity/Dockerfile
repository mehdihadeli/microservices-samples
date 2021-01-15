FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
RUN curl https://api.nuget.org/v3/index.json -k
WORKDIR /app
COPY . .
RUN dotnet publish src/Pacco.Services.Identity.Api -c release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS http://*:80
ENV ASPNETCORE_ENVIRONMENT docker
ENTRYPOINT dotnet Pacco.Services.Identity.Api.dll