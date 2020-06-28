# Kubernetes TLS Watcher

An small utility used to check for the expiry of the tls certificates in the kubernetes cluster. Certificates are added in to the cluster as a secret which could be later mounted using ingresses (either through istio ingressgateway or nginx).

The container checks for all the kubernetes secrets of type ```kubernetes.io/tls```, reads the public key for those secrets and finds out the time its to expire. This information is then presented as metrics for prometheus to scrape at 3031 port. 

Metric : **tls_certs_expiry_in_days** with labels *domain, secretname, namespace*

## Deploying on your cluster

[Deploy](./Deploy) folder contains the neccessary components to deploy the tls watcher to your cluster. I have used the pod monitor and prometheus rules.

As a prerequisite you would need [Prometheus-Operator](https://github.com/coreos/prometheus-operator) in your cluster.

```bash
kubectl apply -k deploy
```