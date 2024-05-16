# Deploying NadekoBot with Docker: A Comprehensive Guide

## Getting Started

Ensure Docker and Docker Compose are installed on your system. If not, follow the official Docker guides for your specific operating system:

- [Docker Installation Guide](https://docs.docker.com/engine/install/)
- [Docker Compose Installation Guide](https://docs.docker.com/compose/install/)

## Step-by-Step Installation

1. **Choose Your Workspace:** Select a directory where you'll set up your NadekoBot stack. Use your terminal to navigate to this directory. For the purpose of this guide, we'll use `/opt/stacks/nadekobot/` as an example, but you can choose any directory that suits your needs.

2. **Create a Docker Compose File:** In this directory, create a Docker Compose file named `docker-compose.yml`. You can use any text editor for this task. For instance, to use the `nano` editor, type `nano docker-compose.yml`.

3. **Configure Your Docker Compose File:** Populate your Docker Compose file with the following configuration:

```yml
services:
  nadeko:
    image: registry.gitlab.com/kwoth/nadekobot:latest
    container_name: nadeko
    restart: unless-stopped
    environment:
      TZ: Europe/Rome
    volumes:
      - /opt/stacks/nadekobot/conf/creds.yml:/app/data/creds.yml
      - /opt/stacks/nadekobot/data:/app/data
networks: {}
```

4. **Prepare Your Credentials File:** Before running Docker Compose, ensure the `creds.yml` file exists in the `/opt/stacks/nadekobot/conf/` directory. If it's missing, create it using `touch /opt/stacks/nadekobot/conf/creds.yml`. You may need to use `sudo`. Remember to replace `/opt/stacks/nadekobot/` with your chosen directory.

5. **Edit Your Credentials File:** Populate the `creds.yml` file in `/opt/stacks/nadekobot/conf/creds.yml` with your bot's credentials. You can use any text editor for this task. For instance, to use the `nano` editor, type `nano /opt/stacks/nadekobot/conf/creds.yml`. You may need to use `sudo`. Again, replace `/opt/stacks/nadekobot/` with your chosen directory.

6. **Launch Your Bot:** Now, you're ready to run Docker Compose. Use the following command: `docker-compose up -d`.

## Keeping Your Bot Up-to-Date

There are two methods to update your NadekoBot:

### Manual Update

1. **Navigate to Your Directory:** Use `cd /path/to/your/directory` to go to the directory containing your Docker Compose file.

2. **Pull the Latest Images:** Use `docker-compose pull` to fetch the latest images.

3. **Restart Your Containers:** Use `docker-compose up -d` to restart the containers.

### Automatic Update with Watchtower

If you prefer an automated update process, consider using Watchtower. Watchtower automatically updates your Docker containers to the latest versions. 

To use Watchtower with NadekoBot, you need to add a specific label to the service in your Docker Compose file. Here's how your Docker Compose file should look:

```yml
services:
  nadeko:
    image: registry.gitlab.com/kwoth/nadekobot:latest
    container_name: nadeko
    restart: unless-stopped
    labels:
      - com.centurylinklabs.watchtower.enable=true
    environment:
      TZ: Europe/Rome
    volumes:
      - /opt/stacks/nadekobot/conf/creds.yml:/app/data/creds.yml
      - /opt/stacks/nadekobot/data:/app/data
networks: {}
```

Remember to replace `/opt/stacks/nadekobot/` with your chosen directory in the Docker Compose file.

To install and run Watchtower, follow the guide provided by Containrrr:

- [Watchtower Installation and Usage Guide](https://containrrr.dev/watchtower/)