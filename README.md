Google File System similar, C# Implementation,Technical University of Lisbon Course

- Replicated Metadata servers
- Load balancing, high-availability, consistency, parallel access and scability


#Metadata Servers (My Main task beside System Architecture)
The client could contact any server. However each server is the serializer of a 2/6 of namespace so the request has to be forward to correct serializer to ensure message total ordering. The master chain-replicates the request throw all other servers to ensure fail-tolerance.

We implemented Bully algorithm for server entrance. Later we implemented a pause, ready, online protocol where a new server notifies all current instances to stop and wait for correct *state vector* each instance includes new server in its group and then start again.

Metadata servers suport n-1 failures.


#Data Servers
Server churn required metadata server notification
All data is persistent on disk.
*Read/Write Semantics*: We implemented monolitic reads (ensuring client read a version equal or higher than last seen version).



