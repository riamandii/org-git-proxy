# org-git-proxy

## Overview

A basic Web proxy for GitHub API

## Building & Running

### Instal dotnet-runtime-2.1
```
  sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-runtime-2.1
```
  
### Start the server
Start the Http Web Proxy by executing the below command inside ./GitProxy folder

```
sudo dotnet run
```

### Configure the server

Configure the listening port and GitHub API Key in .GitProxy/Properties/launchSettings.Json

## Design

### Controllers

There are 3 main controllers, configured to accept requests from different exposed APIs: view/top APIs are handled by ViewController, healthcheck API by ProbeController, and all other APIs by GitProxyController.

Controllers sanitize the requests and call the necessary business components to execute the request. Controllers are asynchronous to not block the available threads on long I/O operations.

### Endpoint Proxy

EndpointProxy is the main business router/manager of requests. EndpointProxy is responsible for determining if the request is forwared to endpoint, retrieved from cache, or updated in cache. EndpointProxy also is responsible for refreshing the configured set of APIs in the background

### EndpointCacheManager

This component is responsible for storing and retrieving cached objects in memory. Aditionally this class can be decorated with File / persistent storage support, for faster bootstrap, and durability

### ViewManager

This components is responsible for applying the custom aggregation logic on cached data, and storing it appropriately in another in-memory cache

### Startup.cs

This file glues all the components together at server's startup time.

1. The concrete Netflix View Manager is configured to aggregate requests from "orgs/netflix/repos" API for "Netflix" organization. This component's read APIs will be used by the ViewController.
2. The generic Cache Manager is configured to cache all results on these sample APIs: "", "orgs/netflix", "orgs/netflix/members", "orgs/netflix/repos". This component is also configured to pass all cached objects to above View Manager for additional business logic
3. The concrete GitRequestHandler is configured to use the GIT API Token from Env Variables. This component acts as the request proxy between backend and GITHub. It handles the response pagination, etag comparison, throttling and retry
4. Endpoint Proxy glues the Cache Manager and Request Handler together, while adding request handling on top. This component is used by the GitProxyController
