# PubSubApiSvc

## 🚀 Installation

1. Install Docker Desktop (Windows and Mac), or Docker and Docker Compose on your Linux machine

2. Clone this repository

```sh
git clone git@github.com:ecortezr/PubSubApiSvc.git
```

3. Access to the projecy folder

```sh
cd PubSubApiSvc
```

4. Up the services that the application requires

```sh
docker-compose up
```

The first time it'll take a few minutes, because it'll download the Docker's images for first time. Also, you can use `-d` at the end, so that it frees the console and you can continue executing commands.

5. Open Visual Studio and access the solution that you will find in the project folder.

6. In Visual Studio, run the application (first verify that the required services are healthy and available to be consumed)

7. Access the different endpoints that you can consume through Swagger

## Tests

1. Make sure all services are stopped

```sh
docker-compose down
```

2. From Visual Studio run the set of tests of your choice or run them all.

## Docker

1. Build a docker image

```sh
docker build -t docker-pub-subapi-svc .
````

2. Run it and create a docker container, to test the api

```sh
docker run -p 5555:80 docker-pub-subapi-svc:latest
````

3. Access to [http://localhost:5555/Api/Permission](http://localhost:5555/Api/Permission) in your browser and check that it's working.

## K8s

1. Create a cluster in your local environment (`minikube`, for example)

2. Apply the deployment to K8s

```sh
minikube kubectl -- apply -f k8s/deployment.yaml
````

3. Apply a service on K8s

```sh
minikube kubectl -- apply -f k8s/service.yaml
````

4. Get access information to your service

```sh
minikube service pub-subapi-svc-service
````

5. Using the local address (127.0.0.1), you should be able to access the created service. For example: `http://127.0.0.1:56790`
