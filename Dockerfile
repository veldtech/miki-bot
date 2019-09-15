FROM microsoft/dotnet:2.2-sdk
ENV BUILD_FOLDER dist
WORKDIR /app
COPY . .

RUN git submodule update --init

RUN dotnet publish ./Miki/Miki.csproj -c Release -v m -o $BUILD_FOLDER

# TODO: automatic migrations

ENTRYPOINT ["dotnet", "dist/Miki.dll"]
