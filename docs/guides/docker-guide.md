# Setting up WizBot with Docker

# DO NOT USE YET - WORK IN PROGRESS

### Docker Compose 
```yml
version: "3.7"
services:
  wizbot:
    image: registry.gitlab.com/veovis/wizbot:v3-docker
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
