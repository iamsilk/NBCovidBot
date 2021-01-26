#!/bin/bash

if [[ $EUID -ne 0 ]]; then
	echo "Run this install script as root"
	exit 1
fi

useradd -r -M -s /bin/false nbcovidbot

mkdir /var/local/NBCovidBot /usr/local/bin/NBCovidBot/

chown nbcovidbot:nbcovidbot /var/local/NBCovidBot

chmod 660 /var/local/NBCovidBot
chmod 775 /usr/local/bin/NBCovidBot

if [ -f "nbcovidbot.service" ]; then
	cp nbcovidbot.service /etc/systemd/system/nbcovidbot.service

	# Setups auto starting
	systemctl start nbcovidbot
else
	echo "Could not find service file - cannot install service."
fi

echo "NBCovidBot build files must be placed in /usr/local/bin/NBCovidBot"
