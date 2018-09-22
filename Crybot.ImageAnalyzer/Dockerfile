FROM node:8-slim
LABEL maintainer="Suz Hinton <noopkat@gmail.com>"

# install chrome rather than relying on Puppeteer 
RUN apt-get update && apt-get install -y wget --no-install-recommends \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list' \
    && apt-get update \
    && apt-get install -y google-chrome-unstable --no-install-recommends \
    && rm -rf /var/lib/apt/lists/* \
    && apt-get purge --auto-remove -y curl \
    && rm -rf /src/*.deb


# copy project files and install dependencies
RUN mkdir -p /var/app
COPY . /var/app 
WORKDIR /var/app
RUN npm install
RUN npm i puppeteer
# grant permissions for Puppeteer user
RUN groupadd -r pptruser && useradd -r -g pptruser -G audio,video pptruser \
    && mkdir -p /home/pptruser/Downloads \
    && chown -R pptruser:pptruser /home/pptruser \
    && chown -R pptruser:pptruser /var/app
USER pptruser
EXPOSE 3000/tcp
ENV PORT 3000

ENTRYPOINT ["npm", "start"]