#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
ENV LANG C.UTF-8
ENV TZ=Asia/Shanghai
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
WORKDIR /app
EXPOSE 80
#EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DeploymentRobotService.csproj", ""]
RUN dotnet restore "./DeploymentRobotService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DeploymentRobotService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeploymentRobotService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY Properties ./Properties
ENTRYPOINT ["dotnet", "DeploymentRobotService.dll"]