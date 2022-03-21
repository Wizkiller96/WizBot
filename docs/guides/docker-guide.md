# Setting up WizBot with Docker

# WORK IN PROGRESS

### Installation

1. Create a `/srv/wizbot` folder
- `mkdir -p /srv/wizbot`
2. Create a `docker-compose.yml`
- nano `docker-compose.yml`
- copy the following contents into it:
##### docker-compose.yml
```yml
version: "3.7"
services:
  wizbot:
    image: registry.gitlab.com/wiznet/wizbot:latest
    depends_on:
      - redis
    environment:
      TZ: Europe/Paris
      WizBot_RedisOptions: redis,name=wizbot
      #WizBot_ShardRunCommand: dotnet
      #WizBot_ShardRunArguments: /app/WizBot.dll {0} {1}
    volumes:
      - /srv/wizbot/conf/creds.yml:/app/creds.yml:ro
      - /srv/wizbot/data:/app/data

  redis:
    image: redis:4-alpine
    sysctls:
      - net.core.somaxconn=511
    command: redis-server --maxmemory 32M --maxmemory-policy volatile-lru
    volumes:
      - /srv/wizbot/redis-data:/data
```
3. Save your file and run docker compose
- `docker-compose up`
4. Edit creds in `/srv/wizbot/conf/creds.yml`
5. Run it again with
- `docker-compose up`

### Updating
- `cd /srv/wizbot`
- `docker-compose pull`
- `docker-compose up -d`
