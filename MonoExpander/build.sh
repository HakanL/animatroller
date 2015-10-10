#!/bin/sh

mono lib/NuGet.exe restore
xbuild /p:TargetFrameworkVersion="v4.5"
