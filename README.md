# Distributed balanced partition-queues assignment using kubernetes statefulSet 

This demo app demonstrates how to leverage kubernetes statefulSets to achieve a balanced assignment of rabbitmq queues between independent distributed consumers

To run this app run
```
helm install balancedpartitions .\charts\
```
if you just want to see the yaml files generated by helm then run this
```
helm install --dry-run balancedpartitions .\charts\
``` 

you can change the amount of partitions and consumer by setting the value in the .\charts\values.yaml file.

you can change the amount of consumer at run time by running this command 
```
kubectl scale statefulsets consumer --replicas=4
```


To watch logs from all the consumers run this
```
kubectl logs -l app=consumer --follow
```

to work locally and debug outside of k8s cluster do this:
1. expose kubernetes API by running 
```
kubectl proxy
```

2. expose rabbitmq (i used port 25672 to avoid conflicting with other rabbitmq you might be running) replace RABBIT_POD_ID with the pod id you see when running kubectl get pods
```
kubectl --namespace default port-forward RABBIT_POD_ID 25672:5672
```
3. you can also expose rabbbit management console by running this
```
kubectl --namespace default port-forward POD_ID 35672:15672
```