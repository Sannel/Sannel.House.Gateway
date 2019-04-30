ARG tag=2.2
FROM mcr.microsoft.com/dotnet/core/runtime:${tag} AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443
COPY qemu-arm-static /usr/bin/
COPY Sannel.House.Users/ .
RUN useradd -m house -u 1100 && chown -R house /app && rm -f /usr/bin/qemu-arm-static
USER house
ENTRYPOINT ["dotnet", "Sannel.House.Gateway.dll"]