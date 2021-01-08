[https://fabiolb.net](https://fabiolb.net)

[https://www.nginx.com/resources/glossary/reverse-proxy-vs-load-balancer/](https://www.nginx.com/resources/glossary/reverse-proxy-vs-load-balancer/)

[Load Balancer vs Reverse Proxy (Explained by Example)](https://www.youtube.com/watch?v=S8J2fkN2FeI)

[What is a Reverse Proxy vs. Load Balancer?](https://www.nginx.com/resources/glossary/reverse-proxy-vs-load-balancer/)

[Proxy vs Reverse Proxy Server Explained](https://www.youtube.com/watch?v=SqqrOspasag)

[Load Balancing Strategies for Consul](https://www.hashicorp.com/blog/load-balancing-strategies-for-consul)

[Load Balancing with NGINX and Consul Template](https://learn.hashicorp.com/tutorials/consul/load-balancing-nginx)

[Load Balancing with HAProxy Service Discovery Integration](https://learn.hashicorp.com/tutorials/consul/load-balancing-haproxy?in=consul/integrations)

[Load Balance Registered Services](https://learn.hashicorp.com/collections/consul/load-balancing)

[Load Balancing Strategies with NGINX/HAProxy and Consul](https://www.youtube.com/watch?v=ZvKPAug-IgA)

[Dynamic Load Balancing with Consul and Nginx Plus](https://www.youtube.com/watch?v=68qJuV2iRtk)

[Fabio - A Stateless Load Balancer](https://www.youtube.com/watch?v=fJ3qHbgZsU0&t=1214s)

[Load balancing with Consul & Nginx](https://hectormartinez.dev/posts/load-balancing-consul-nginx/)

[Introducing Consul Template](https://www.hashicorp.com/blog/introducing-consul-template)

[Nomad Auto-Proxy with Consul-Template and NGINX](https://www.youtube.com/watch?v=75vF92Vue2U)


[Fabio](https://fabiolb.net) is an HTTP and TCP reverse proxy that configures itself with data from Consul. Fabio works differently with traditional load balancers that need to be configured with a config file and This process can be automated with tools like consul-template that generate config files and trigger a reload, since fabio updates its routing table directly from the data stored in Consul as soon as there is a change and without restart or reloading.

When you register a service in Consul all you need to add is a tag that announces the paths the upstream service accepts, e.g. urlprefix-/user or urlprefix-/order and fabio will do the rest.

Fabio will work with consul service registry and then it scrap and build its own dynamic routing table once we register a service in consul the fabio use consul to build its internal routing table then we will be able to send a request to fabio as our load balancer that it live on a static ip address or static dns and fabio take care of properly redirecting our request to selected service instance. registration data for our service in consul service registry can have the `tags`. we can define `tags` for particular registration and fabio uses this tag that called `urlprefix` and based on this create dynamic routing table. we also can see this routing table in fabio browser. in fabio we have some option like `a/b testing` or `canary release` like 5 percnt our request go to `version1` of our service and 95 percent of our request go to `version2` of our service. routing table is available in `http://localhost:9998`

for enabling fabio in customer service we should enable it in our `appsettings.json` 

``` json
"fabio": {
"enabled": true,
"url": "http://localhost:9999",
"service": "customers-service"
},
```

and `appsettings.local.json`

``` json
"fabio": {
"enabled": true
},
```

the `url` is address of our reverse proxy and load balancer and `service` that is same as our consul service. after run customer service in consul ui we should see tags for our `customer-service` with this tag `urlprefix-/customers-service strip=/customers-service` that is a `strip route` and this tag is special `url prefix` that will use by fabio to scrap services and build routing table (http://localhost:9998). the [strip](https://github.com/fabiolb/fabio/wiki/Features#path-stripping) will use for stripping a path from the incoming request.

SERVICE_TAGS: the value here is very important for Fabio. Fabio monitors the tags used in the Consul services, and when it detects the urlprefix keyword, it will automatically add a route for that service. The value urlprefix-/ tells Fabio that our trigger route is for/.

[https://fabiolb.net/quickstart/](https://fabiolb.net/quickstart/)

[https://medium.com/@wisegain/consul-registrator-fabio-integration-c068280710b9](https://medium.com/@wisegain/consul-registrator-fabio-integration-c068280710b9)

now if we make a request to `http://localhost:9999/customers-service` it will use `customers-service` source path in our routing table that created before based on our tag and after match and find a matched route for this `path source` in routing table and after finding a row for our path apply strip on our path then send it to upstream service. for example `http://localhost:9999/customers-service/customers`, after built our routing table by service tags, requested path to fabio `http://localhost:9999` with path `customers-service/customers` will search in our routing table and after find a `source` for that strip for this row will apply on the path, here strip is `customers-service` and it will remove from path and final path here `\` will send to upstream service (customers-service). so with request to this url we hit microservices instance and we don't care which instance it is. it just been take care of by fabio load balancer.

in routing table if we have some instance of a service it will show them in separate row with its original `dest` address and its `weight` for participating in load balancing algorithm, fro example if we hav 4 instance each instance will be `25` percent.

now lets plug this for communication between availability service and customer service, from availability perspective we don't need register ourself to fabio and just hit load balancer endpoint but if we want to add all services to load balancer we can do following steps:

first we should setting some setting in our `appsettings.json`

``` json
"fabio": {
    "enabled": true,
    "url": "http://localhost:9999",
    "service": "availability-service"
},
```
and we can override this in `appsettings.local.json`

second we should add `AddFabio` in our Extensions of our infrastructure layer and it does some setting like we did in consul like a custom `FabioHttpClient` and `FabioMessageHandler` for override request url to corresponding fabio endpoint (`http://localhost:9999`. then we should change HttpClient type to `fabio` in our availability `appsettings.local.json` and run our service. we wi

``` json
"httpClient": {
    "type": "fabio",
    "retries": 3,
    "services": {
        "customers": "customers-service"
    }
},
```

we will see different implementation of `HttpClient` being injected into our `ICustomerServiceClient` in our `AvailabilityService` with this type of HttpClient (fabio).
now after executing this line

``` csharp
var customerState = await _customersServiceClient.GetStateAsync(command.CustomerId);
```

in log we can see this request `http://customers-service/customers/d3d6c421-8b3c-4b6d-9ee9-1ac56d683301/state` that override by `FabioMessageHandler` for `FabioHttpClient` to this fabio endpoint `http://localhost:9999/customers-service/customers/d3d6c421-8b3c-4b6d-9ee9-1ac56d683301/state` for load balancing.

beside of consul and fabio we can use another `service registry` and `load balancer` tools. one of popular one is [traefik](https://traefik.io) that is load balancer and we can integrate it with consul as a backend registry with this [guid](https://doc.traefik.io/traefik/v1.4/configuration/backends/consul/).

[https://learn.hashicorp.com/tutorials/nomad/load-balancing-traefik](https://learn.hashicorp.com/tutorials/nomad/load-balancing-traefik)

[https://www.youtube.com/watch?v=4PzQSyL0Zw8&feature=emb_title](https://www.youtube.com/watch?v=4PzQSyL0Zw8&feature=emb_title)

we don't event need to use service registry and load balancer we can use docker network or kubernetes or some orchestration mechanism (last module we see that).

we can use of docker network and dns to do similar behavior

[https://medium.com/@lherrera/poor-mans-load-balancing-with-docker-2be014983e5](https://medium.com/@lherrera/poor-mans-load-balancing-with-docker-2be014983e5)

[https://stefanjarina.gitbooks.io/docker/content/examples/assignments/dns-round-robin-test.html](https://stefanjarina.gitbooks.io/docker/content/examples/assignments/dns-round-robin-test.html)

[vegibit.com/dns-round-robin-in-docker](vegibit.com/dns-round-robin-in-dockerg)


we use point-to-point communication between services we this communication are temporal coupling because they execute synchronously. when we make a call on different component or service and our service not go further processing this particular request because only it need to wait for this response coming from particular service so if service failed for a reason then we either need to retry using polly or maybe over process fail as well. but by doing so we can get `up to date` and `fresh` data and we get immediate consistency but we sacrifice system availability for data consistency by doing so.


[https://www.capitalone.com/tech/software-engineering/how-to-avoid-loose-coupled-microservices/](https://www.capitalone.com/tech/software-engineering/how-to-avoid-loose-coupled-microservices/)

Temporal coupling happens when a service - the caller - expects an instantaneous response from another - the callee - before it can resume processing. Since any delay in the response time of the callee would adversely affect the response time of the caller, the callee has to be always up and responsive. This situation usually happens when services use synchronous communication.

the same would apply if we were to use `grpc` or `websocket` and see the same potential challenges with load balancing, routing and so on and this is general challenges for commination.