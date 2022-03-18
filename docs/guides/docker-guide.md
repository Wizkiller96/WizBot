# Setting up NadekoBot with Docker

# WORK IN PROGRESS

### Installation

1. Create a `/srv/nadeko` folder 
  - `mkdir -p /srv/nadeko`
2. Create a `docker-compose.yml`
  - nano `docker-compose.yml`
  - copy the following contents into it:
##### docker-compose.yml 
```yml
version: "3.7"
services:
  nadeko:
    image: registry.gitlab.com/kwoth/nadekobot:latest
    depends_on:
      - redis
    environment:
      TZ: Europe/Paris
      NadekoBot_RedisOptions: redis,name=nadeko
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
3. Save your file and run docker compose
  - `docker-compose up`  
4. Edit creds in `/srv/nadeko/conf/creds.yml`
5. Run it again with
  - `docker-compose up`

### Updating
- `cd /srv/nadeko`
- `docker-compose pull`
- `docker-compose up -d`
