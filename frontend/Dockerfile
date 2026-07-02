# syntax=docker/dockerfile:1

FROM node:24-slim AS build
WORKDIR /app
RUN corepack enable
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
RUN pnpm install --frozen-lockfile
COPY . .
RUN pnpm build

FROM node:24-slim AS runtime
WORKDIR /app
RUN corepack enable
ENV NODE_ENV=production
ENV PORT=3000
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
RUN pnpm install --frozen-lockfile --prod
COPY --from=build /app/build ./build
EXPOSE 3000
# PUBLIC_API_BASE_URL / API_BASE_URL_INTERNAL are read at request time via
# $env/dynamic/* — set them as container env vars for the target deployment.
CMD ["node", "build/index.js"]
