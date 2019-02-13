FROM microsoft/dotnet:sdk as build

ENV BUILD_FOLDER dist

WORKDIR /app

COPY . ./app
COPY ./selfhost.json ./app/Miki/miki/settings.json

RUN dotnet publish ./app/Miki/Miki.csproj -c Release -v m -o $BUILD_FOLDER

RUN cat ./app/Miki/miki/settings.json

RUN cd ./app/Miki && dotnet ef database update

CMD dotnet ./app/Miki/$BUILD_FOLDER/Miki.dll
