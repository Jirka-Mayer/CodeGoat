FROM mono:latest

RUN mkdir /app
COPY ./Server/bin/Debug /app

WORKDIR /app

EXPOSE 8080
EXPOSE 8181

ENTRYPOINT ["mono", "/app/Server.exe"]
CMD []
