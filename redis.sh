#!/bin/bash

docker stop redis && docker rm redis;

docker run \
-p 6379:6379 \
--name redis \
-d redis redis-server --save 60 1 \
--loglevel warning
