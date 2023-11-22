# Model

Model namespace provides definitions for all the interfaces representing basic components for executing the top-level consensus protocol.

The main components defined in this namespace are:
1. The IUnit and IPreunit interfaces, representing information produced by a single process in a single round of the protocol.
2. The DAG, containing all the units created by processes and representing the partial order between them.
3. The random source interacting with the DAG to generate randomness needed for the protocol.

This namespace provides more than presented above. Each file is documented with information for all definitions. 