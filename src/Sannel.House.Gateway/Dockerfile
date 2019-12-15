#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see http://aka.ms/containercompat 
ARG TAG="3.1"
FROM mcr.microsoft.com/dotnet/core/aspnet:${TAG} AS base
WORKDIR /app
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:${TAG} AS build
WORKDIR /src
COPY [".nuget/nuget.config", ".nuget/nuget.config"]
COPY ["Directory.Build.props", "./"]
COPY src/Sannel.House.Gateway/Sannel.House.Gateway.csproj src/Sannel.House.Gateway/
RUN dotnet restore --configfile .nuget/nuget.config src/Sannel.House.Gateway/Sannel.House.Gateway.csproj
COPY . .
WORKDIR /src/src/Sannel.House.Gateway
RUN dotnet build Sannel.House.Gateway.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Sannel.House.Gateway.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ARG USER=root
USER ${USER}
ENTRYPOINT ["dotnet", "Sannel.House.Gateway.dll"]
