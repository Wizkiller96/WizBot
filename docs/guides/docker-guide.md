# Setting up NadekoBot with Docker

# DO NOT USE YET - WORK IN PROGRESS

Upgrade from 2.x to v3 does not work because the file is mount readonly

### Docker Compose 
```yml
version: "3.7"
services:
  nadeko:
    image: registry.gitlab.com/veovis/nadekobot:v3-docker
    depends_on:
      - redis
    environment:
      TZ: Europe/Paris
      #NadekoBot_RedisOptions: redis,name=nadeko
      #NadekoBot_ShardRunCommand: dotnet
      #NadekoBot_ShardRunArguments: /app/NadekoBot.dll {0} {1}
    volumes:
      - /srv/nadeko/conf/creds.yml:/app/creds.yml:ro
      - /srv/nadeko/data:/app/data

  redis:
    image: redis:4-alpine
    sysctls:
      - net.core.somaxconn=511
    command: redis-server --maxmemory 32M --maxmemory-policy volatile-lru
    volumes:
      - /srv/nadeko/redis-data:/data
```
### Updating
- `cd /srv/nadeko`
- `docker-compose pull`
- `docker-compose up -d`
