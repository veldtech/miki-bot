FROM microsoft/dotnet:2.2-sdk

ENV BUILD_FOLDER dist

WORKDIR ./app

COPY . .

# necessary for the migration settings since for SOME reason
# there are 2 configs generated in separate folders, thanks Veld
COPY ./selfhost.json ./Miki/miki/settings.json

RUN dotnet publish ./Miki/Miki.csproj -c Release -v m -o $BUILD_FOLDER

RUN dotnet ef database update --project Miki/Miki.csproj

CMD dotnet ./Miki/$BUILD_FOLDER/Miki.dll
