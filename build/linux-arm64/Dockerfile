ARG tag=2.2
FROM mcr.microsoft.com/dotnet/core/runtime:${tag} AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443
COPY  qemu-aarch64-static /usr/bin/
COPY Sannel.House.Gateway/ .
RUN useradd -m house -u 1200 && chown -R house /app && rm -f /usr/bin/qemu-aarch64-static
USER 1200
ENTRYPOINT ["dotnet", "Sannel.House.Gateway.dll"]