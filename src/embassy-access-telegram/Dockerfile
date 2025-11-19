FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution file first for better layer caching
COPY embassy-access.slnx .

# Copy project files for restore
COPY ./src/embassy-access-core/*.fsproj ./src/embassy-access-core/
COPY ./src/embassy-access-modules/embassy-access-italian/*.fsproj ./src/embassy-access-modules/embassy-access-italian/
COPY ./src/embassy-access-modules/embassy-access-russian/*.fsproj ./src/embassy-access-modules/embassy-access-russian/
COPY ./src/embassy-access-telegram/*.fsproj ./src/embassy-access-telegram/

# Copy submodule project files
COPY ./submodules ./submodules

# Restore dependencies
WORKDIR /app/src/embassy-access-telegram
RUN dotnet restore

# Copy source code
WORKDIR /app
COPY ./src/embassy-access-core ./src/embassy-access-core
COPY ./src/embassy-access-modules ./src/embassy-access-modules
COPY ./src/embassy-access-telegram ./src/embassy-access-telegram

# Build and publish
WORKDIR /app/src/embassy-access-telegram
RUN dotnet publish -c Release -r linux-x64 --self-contained true --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Configure OpenSSL for legacy protocols
RUN sed -i 's/\[openssl_init\]/# [openssl_init]/' /etc/ssl/openssl.cnf && \
    printf "\n\n[openssl_init]\nssl_conf = ssl_sect" >> /etc/ssl/openssl.cnf && \
    printf "\n\n[ssl_sect]\nsystem_default = ssl_default_sect" >> /etc/ssl/openssl.cnf && \
    printf "\n\n[ssl_default_sect]\nMinProtocol = TLSv1\nCipherString = DEFAULT@SECLEVEL=0\n" >> /etc/ssl/openssl.cnf

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser && \
    chown -R appuser:appuser /app

USER appuser

COPY --from=build --chown=appuser:appuser /app/publish .

ENTRYPOINT ["dotnet", "embassy-access-telegram.dll"]
