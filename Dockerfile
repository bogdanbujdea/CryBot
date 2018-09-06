FROM microsoft/dotnet:2.1-sdk AS build-env
COPY . /app

ENV BuildVersion 1.0.0.0
ENV ASPNETCORE_ENVIRONMENT=Development
RUN curl -sL https://deb.nodesource.com/setup_6.x |  bash -
RUN apt-get install -y nodejs

WORKDIR /app/CryBot.Core
RUN ["dotnet", "build", "-c", "Release"]

WORKDIR /app/CryBot.Web
RUN ["dotnet", "publish", "-c", "Release"]

EXPOSE 80/tcp

ENTRYPOINT ["dotnet", "run"]