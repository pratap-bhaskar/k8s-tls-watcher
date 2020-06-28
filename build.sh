docker build -t k8s-tls-watcher .
docker tag k8s-tls-watcher pratapgowda/k8s-tls-watcher:$1
docker push pratapgowda/k8s-tls-watcher:$1

kubectl apply -k deploy