ARG version="1.0.0.0"

FROM mcr.microsoft.com/dotnet/sdk AS build
ARG version
WORKDIR /GitLabSymbolServerProxy
COPY ./GitLabSymbolServerProxy /GitLabSymbolServerProxy
RUN dotnet publish -c Release -r linux-musl-x64 --self-contained /p:Version=${version}

FROM alpine:latest
WORKDIR /GitLabSymbolServerProxy
COPY --from=build /GitLabSymbolServerProxy/bin/Release/net6.0/linux-musl-x64/publish/* ./
COPY --from=build /GitLabSymbolServerProxy/certs/* ./certs/
COPY --from=build /GitLabSymbolServerProxy/appsettings.yml ./
RUN apk add gcompat
RUN apk add libstdc++
RUN apk add icu-libs
ENTRYPOINT ["/GitLabSymbolServerProxy/GitLabSymbolServerProxy"]