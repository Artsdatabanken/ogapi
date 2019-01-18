#!/bin/bash
echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin
docker push artsdatabanken/ninmemapi
docker push artsdatabanken/ninmemapi_datapreprocessing