# Setting up WizBot with Docker

# DO NOT USE YET - WORK IN PROGRESS

Upgrade from 2.x to v3 does not work because the file is mount readonly

### Docker Compose 
```yml
version: "3.7"
services:
  nadeko:
    image: registry.gitlab.com/wiznet/wizbot:v3
    depends_on:
      - redis
    environment:
      TZ: Europe/Paris
      #WizBot_RedisOptions: redis,name=wizbot
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
### Updating
- `cd /srv/wizbot`
- `docker-compose pull`
- `docker-compose up -d`
