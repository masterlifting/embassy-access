FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ./submodules /app/submodules
COPY ./src/embassy-access-core /app/src/embassy-access-core
COPY ./src/embassy-access-modules /app/src/embassy-access-modules
COPY ./src/embassy-access-worker /app/src/embassy-access-worker

WORKDIR /app/src/embassy-access-worker

RUN dotnet restore
RUN dotnet publish -c Release -r linux-x64 --self-contained true -o publish

FROM mcr.microsoft.com/dotnet/runtime:10.0

WORKDIR /app/src/embassy-access-worker/publish

RUN sed -i 's/\[openssl_init\]/# [openssl_init]/' /etc/ssl/openssl.cnf
RUN printf "\n\n[openssl_init]\nssl_conf = ssl_sect" >> /etc/ssl/openssl.cnf
RUN printf "\n\n[ssl_sect]\nsystem_default = ssl_default_sect" >> /etc/ssl/openssl.cnf
RUN printf "\n\n[ssl_default_sect]\nMinProtocol = TLSv1\nCipherString = DEFAULT@SECLEVEL=0\n" >> /etc/ssl/openssl.cnf

COPY --from=build /app/src/embassy-access-worker/publish .

ENTRYPOINT ["dotnet", "embassy-access-worker.dll"]
