@echo off
pushd
cd bin\Debug
MonoExpander.exe -a -fs "C:\Temp\MonoExpanderFiles" -s 127.0.0.1:8086
popd
