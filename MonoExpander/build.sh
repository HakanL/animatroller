#!/bin/sh

mono lib/nuget.exe restore
xbuild /p:TargetFrameworkVersion="v4.5"
