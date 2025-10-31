ARG DOTNET_VERSION=9.0
ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS BUILD

WORKDIR /src

COPY *.csproj ./

RUN dotnet restore MessagingApp.csproj

RUN rm -rf bin obj

COPY . .


RUN dotnet publish MessagingApp.csproj -c Release -o /app/publish /p:Exclude="MessagingAppTests"




FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

WORKDIR /app

COPY --from=BUILD /app/publish .

EXPOSE 4200

ENTRYPOINT ["dotnet", "MessagingApp.dll"]