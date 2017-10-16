#!/bin/sh

/usr/bin/amixer sset 'PCM',0 100% 100%

mkdir -p /root/monoexpander
mkdir -p /root/ExpanderFiles

while [ 1 ]
do
	cd /root/animatroller
	git pull --ff-only
	cd /root/monoexpander
	unzip -o /root/animatroller/MonoExpander/Releases/Latest.zip
	
	/usr/bin/mono MonoExpander.exe -a -fs /root/ExpanderFiles/ -s hakan-el:8899 ##-sp0 /dev/ttyAMA0 -sb0 9600
done
