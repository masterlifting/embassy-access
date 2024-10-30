FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY ./submodules /app/submodules
COPY ./src/embassy-access-core /app/src/embassy-access-core
COPY ./src/embassy-access-telegram /app/src/embassy-access-telegram
COPY ./src/embassy-access-telegram-api /app/src/embassy-access-telegram-api

WORKDIR /app/src/embassy-access-telegram-api

RUN dotnet restore
RUN dotnet publish -c Release -o publish

FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app/src/embassy-access-telegram-api/publish

COPY --from=build-env /app/src/embassy-access-telegram-api/publish .

RUN touch /app/chats && chmod 777 /app/chats

ENTRYPOINT ["dotnet", "embassy-access-telegram-api.dll"]
