docker build -f ".\consumer\dockerfile" --force-rm -t consumer   "c:\users\tamir.dresher\source\repos\distributedbalancedpartitionqueueassignment"
docker build -f ".\producer\dockerfile" --force-rm -t producer   "c:\users\tamir.dresher\source\repos\distributedbalancedpartitionqueueassignment"
helm install balancedpartitions .\charts\