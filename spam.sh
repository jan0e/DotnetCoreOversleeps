#!/bin/bash
while true; do
    echo "spam"
    sleep $1
    >&2 echo "error"
done

