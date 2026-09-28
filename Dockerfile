# Imagem da aplicação CLOUUD (usada pelo docker-compose.yml).
# Etapa 1: compila e publica com o SDK. Etapa 2: roda só com o runtime do ASP.NET, bem menor.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com o .csproj: enquanto os pacotes não mudam, o Docker reaproveita esta camada
COPY global.json ./
COPY src/Clouud.Web/Clouud.Web.csproj src/Clouud.Web/
RUN dotnet restore src/Clouud.Web/Clouud.Web.csproj

COPY src/ src/
RUN dotnet publish src/Clouud.Web/Clouud.Web.csproj --configuration Release --no-restore --output /app


FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

# Pastas que a aplicação grava (imagens enviadas e chaves dos cookies de login); viram volumes no compose.
# A aplicação roda com o usuário "app" da imagem, sem privilégios de root.
RUN mkdir -p wwwroot/uploads/perfis wwwroot/uploads/galeria wwwroot/uploads/capas /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app wwwroot/uploads /home/app/.aspnet
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Clouud.Web.dll"]
