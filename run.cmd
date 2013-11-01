@echo off

pushd %~dp0src\Gateway\NuGet.Services.Gateway
%~dp0packages\OwinHost.2.0.0\tools\OwinHost.exe -u http://nugetapi.localtest.me
popd