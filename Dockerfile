ARG version="1.0.0.0"
ARG dotNetVersion="8.0"

FROM mcr.microsoft.com/dotnet/sdk:${dotNetVersion} AS build
ARG version
WORKDIR /GitLabSymbolServerProxy
COPY ./GitLabSymbolServerProxy /GitLabSymbolServerProxy
RUN dotnet publish -c Release -r linux-musl-x64 --self-contained /p:Version=${version}

FROM alpine:latest
ARG dotNetVersion
WORKDIR /GitLabSymbolServerProxy
COPY --from=build /GitLabSymbolServerProxy/bin/Release/net${dotNetVersion}/linux-musl-x64/publish/* /GitLabSymbolServerProxy/appsettings.yml ./
COPY --from=build /GitLabSymbolServerProxy/certs/* ./certs/
RUN apk add gcompat libstdc++ icu-libs
ENTRYPOINT ["/GitLabSymbolServerProxy/GitLabSymbolServerProxy"]