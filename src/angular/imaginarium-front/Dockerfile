FROM node:22.12.0-alpine AS builder

WORKDIR /app

COPY package.json ./

RUN npm install

COPY . .

RUN npm run build

FROM nginx:1.25-alpine

RUN rm -rf /usr/share/nginx/html/*
COPY --from=builder /app/dist/imaginarium-front/browser/ /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf