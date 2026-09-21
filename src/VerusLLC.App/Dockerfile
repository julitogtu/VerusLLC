FROM node:24.12-alpine AS build
WORKDIR /source

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /source/dist/VerusLLC.App/browser /usr/share/nginx/html

EXPOSE 8080
