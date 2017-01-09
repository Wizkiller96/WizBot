FROM microsoft/dotnet:sdk
MAINTAINER Poag <poag@gany.net>

WORKDIR /opt/

#Install required software
RUN echo "deb http://www.deb-multimedia.org jessie main non-free" | tee /etc/apt/sources.list.d/debian-backports.list \
	&& apt-get update \
	&& apt-get install -y --force-yes deb-multimedia-keyring \
	&& apt-get update \
	&& apt-get install -y git libopus0 opus-tools libopus-dev libsodium-dev ffmpeg

#Download and install stable version of WizBot
RUN curl -L https://github.com/Wizkiller96/WizBot-BashScript/raw/master/wizbot_installer_latest.sh | sh \
	&& curl -L https://github.com/Wizkiller96/WizBot-BashScript/raw/master/wizbot_autorestart.sh > nadeko.sh \
	&& chmod 755 wizbot.sh

VOLUME ["/opt"]

CMD ["/opt/wizbot.sh"]
