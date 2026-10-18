# Багатоетапна збірка: в образі SDK компілюємо, а у фінальний (легкий, лише runtime) копіюємо результат.
# Збирати з кореня репозиторію: docker build -f src/SocialNetwork.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Спочатку лише .csproj і restore — цей шар кешується, поки не змінюються залежності.
COPY src/SocialNetwork.Domain/SocialNetwork.Domain.csproj src/SocialNetwork.Domain/
COPY src/SocialNetwork.Application/SocialNetwork.Application.csproj src/SocialNetwork.Application/
COPY src/SocialNetwork.Infrastructure/SocialNetwork.Infrastructure.csproj src/SocialNetwork.Infrastructure/
COPY src/SocialNetwork.Api/SocialNetwork.Api.csproj src/SocialNetwork.Api/
RUN dotnet restore src/SocialNetwork.Api/SocialNetwork.Api.csproj

COPY src/ src/
RUN dotnet publish src/SocialNetwork.Api/SocialNetwork.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Запуск не від root (користувач "app" уже є в офіційному образі).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SocialNetwork.Api.dll"]
