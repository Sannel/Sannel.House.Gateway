#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see http://aka.ms/containercompat 

FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 443

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY src/Sannel.House.Gateway/Sannel.House.Gateway.csproj src/Sannel.House.Gateway/
RUN dotnet restore src/Sannel.House.Gateway/Sannel.House.Gateway.csproj
COPY . .
WORKDIR /src/src/Sannel.House.Gateway
RUN dotnet build Sannel.House.Gateway.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Sannel.House.Gateway.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Sannel.House.Gateway.dll"]
