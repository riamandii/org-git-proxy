# org-git-proxy

## Overview

A basic Web proxy for GitHub API

## Building

### Instal dotnet-runtime-2.1
sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-runtime-2.1
  
### Start the server
sudo dotnet runtime-2

### Configure the server

Configure the listening port and GitHub API Key in launchSettings.Json