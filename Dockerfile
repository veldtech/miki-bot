FROM microsoft/dotnet:2.2-sdk
ENV BUILD_FOLDER /dist
WORKDIR /app
COPY . .

RUN git submodule update --init

RUN dotnet publish ./Miki/Miki.csproj -c Release -v m -o $BUILD_FOLDER

RUN mkdir -p /app

RUN cp -r $BUILD_FOLDER/* /app

ENTRYPOINT ["dotnet", "/app/Miki.dll"]
