ARG tag=2.2
FROM mcr.microsoft.com/dotnet/core/runtime:${tag} AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443
COPY Sannel.House.Gateway/ .
RUN ln -s  /var/run/secrets/kubernetes.io/serviceaccount/ca.crt /etc/ssl/certs/kubernetes.pem \
    && useradd -m house -u 1200 \
    && chown -R house /app
USER 1200
ENTRYPOINT ["dotnet", "Sannel.House.Gateway.dll"]