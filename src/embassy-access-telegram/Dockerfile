FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY ./submodules /app/submodules
COPY ./src/embassy-access-core /app/src/embassy-access-core
COPY ./src/embassy-access-modules /app/src/embassy-access-modules

WORKDIR /app/src/embassy-access-telegram

RUN dotnet restore
RUN dotnet publish -c Release -r linux-x64 --self-contained true -o publish

FROM mcr.microsoft.com/dotnet/runtime:9.0

WORKDIR /app/src/embassy-access-telegram/publish

RUN sed -i 's/\[openssl_init\]/# [openssl_init]/' /etc/ssl/openssl.cnf
RUN printf "\n\n[openssl_init]\nssl_conf = ssl_sect" >> /etc/ssl/openssl.cnf
RUN printf "\n\n[ssl_sect]\nsystem_default = ssl_default_sect" >> /etc/ssl/openssl.cnf
RUN printf "\n\n[ssl_default_sect]\nMinProtocol = TLSv1\nCipherString = DEFAULT@SECLEVEL=0\n" >> /etc/ssl/openssl.cnf

COPY --from=build /app/src/embassy-access-telegram/publish .

ENTRYPOINT ["dotnet", "embassy-access-telegram.dll"]
